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

namespace Apache.Ignite.Core.Tests.Binary.Deployment
{
    using System;
    using System.IO;
    using System.Threading;
    using Apache.Ignite.Core.Impl;
    using Apache.Ignite.Core.Impl.Common;
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
        /// Tests that a binarizable type can be peer deployed.
        /// </summary>
        [Test]
        public void TestComputeCall()
        {
            TestDeployment(ignite =>
            {
                var result = ignite.GetCluster().ForRemotes().GetCompute().Call(new ProcessNameFunc());

                Assert.AreEqual("Apache.Ignite", result);
            });
        }

        /// <summary>
        /// Tests that a type which requires multiple assemblies can be peer deployed.
        /// </summary>
        [Test]
        public void TestMultipleAssemblies()
        {
            // TODO
        }

        /// <summary>
        /// Tests that multiple versions of same assembly can be used on remote nodes.
        /// </summary>
        [Test]
        public void TestMultipleVersionsOfSameAssembly()
        {
            // TODO
        }

        /// <summary>
        /// Tests the peer deployment.
        /// </summary>
        private void TestDeployment(Action<IIgnite> test)
        {
            // Copy Apache.Ignite.exe and Apache.Ignite.Core.dll 
            // to a separate folder so that it does not locate our assembly automatically.
            var folder = IgniteUtils.GetTempDirectoryName();
            foreach (var asm in new[] {typeof(IgniteRunner).Assembly, typeof(Ignition).Assembly})
            {
                Assert.IsNotNull(asm.Location);
                File.Copy(asm.Location, Path.Combine(folder, Path.GetFileName(asm.Location)));
            }

            var exePath = Path.Combine(folder, "Apache.Ignite.exe");

            // Start separate Ignite process without loading current dll.
            // ReSharper disable once AssignNullToNotNullAttribute
            var config = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location),
                "Binary\\Deployment\\peer_assembly_app.config");

            var proc = IgniteProcess.Start(exePath, IgniteHome.Resolve(null), null,
                "-ConfigFileName=" + config, "-ConfigSectionName=igniteConfiguration");

            Thread.Sleep(300);
            Assert.IsFalse(proc.HasExited);

            // Start Ignite and execute computation on remote node.
            var cfg = new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                IsPeerAssemblyLoadingEnabled = true
            };

            using (var ignite = Ignition.Start(cfg))
            {
                Assert.IsTrue(ignite.WaitTopology(2));

                for (var i = 0; i < 10; i++)
                {
                    test(ignite);
                }
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
    }
}
