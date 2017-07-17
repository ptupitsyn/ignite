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
    using System.Net;
    using System.Net.Sockets;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Configuration;
    using NUnit.Framework;

    /// <summary>
    /// Tests the thin client mode (no JVM in process).
    /// </summary>
    public class ThinClientTest
    {
        /// <summary>
        /// Tests the ODBC connection.
        /// </summary>
        [Test]
        public void TestOdbc()
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

                // Handshake.

                // SQL query.
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
