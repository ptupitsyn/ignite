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
    using System.Linq;
    using Apache.Ignite.Core.Cache.Configuration;
    using NUnit.Framework;

    /// <summary>
    /// Memory metrics tests.
    /// </summary>
    public class MemoryMetricsTest
    {
        /** */
        private const string MemoryPolicyWithMetrics = "plcWithMetrics";

        /** */
        private const string MemoryPolicyNoMetrics = "plcNoMetrics";

        [Test]
        public void Test()
        {
            var ignite = StartIgniteWithTwoPolicies();

            // Verify metrics.
            var metrics = ignite.GetMemoryMetrics();
            Assert.AreEqual(2, metrics.Count);

            var memMetrics = metrics.First();
            Assert.AreEqual(MemoryPolicyWithMetrics, memMetrics.Name);
            // TODO

            var emptyMetrics = metrics.Last();
            Assert.AreEqual(MemoryPolicyNoMetrics, emptyMetrics.Name);
            // TODO
        }

        /// <summary>
        /// Starts the ignite with two policies.
        /// </summary>
        private static IIgnite StartIgniteWithTwoPolicies()
        {
            var cfg = new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                MemoryConfiguration = new MemoryConfiguration
                {
                    DefaultMemoryPolicyName = MemoryPolicyWithMetrics,
                    MemoryPolicies = new[]
                    {
                        new MemoryPolicyConfiguration
                        {
                            Name = MemoryPolicyWithMetrics,
                            MetricsEnabled = true
                        },
                        new MemoryPolicyConfiguration
                        {
                            Name = MemoryPolicyNoMetrics,
                            MetricsEnabled = false
                        }
                    }
                }
            };

            var ignite = Ignition.Start(cfg);

            // Create caches and do some things with them.
            var cacheNoMetrics = ignite.CreateCache<int, int>(new CacheConfiguration("cacheNoMetrics")
            {
                MemoryPolicyName = MemoryPolicyNoMetrics
            });

            cacheNoMetrics.Put(1, 1);
            cacheNoMetrics.Get(1);

            var cacheWithMetrics = ignite.CreateCache<int, int>(new CacheConfiguration("cacheWithMetrics")
            {
                MemoryPolicyName = MemoryPolicyWithMetrics
            });

            cacheWithMetrics.Put(1, 1);
            cacheWithMetrics.Get(1);

            return ignite;
        }

        /// <summary>
        /// Tears down the test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Ignition.StopAll(true);
        }
    }
}
