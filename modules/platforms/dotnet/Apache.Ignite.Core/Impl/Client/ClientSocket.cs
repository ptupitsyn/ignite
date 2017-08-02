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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using Apache.Ignite.Core.Client;
    using Apache.Ignite.Core.Common;

    /// <summary>
    /// Wrapper over framework socket for Ignite thin client operations.
    /// </summary>
    internal class ClientSocket : IDisposable
    {
        /** Unerlying socket. */
        private readonly Socket _socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSocket"/> class.
        /// </summary>
        /// <param name="clientConfiguration">The client configuration.</param>
        public ClientSocket(IgniteClientConfiguration clientConfiguration)
        {
            Debug.Assert(clientConfiguration != null);

            _socket = Connect(clientConfiguration);
        }

        /// <summary>
        /// Connects the socket.
        /// </summary>
        private static Socket Connect(IgniteClientConfiguration cfg)
        {
            List<Exception> errors = null;

            foreach (var ipEndPoint in GetEndPoints(cfg))
            {
                try
                {
                    var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                    {
                        SendBufferSize = cfg.SocketSendBufferSize,
                        ReceiveBufferSize = cfg.SocketReceiveBufferSize,
                        NoDelay = cfg.TcpNoDelay
                    };
                    
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
            var addressList = cfg.Host != null
                ? Dns.GetHostEntry(cfg.Host).AddressList
                : new[] { IPAddress.Loopback };

            foreach (var ipAddress in addressList)
            {
                for (var port = cfg.Port; port < cfg.Port + cfg.PortRange; port++)
                {
                    yield return new IPEndPoint(ipAddress, port);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}
