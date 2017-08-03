﻿/*
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
    using System.Linq;
    using System.Net.Sockets;
    using Apache.Ignite.Core.Client;
    using NUnit.Framework;

    /// <summary>
    /// Tests client connection: port ranges, version checks, etc.
    /// </summary>
    public class ClientConnectionTest
    {
        /// <summary>
        /// Fixture set up.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            Ignition.Start(TestUtils.GetTestConfiguration());
        }

        /// <summary>
        /// Fixture tear down.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            Ignition.StopAll(true);
        }

        /// <summary>
        /// Tests that incorrect port yields connection refused error.
        /// </summary>
        [Test]
        public void TestIncorrectPortConnectionRefused()
        {
            // Try incorrect port.
            var cfg = new IgniteClientConfiguration {Port = 65000};

            var ex = Assert.Throws<AggregateException>(() => Ignition.GetClient(cfg));
            var socketEx = ex.InnerExceptions.OfType<SocketException>().First();
            Assert.AreEqual(SocketError.ConnectionRefused, socketEx.SocketErrorCode);
        }

        /// <summary>
        /// Tests that multiple clients can connect to one server.
        /// </summary>
        [Test]
        public void TestMultipleClients()
        {
            
        }

        /// <summary>
        /// Tests custom connector and client configuration.
        /// </summary>
        [Test]
        public void TestCustomConfig()
        {
            
        }
    }
}
