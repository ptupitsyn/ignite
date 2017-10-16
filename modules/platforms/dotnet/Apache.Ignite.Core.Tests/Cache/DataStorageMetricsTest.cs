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
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Apache.Ignite.Core.Configuration;
    using Apache.Ignite.Core.Impl;
    using NUnit.Framework;

    /// <summary>
    /// Tests <see cref="IDataStorageMetrics"/>.
    /// </summary>
    public class DataStorageMetricsTest
    {
        /** Temp dir for WAL. */
        private readonly string _tempDir = IgniteUtils.GetTempDirectoryName();

        /// <summary>
        /// Tests the data storage metrics.
        /// </summary>
        [Test]
        public void TestDataStorageMetrics()
        {
            var timeout = TimeSpan.FromSeconds(1);  // 1 second is the minimum allowed.

            var cfg = new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                DataStorageConfiguration = new DataStorageConfiguration
                {
                    CheckpointFrequency = timeout,
                    MetricsRateTimeInterval = timeout,
                    DefaultDataRegionConfiguration = new DataRegionConfiguration
                    {
                        PersistenceEnabled = true,
                        Name = "foobar",
                        MetricsRateTimeInterval = timeout
                    }
                },
                WorkDirectory = _tempDir
            };

            using (var ignite = Ignition.Start(cfg))
            {
                ignite.SetActive(true);

                var cache = ignite.CreateCache<int, string>("c");

                cache.PutAll(Enumerable.Range(1, 100000).ToDictionary(x => x, x => Guid.NewGuid().ToString()));

                // Wait for checkpoint and metrics update and verify.
                var metrics = ignite.GetDataStorageMetrics();

                Assert.IsTrue(TestUtils.WaitForCondition(() =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    metrics = ignite.GetDataStorageMetrics();

                    return metrics.LastCheckpointDataPagesNumber > 0;
                }, 5000));

                Assert.AreEqual(1, metrics.LastCheckpointTotalPagesNumber);
            }
        }

        /// <summary>
        /// Tears down the test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
