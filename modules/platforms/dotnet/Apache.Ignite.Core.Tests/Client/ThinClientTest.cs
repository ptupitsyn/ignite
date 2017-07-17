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

namespace Apache.Ignite.Core.Tests.Client
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Configuration;
    using Apache.Ignite.Core.Impl.Binary.IO;
    using NUnit.Framework;

    /// <summary>
    /// Tests the thin client mode (no JVM in process).
    /// </summary>
    public class ThinClientTest
    {
        /// <summary>
        /// Tests the socket handshake connection.
        /// </summary>
        [Test]
        public void TestHandshake()
        {
            var cfg = new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                SqlConnectorConfiguration = new SqlConnectorConfiguration()
            };

            using (var ignite = Ignition.Start(cfg))
            {
                // Create cache.
                var cacheCfg = new CacheConfiguration("foo", new QueryEntity(typeof(int), typeof(string)));
                var cache = ignite.CreateCache<int, string>(cacheCfg);
                cache[1] = "bar";

                // Connect socket.
                var sock = GetSocket(SqlConnectorConfiguration.DefaultPort);
                Assert.IsTrue(sock.Connected);

                var sentBytes = SendRequest(sock, stream =>
                {
                    // Handshake.
                    stream.WriteByte(1);

                    // Protocol version.
                    stream.WriteShort(2);
                    stream.WriteShort(1);
                    stream.WriteShort(0);

                    // Client type: platform.
                    stream.WriteByte(2);
                });

                Assert.AreEqual(12, sentBytes);

                // ACK.
                var buf = new byte[1];
                sock.Receive(buf);

                using (var stream = new BinaryHeapStream(buf))
                {
                    var ack = stream.ReadBool();

                    Assert.IsTrue(ack);
                }
            }
        }

        private static int SendRequest(Socket sock, Action<BinaryHeapStream> writeAction)
        {
            // TODO: Use NetworkStream instead? But there is no message size in it..
            using (var stream = new BinaryHeapStream(128))
            {
                stream.WriteInt(0);  // Reserve message size.

                writeAction(stream);

                stream.WriteInt(0, stream.Position - 4);  // Write message size.

                return sock.Send(stream.GetArray(), stream.Position, SocketFlags.None);
            }
        }

        /// <summary>
        /// Gets the socket.
        /// </summary>
        private static Socket GetSocket(int port)
        {
            var endPoint = new IPEndPoint(IPAddress.Loopback, port);
            var sock = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(endPoint);
            return sock;
        }
    }
}
