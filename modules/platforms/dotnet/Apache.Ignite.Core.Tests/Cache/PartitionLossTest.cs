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

namespace Apache.Ignite.Core.Tests.Cache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Affinity.Rendezvous;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Common;
    using NUnit.Framework;

    /// <summary>
    /// Tests partition loss management functionality:
    /// <see cref="PartitionLossPolicy"/>, <see cref="IIgnite.ResetLostPartitions(IEnumerable{string})"/>,
    /// <see cref="ICache{TK,TV}.GetLostPartitions"/>, <see cref="ICache{TK,TV}"/>.
    /// </summary>
    public class PartitionLossTest
    {
        /** */
        private const string CacheName = "lossTestCache";

        [Test]
        public void Test()
        {
            var cacheCfg = new CacheConfiguration(CacheName)
            {
                CacheMode = CacheMode.Partitioned,
                Backups = 0,
                WriteSynchronizationMode = CacheWriteSynchronizationMode.FullSync,
                PartitionLossPolicy = PartitionLossPolicy.ReadOnlyAll,
                AffinityFunction = new RendezvousAffinityFunction
                {
                    ExcludeNeighbors = false,
                    Partitions = 32
                }
            };

            using (var ignite = Ignition.Start(TestUtils.GetTestConfiguration()))
            {
                var cache = ignite.CreateCache<int, int>(cacheCfg);

                var lostPart = PrepareTopology();

                var lostParts = cache.GetLostPartitions();

                Assert.IsTrue(lostParts.Contains(lostPart));

                ignite.ResetLostPartitions(CacheName);

                Assert.IsEmpty(cache.GetLostPartitions());

                // Check another ResetLostPartitions overload.
                PrepareTopology();
                Assert.IsNotEmpty(cache.GetLostPartitions());
                ignite.ResetLostPartitions(new List<string> {CacheName});
                Assert.IsEmpty(cache.GetLostPartitions());

                // Invalid cache name.
                var ex = Assert.Throws<IgniteException>(() => ignite.ResetLostPartitions("baz"));
                Assert.AreEqual("todo", ex.Message);

                ex = Assert.Throws<IgniteException>(() => ignite.ResetLostPartitions(CacheName, "baz"));
                Assert.AreEqual("todo", ex.Message);

            }
        }

        [TearDown]
        public void TearDown()
        {
            Ignition.StopAll(true);
        }

        /// <summary>
        /// Prepares the topology: starts a new node and stops it after rebalance to ensure data loss.
        /// </summary>
        /// <returns>Lost partition id.</returns>
        private static int PrepareTopology()
        {
            using (var ignite = Ignition.Start(TestUtils.GetTestConfiguration(name: "ignite-2")))
            {
                var cache = ignite.GetCache<int, int>(CacheName);

                var affinity = ignite.GetAffinity(CacheName);

                var keys = Enumerable.Range(1, affinity.Partitions).ToArray();

                cache.PutAll(keys.ToDictionary(x => x, x => x));

                cache.Rebalance();

                // Wait for rebalance to complete.
                var node = ignite.GetCluster().GetLocalNode();
                Func<int, bool> isPrimary = x => affinity.IsPrimary(node, x);

                while (!keys.Any(isPrimary))
                {
                    Thread.Sleep(10);
                }

                return keys.First(isPrimary);
            }
        }
    }
}
