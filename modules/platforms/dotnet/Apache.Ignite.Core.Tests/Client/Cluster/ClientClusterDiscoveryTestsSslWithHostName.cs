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

namespace Apache.Ignite.Core.Tests.Client.Cluster
{
    using System;
    using Apache.Ignite.Core.Client;
    using NUnit.Framework;

    public class ClientClusterDiscoveryTestsSslWithHostName : ClientTestBase
    {
        public ClientClusterDiscoveryTestsSslWithHostName() : base(3, enableSsl: true)
        {
            // No-op.
        }

        /// <summary>
        /// Tests that client with one initial endpoint discovers all servers.
        /// </summary>
        [Test]
        public void TestClientWithOneEndpointDiscoversAllServers()
        {
            using (var client = GetClient())
            {
                ClientClusterDiscoveryTestsBase.AssertClientConnectionCount(client, 3);
            }
        }

        /** <inheritdoc /> */
        protected override IgniteClientConfiguration GetClientConfiguration()
        {
            return new IgniteClientConfiguration(base.GetClientConfiguration())
            {
                EnablePartitionAwareness = true
            };
        }

        /** <inheritdoc /> */
        protected override IgniteConfiguration GetIgniteConfiguration()
        {
            // IgniteUtils.addresses ignores loopback host names, so we set it via env variable.
            Environment.SetEnvironmentVariable("IGNITE_LOCAL_HOST", "foo.localhost");

            return new IgniteConfiguration(base.GetIgniteConfiguration())
            {
                // Localhost = _noLocalhost ? null : "foo.localhost",
                AutoGenerateIgniteInstanceName = true
            };
        }
    }
}
