/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Apache.Ignite.Core.Impl.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Apache.Ignite.Core.Client;
    using Apache.Ignite.Core.Common;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Binary.IO;

    /// <summary>
    /// Wrapper over framework socket for Ignite thin client operations.
    /// </summary>
    internal sealed class ClientSocket : IDisposable
    {
        /** Current version. */
        private static readonly ClientProtocolVersion CurrentProtocolVersion = new ClientProtocolVersion(1, 0, 0);

        /** Handshake opcode. */
        private const byte OpHandshake = 1;

        /** Client type code. */
        private const byte ClientType = 2;

        /** Underlying socket. */
        private readonly Socket _socket;

        /** */
        private long _requestId;

        /** Current async operations, map from request id. */
        private readonly ConcurrentDictionary<long, TaskCompletionSource<BinaryHeapStream>> _requests
            = new ConcurrentDictionary<long, TaskCompletionSource<BinaryHeapStream>>();

        /** Receiver sync root. */
        private readonly object _receiveSyncRoot = new object();

        /** Buffer for currently received message */
        private byte[] _receiveBuf;

        /** Length of the currently received message. */
        private int _receiveMessageLen;

        /** Number of received bytes for the current message. */
        private int _received;

        /** Whether we are waiting for new message (starts with 4-byte length), or receiving message data. */
        private bool _waitingForNewMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSocket" /> class.
        /// </summary>
        /// <param name="clientConfiguration">The client configuration.</param>
        /// <param name="version">Protocol version.</param>
        public ClientSocket(IgniteClientConfiguration clientConfiguration, ClientProtocolVersion? version = null)
        {
            Debug.Assert(clientConfiguration != null);

            _socket = Connect(clientConfiguration);

            Handshake(_socket, version ?? CurrentProtocolVersion);

            // Continuously and asynchronously wait for data from server.
            _socket.Blocking = false;
            WaitForNewMessage();
        }

        /// <summary>
        /// Performs a send-receive operation.
        /// </summary>
        public T DoOutInOp<T>(ClientOp opId, Action<IBinaryStream> writeAction,
            Func<IBinaryStream, T> readFunc, Func<ClientStatusCode, string, T> errorFunc = null)
        {
            var response = SendRequestAsync(opId, writeAction).Result;

            // Decode on current thread for proper exception handling.
            // We could call DoOutInOpAsync, but it wraps exceptions in AggregateException.
            return DecodeResponse(response, readFunc, errorFunc);
        }

        /// <summary>
        /// Performs a send-receive operation asynchronously.
        /// </summary>
        public Task<T> DoOutInOpAsync<T>(ClientOp opId, Action<IBinaryStream> writeAction,
            Func<IBinaryStream, T> readFunc, Func<ClientStatusCode, string, T> errorFunc = null)
        {
            return SendRequestAsync(opId, writeAction)
                .ContinueWith(responseTask => DecodeResponse(responseTask.Result, readFunc, errorFunc));
        }

        /// <summary>
        /// Sends the request asynchronously and returns a task for corresponding response.
        /// </summary>
        private Task<BinaryHeapStream> SendRequestAsync(ClientOp opId, Action<IBinaryStream> writeAction)
        {
            // Register new request.
            var requestId = Interlocked.Increment(ref _requestId);
            var tcs = new TaskCompletionSource<BinaryHeapStream>();
            var added = _requests.TryAdd(requestId, tcs);
            Debug.Assert(added);

            // Send.
            SendAsync(_socket, stream =>
            {
                stream.WriteShort((short) opId);
                stream.WriteLong(requestId);

                if (writeAction != null)
                {
                    writeAction(stream);
                }
            });
            
            return tcs.Task;
        }

        /// <summary>
        /// Starts waiting for the new message.
        /// </summary>
        private IAsyncResult WaitForNewMessage()
        {
            return WaitForPayload(4, true);
        }

        /// <summary>
        /// Starts waiting for the payload of specified size.
        /// </summary>
        private IAsyncResult WaitForPayload(int size, bool newMessage = false)
        {
            _receiveMessageLen = size;
            _receiveBuf = new byte[_receiveMessageLen];
            _received = 0;
            _waitingForNewMessage = newMessage;

            // TODO: check if data is available before calling BeginReceive?
            // While loop inside OnReceive to avoid stackOverflow
            return _socket.BeginReceive(_receiveBuf, 0, _receiveMessageLen, SocketFlags.None, OnReceive, null);
        }

        /// <summary>
        /// Called when data has been received from socket.
        /// </summary>
        private void OnReceive(IAsyncResult ar)
        {
            if (ar.CompletedSynchronously)
            {
                // Avoid stack overflow caused by recursive OnReceive calls.
                // Synchronous completion is handled in a loop below.
                return;
            }

            while (true)
            {
                byte[] response = null;

                // TODO: Do we need a lock? Only one callback works at a time.
                lock (_receiveSyncRoot)
                {
                    _received += _socket.EndReceive(ar);

                    if (_received < _receiveMessageLen)
                    {
                        // Got a part of data, continue waiting for the entire message.
                        ar = _socket.BeginReceive(_receiveBuf, _received, _receiveMessageLen - _received, 
                            SocketFlags.None, OnReceive, null);
                    }
                    else if (_received == _receiveMessageLen)
                    {
                        if (_waitingForNewMessage)
                        {
                            // Got a new message length in the buffer, start receiving payload.
                            Debug.Assert(_received == 4);
                            Debug.Assert(_receiveBuf.Length == 4);

                            var size = GetInt(_receiveBuf);
                            ar = WaitForPayload(size);
                        }
                        else
                        {
                            // Got the message payload, dispatch corresponding task.
                            response = _receiveBuf;
                            ar = WaitForNewMessage();
                        }
                    }
                    else
                    {
                        // Invalid situation.
                        throw new InvalidOperationException(
                            string.Format("Received unexpected number of bytes. Expected: {0}, got: {1}",
                                _receiveMessageLen, _received));
                    }
                }

                if (response != null)
                {
                    // Decode response outside the lock.
                    HandleResponse(response);
                }

                // ReSharper disable once PossibleNullReferenceException (looks wrong)
                if (!ar.CompletedSynchronously)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Handles the response.
        /// </summary>
        private void HandleResponse(byte[] response)
        {
            using (var stream = new BinaryHeapStream(response))
            {
                var requestId = stream.ReadLong();

                TaskCompletionSource<BinaryHeapStream> req;
                if (!_requests.TryRemove(requestId, out req))
                {
                    // Response with unknown id.
                    // Nothing to do, nowhere to throw an error.
                    return;
                }

                req.SetResult(stream);
            }
        }

        /// <summary>
        /// Decodes the response that we got from <see cref="HandleResponse"/>.
        /// </summary>
        private static T DecodeResponse<T>(BinaryHeapStream stream, Func<IBinaryStream, T> readFunc, 
            Func<ClientStatusCode, string, T> errorFunc)
        {
            var statusCode = (ClientStatusCode)stream.ReadInt();

            if (statusCode == ClientStatusCode.Success)
            {
                return readFunc != null ? readFunc(stream) : default(T);
            }

            var msg = BinaryUtils.Marshaller.StartUnmarshal(stream).ReadString();

            if (errorFunc != null)
            {
                return errorFunc(statusCode, msg);
            }

            throw new IgniteClientException(msg, null, statusCode);
        }

        /// <summary>
        /// Performs client protocol handshake.
        /// </summary>
        private static void Handshake(Socket sock, ClientProtocolVersion version)
        {
            // Perform handshake in blocking mode for simplicity.
            sock.Blocking = true;

            // Send request.
            int messageLen;
            var buf = WriteMessage(stream =>
            {
                // Handshake.
                stream.WriteByte(OpHandshake);

                // Protocol version.
                stream.WriteShort(version.Major);
                stream.WriteShort(version.Minor);
                stream.WriteShort(version.Maintenance);

                // Client type: platform.
                stream.WriteByte(ClientType);
            }, 20, out messageLen);

            Debug.Assert(messageLen == 20);

            var sent = sock.Send(buf, messageLen, SocketFlags.None);
            Debug.Assert(sent == messageLen);

            // Decode response.
            buf = ReceiveAll(sock, 4);
            var size = GetInt(buf);
            var res = ReceiveAll(sock, size);

            using (var stream = new BinaryHeapStream(res))
            {
                var success = stream.ReadBool();

                if (success)
                {
                    return;
                }

                var serverVersion =
                    new ClientProtocolVersion(stream.ReadShort(), stream.ReadShort(), stream.ReadShort());

                var errMsg = BinaryUtils.Marshaller.Unmarshal<string>(stream);

                throw new IgniteClientException(string.Format(
                    "Client handhsake failed: '{0}'. Client version: {1}. Server version: {2}",
                    errMsg, version, serverVersion));
            }
        }

        /// <summary>
        /// Receives the data filling provided buffer entirely.
        /// </summary>
        private static byte[] ReceiveAll(Socket sock, int size)
        {
            // Socket.Receive can return any number of bytes, even 1.
            // We should repeat Receive calls until required amount of data has been received.
            var buf = new byte[size];
            var received = sock.Receive(buf);

            while (received < size)
            {
                received += sock.Receive(buf, received, size - received, SocketFlags.None);
            }

            return buf;
        }

        /// <summary>
        /// Sends the request and receives a response.
        /// </summary>
        private static void SendAsync(Socket sock, Action<IBinaryStream> writeAction, int bufSize = 128)
        {
            int messageLen;
            var buf = WriteMessage(writeAction, bufSize, out messageLen);

            sock.BeginSend(buf, 0, messageLen, SocketFlags.None, ar =>
            {
                var sent = sock.EndSend(ar);
                Debug.Assert(sent == (int) ar.AsyncState);
            }, messageLen);
        }

        /// <summary>
        /// Writes the message to a byte array.
        /// </summary>
        private static byte[] WriteMessage(Action<IBinaryStream> writeAction, int bufSize, out int messageLen)
        {
            using (var stream = new BinaryHeapStream(bufSize))
            {
                stream.WriteInt(0); // Reserve message size.

                writeAction(stream);

                stream.WriteInt(0, stream.Position - 4); // Write message size.

                messageLen = stream.Position;

                return stream.GetArray();
            }
        }

        /// <summary>
        /// Connects the socket.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", 
            Justification = "Socket is returned from this method.")]
        private static Socket Connect(IgniteClientConfiguration cfg)
        {
            List<Exception> errors = null;

            foreach (var ipEndPoint in GetEndPoints(cfg))
            {
                try
                {
                    var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = cfg.TcpNoDelay
                    };

                    if (cfg.SocketSendBufferSize != IgniteClientConfiguration.DefaultSocketBufferSize)
                    {
                        socket.SendBufferSize = cfg.SocketSendBufferSize;
                    }

                    if (cfg.SocketReceiveBufferSize != IgniteClientConfiguration.DefaultSocketBufferSize)
                    {
                        socket.ReceiveBufferSize = cfg.SocketReceiveBufferSize;
                    }

                    socket.Connect(ipEndPoint);

                    return socket;
                }
                catch (SocketException e)
                {
                    if (errors == null)
                    {
                        errors = new List<Exception>();
                    }

                    errors.Add(e);
                }
            }

            if (errors == null)
            {
                throw new IgniteException("Failed to resolve client host: " + cfg.Host);
            }

            throw new AggregateException("Failed to establish Ignite thin client connection, " +
                                         "examine inner exceptions for details.", errors);
        }

        /// <summary>
        /// Gets the endpoints: all combinations of IP addresses and ports according to configuration.
        /// </summary>
        private static IEnumerable<IPEndPoint> GetEndPoints(IgniteClientConfiguration cfg)
        {
            var host = cfg.Host;

            if (host == null)
            {
                throw new IgniteException("IgniteClientConfiguration.Host cannot be null.");
            }

            // GetHostEntry accepts IPs, but TryParse is a more efficient shortcut.
            IPAddress ip;

            if (IPAddress.TryParse(host, out ip))
            {
                return new[] {new IPEndPoint(ip, cfg.Port)};
            }

            return Dns.GetHostEntry(host).AddressList.Select(x => new IPEndPoint(x, cfg.Port));
        }

        /// <summary>
        /// Gets the int from buffer.
        /// </summary>
        private static unsafe int GetInt(byte[] buf)
        {
            fixed (byte* b = buf)
            {
                return BinaryHeapStream.ReadInt0(b);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly",
            Justification = "There is no finalizer.")]
        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}
