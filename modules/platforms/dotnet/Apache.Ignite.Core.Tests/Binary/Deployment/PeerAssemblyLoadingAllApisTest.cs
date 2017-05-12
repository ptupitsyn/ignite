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

namespace Apache.Ignite.Core.Tests.Binary.Deployment
{
    using System.Linq;
    using Apache.Ignite.Core.Cluster;
    using Apache.Ignite.Core.Tests.Compute;
    using Apache.Ignite.Core.Tests.Process;
    using NUnit.Framework;

    /// <summary>
    /// Tests all APIs that support peer assembly loading.
    /// </summary>
    [Category(TestUtils.CategoryIntensive)]
    public class PeerAssemblyLoadingAllApisTest
    {
        /// <summary>
        /// Tests Compute.Call.
        /// </summary>
        [Test]
        public void TestComputeCall([Values(true, false)] bool async)
        {
            PeerAssemblyLoadingTest.TestDeployment(remoteCompute =>
            {
                Assert.AreEqual("Apache.Ignite", async
                    ? remoteCompute.CallAsync(new ProcessNameFunc()).Result
                    : remoteCompute.Call(new ProcessNameFunc()));
            });
        }

        /// <summary>
        /// Tests Compute.Call.
        /// </summary>
        [Test]
        public void TestComputeAffinityCall([Values(true, false)] bool async)
        {
            PeerAssemblyLoadingTest.TestDeployment(ignite =>
            {
                var cache = ignite.GetOrCreateCache<int, int>("myCache");

                var key = TestUtils.GetPrimaryKey(ignite, cache.Name, ignite.GetCluster().ForRemotes().GetNode());

                var res = async
                    ? ignite.GetCompute().AffinityCallAsync(cache.Name, key, new ProcessNameFunc()).Result
                    : ignite.GetCompute().AffinityCall(cache.Name, key, new ProcessNameFunc());

                Assert.AreEqual("Apache.Ignite",
                    res);
            });
        }

        /// <summary>
        /// Tests Compute.Execute.
        /// </summary>
        [Test]
        public void TestComputeExecute([Values(true, false)] bool async)
        {
            PeerAssemblyLoadingTest.TestDeployment(remoteCompute =>
            {
                Assert.AreEqual("Apache.Ignite", async 
                    ? remoteCompute.ExecuteAsync(new ProcessNameTask()).Result
                    : remoteCompute.Execute(new ProcessNameTask()));
            });
        }

        /// <summary>
        /// Tests Compute.Broadcast(IComputeAction).
        /// </summary>
        [Test]
        public void TestComputeBroadcastAction([Values(true, false)] bool async)
        {
            PeerAssemblyLoadingTest.TestDeployment(remoteCompute =>
            {
                if (async)
                {
                    remoteCompute.BroadcastAsync(new ComputeAction()).Wait();
                }
                else
                {
                    remoteCompute.Broadcast(new ComputeAction());
                }
            });
        }

        /// <summary>
        /// Tests Compute.Broadcast(IComputeFunc{T}).
        /// </summary>
        [Test]
        public void TestComputeBroadcastOutFunc([Values(true, false)] bool async)
        {
            PeerAssemblyLoadingTest.TestDeployment(remoteCompute =>
            {
                var results = async
                    ? remoteCompute.BroadcastAsync(new ProcessNameFunc()).Result
                    : remoteCompute.Broadcast(new ProcessNameFunc());

                Assert.AreEqual("Apache.Ignite", results.Single());
            });
        }

        /// <summary>
        /// Tests Compute.Broadcast(IComputeFunc{TArg, TRes}).
        /// </summary>
        [Test]
        public void TestComputeBroadcastFunc([Values(true, false)] bool async)
        {
            PeerAssemblyLoadingTest.TestDeployment(remoteCompute =>
            {
                var results = async
                    ? remoteCompute.BroadcastAsync(new ProcessNameArgFunc(), "_").Result
                    : remoteCompute.Broadcast(new ProcessNameArgFunc(), "_");

                Assert.AreEqual("Apache.Ignite_", results.Single());
            });
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
    }
}
