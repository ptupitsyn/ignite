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

namespace Apache.Ignite.Core.Tests.Client.Datastream
{
    using Apache.Ignite.Core.Client;
    using Apache.Ignite.Core.Impl.Client;
    using NUnit.Framework;

    /// <summary>
    /// Tests thin client data streamer with topology changes.
    /// </summary>
    public class DataStreamerClientTopologyChangeTest
    {
        [Test]
        public void TestNodeLeave()
        {
            var node1 = StartServer();
            var client = StartClient();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            Ignition.StopAll(true);
        }

        private static IgniteConfiguration GetServerConfiguration()
        {
            return new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                AutoGenerateIgniteInstanceName = true
            };
        }

        private static IIgnite StartServer()
        {
            return Ignition.Start(GetServerConfiguration());
        }

        private static IIgniteClient StartClient()
        {
            var cfg = new IgniteClientConfiguration("127.0.0.1:10800..10805");

            return new IgniteClient(cfg);
        }
    }
}