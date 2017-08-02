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

            var addressList = clientConfiguration.Host != null
                ? Dns.GetHostEntry(clientConfiguration.Host).AddressList
                : new[] {IPAddress.Loopback};

            if (addressList.Length == 0)
            {
                throw new IgniteException("Failed to resolve client host: " + clientConfiguration.Host);
            }

            List<Exception> errors = null;

            foreach (var ipAddress in addressList)
            {
                _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    _socket.Connect(ipAddress, clientConfiguration.Port);
                    return;
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

            throw new AggregateException("Failed to establish Ignite thin client connection, " +
                                         "examine inner exceptions for details.", errors);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_socket != null)
            {
                _socket.Dispose();
            }
        }
    }
}
