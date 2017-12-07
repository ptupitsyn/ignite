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

        /** Operation timeout. */
        private readonly TimeSpan _timeout;

        /** Request timeout checker. */
        private readonly Timer _timeoutCheckTimer;

        /** Current async operations, map from request id. */
        private readonly ConcurrentDictionary<long, Request> _requests
            = new ConcurrentDictionary<long, Request>();

        /** Request id generator. */
        private long _requestId;

        /** Socket failure exception. */
        private volatile Exception _exception;

        /** Locker. */
        private readonly object _syncRoot = new object();

        /** */
        private readonly ManualResetEventSlim _listenerEvent = new ManualResetEventSlim();

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSocket" /> class.
        /// </summary>
        /// <param name="clientConfiguration">The client configuration.</param>
        /// <param name="version">Protocol version.</param>
        public ClientSocket(IgniteClientConfiguration clientConfiguration, ClientProtocolVersion? version = null)
        {
            Debug.Assert(clientConfiguration != null);

            _timeout = clientConfiguration.SocketTimeout;

            _socket = Connect(clientConfiguration);

            Handshake(_socket, version ?? CurrentProtocolVersion);

            // Check periodically if any request has timed out.
            if (_timeout > TimeSpan.Zero)
            {
                _timeoutCheckTimer = new Timer(CheckTimeouts, null, _timeout, TimeSpan.FromMilliseconds(100));
            }

            // Continuously and asynchronously wait for data from server.
            ThreadPool.QueueUserWorkItem(o => WaitForMessages());
        }

        /// <summary>
        /// Performs a send-receive operation.
        /// </summary>
        public T DoOutInOp<T>(ClientOp opId, Action<IBinaryStream> writeAction,
            Func<IBinaryStream, T> readFunc, Func<ClientStatusCode, string, T> errorFunc = null)
        {
            lock (_syncRoot)
            {
                // If there are no pending async requests, we can execute this operation synchronously,
                // which is more efficient.
                if (!_listenerEvent.IsSet)
                {
                    var requestId = SendRequest(opId, writeAction);

                    var msg = Receive(_socket);
                    var stream = new BinaryHeapStream(msg);
                    var responseId = stream.ReadLong();

                    Debug.Assert(responseId == requestId);
                    return DecodeResponse(stream, readFunc, errorFunc);
                }
            }

            // Fallback to async mechanism.
            var response = SendRequestAsync(opId, writeAction).Result;
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
        /// Starts waiting for the new message.
        /// </summary>
        private void WaitForMessages()
        {
            // Null exception means active socket.
            while (_exception == null)
            {
                // Do not call Receive if there are no async requests pending.
                while (_requests.IsEmpty)
                {
                    _listenerEvent.Wait();
                    _listenerEvent.Reset();
                }

                try
                {
                    var msg = Receive(_socket);
                    HandleResponse(msg);
                }
                catch (Exception ex)
                {
                    // Socket failure (connection dropped, etc).
                    // Propagate to all pending requests.
                    // Note that this does not include request decoding exceptions (TODO: add test).
                    _exception = new IgniteClientException("Socket communication failed.", ex);
                    _socket.Dispose();
                    EndRequestsWithError();
                }
            }
        }

        /// <summary>
        /// Handles the response.
        /// </summary>
        private void HandleResponse(byte[] response)
        {
            var stream = new BinaryHeapStream(response);
            var requestId = stream.ReadLong();

            Request req;
            if (!_requests.TryRemove(requestId, out req))
            {
                // Response with unknown id.
                // Nothing to do, nowhere to throw an error.
                return;
            }

            req.CompletionSource.TrySetResult(stream);
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
            }, 12, out messageLen);

            Debug.Assert(messageLen == 12);

            var sent = sock.Send(buf, messageLen, SocketFlags.None);
            Debug.Assert(sent == messageLen);

            // Decode response.
            var res = Receive(sock);

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
                    "Client handshake failed: '{0}'. Client version: {1}. Server version: {2}",
                    errMsg, version, serverVersion));
            }
        }

        /// <summary>
        /// Receives a message from socket.
        /// </summary>
        private static byte[] Receive(Socket sock)
        {
            var size = GetInt(ReceiveAll(sock, 4));
            var msg = ReceiveAll(sock, size);
            return msg;
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
        /// Sends the request asynchronously and returns a task for corresponding response.
        /// </summary>
        private Task<BinaryHeapStream> SendRequestAsync(ClientOp opId, Action<IBinaryStream> writeAction)
        {
            var ex = _exception;

            if (ex != null)
            {
                throw ex;
            }

            // Register new request.
            var requestId = Interlocked.Increment(ref _requestId);
            var req = new Request();
            var added = _requests.TryAdd(requestId, req);
            Debug.Assert(added);

            // Send.
            int messageLen;
            var buf = WriteMessage(writeAction, opId, requestId, 128, out messageLen);

            lock (_syncRoot)
            {
                _socket.Send(buf, 0, messageLen, SocketFlags.None);
                _listenerEvent.Set();
            }

            return req.CompletionSource.Task;
        }

        /// <summary>
        /// Sends the request asynchronously and returns a task for corresponding response.
        /// </summary>
        private long SendRequest(ClientOp opId, Action<IBinaryStream> writeAction)
        {
            var ex = _exception;

            if (ex != null)
            {
                throw ex;
            }

            // Register new request.
            var requestId = Interlocked.Increment(ref _requestId);

            // Send.
            int messageLen;
            var buf = WriteMessage(writeAction, opId, requestId, 128, out messageLen);

            _socket.Send(buf, 0, messageLen, SocketFlags.None);

            return requestId;
        }

        /// <summary>
        /// Writes the message to a byte array.
        /// </summary>
        private static byte[] WriteMessage(Action<IBinaryStream> writeAction, int bufSize, out int messageLen)
        {
            var stream = new BinaryHeapStream(bufSize);

            stream.WriteInt(0); // Reserve message size.
            writeAction(stream);
            stream.WriteInt(0, stream.Position - 4); // Write message size.

            messageLen = stream.Position;
            return stream.GetArray();
        }

        /// <summary>
        /// Writes the message to a byte array.
        /// </summary>
        private static byte[] WriteMessage(Action<IBinaryStream> writeAction, ClientOp opId, long requestId,
            int bufSize, out int messageLen)
        {
            var stream = new BinaryHeapStream(bufSize);

            stream.WriteInt(0); // Reserve message size.
            stream.WriteShort((short) opId);
            stream.WriteLong(requestId);
            writeAction(stream);
            stream.WriteInt(0, stream.Position - 4); // Write message size.

            messageLen = stream.Position;

            return stream.GetArray();
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
        /// Checks if any of the current requests timed out.
        /// </summary>
        private void CheckTimeouts(object _)
        {
            foreach (var pair in _requests)
            {
                var req = pair.Value;
                
                if (req.Duration > _timeout)
                {
                    req.CompletionSource.TrySetException(
                        new TimeoutException("Ignite thin client operation has timed out."));

                    _requests.TryRemove(pair.Key, out req);
                }
            }
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
        /// Closes the socket and completes all pending requests with an error.
        /// </summary>
        private void EndRequestsWithError()
        {
            var ex = _exception;
            Debug.Assert(ex != null);

            while (!_requests.IsEmpty)
            {
                foreach (var reqId in _requests.Keys.ToArray())
                {
                    Request req;
                    if (_requests.TryRemove(reqId, out req))
                    {
                        req.CompletionSource.TrySetException(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly",
            Justification = "There is no finalizer.")]
        public void Dispose()
        {
            _exception = new ObjectDisposedException(typeof(ClientSocket).FullName);

            _socket.Dispose();

            _timeoutCheckTimer.Dispose();

            EndRequestsWithError();
        }

        /// <summary>
        /// Represents a request.
        /// </summary>
        private class Request
        {
            /** */
            private readonly TaskCompletionSource<BinaryHeapStream> _completionSource;

            /** */
            private readonly DateTime _startTime;

            /// <summary>
            /// Initializes a new instance of the <see cref="Request"/> class.
            /// </summary>
            public Request()
            {
                _completionSource = new TaskCompletionSource<BinaryHeapStream>();
                _startTime = DateTime.Now;
            }

            /// <summary>
            /// Gets the completion source.
            /// </summary>
            public TaskCompletionSource<BinaryHeapStream> CompletionSource
            {
                get { return _completionSource; }
            }

            /// <summary>
            /// Gets the duration.
            /// </summary>
            public TimeSpan Duration
            {
                get { return DateTime.Now - _startTime; }
            }
        }
    }
}
