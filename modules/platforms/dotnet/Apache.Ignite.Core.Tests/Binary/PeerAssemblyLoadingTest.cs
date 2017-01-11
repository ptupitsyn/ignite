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

namespace Apache.Ignite.Core.Tests.Binary
{
    using System;
    using Apache.Ignite.Core.Compute;
    using Apache.Ignite.Core.Tests.Process;
    using NUnit.Framework;

    /// <summary>
    /// Tests peer assembly loading feature:
    /// when user-defined type is not found within loaded assemblies on remote node,
    /// corresponding assembly is sent and loaded automatically.
    /// </summary>
    public class PeerAssemblyLoadingTest
    {
        /// <summary>
        /// Tests that a normal assembly (loaded from disk) can be peer deployed.
        /// </summary>
        [Test]
        public void TestStaticAssembly()
        {
            // Start separate Ignite process without loading current dll.
            var appConfig = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            // TODO: This includes assembly, remove it.
            var proc = new IgniteProcess("-ConfigFileName="+appConfig, "-ConfigSectionName=igniteConfiguration");
            Assert.IsTrue(proc.Alive);

            // Start Ignite node in client mode to ensure that computations execute on standalone node.
            var cfg = new IgniteConfiguration(TestUtils.GetTestConfiguration()) {ClientMode = true};
            using (var ignite = Ignition.Start(cfg))
            {
                var result = ignite.GetCompute().Call(new GetProcessNameFunc());

                Assert.IsNotNullOrEmpty(result);
            }
        }

        /// <summary>
        /// Tears down the test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Ignition.StopAll(true);
            IgniteProcess.KillAll();
        }

        /// <summary>
        /// Tests that a dynamic in-memory assembly can be peer deployed.
        /// </summary>
        [Test]
        public void TestDynamicAssembly()
        {
            // TODO: ???
        }

        [Serializable]
        private class GetProcessNameFunc : IComputeFunc<string>
        {
            public string Invoke()
            {
                return System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            }
        }
    }
}
