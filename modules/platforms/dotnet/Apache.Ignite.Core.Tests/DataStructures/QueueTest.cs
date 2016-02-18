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

namespace Apache.Ignite.Core.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Cluster;
    using Apache.Ignite.Core.Common;
    using Apache.Ignite.Core.Configuration;
    using Apache.Ignite.Core.DataStructures;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Binary.IO;
    using NUnit.Framework;

    /// <summary>
    /// Tests for <see cref="IQueue{T}"/>.
    /// </summary>
    public class QueueTest : IgniteTestBase
    {
        /** */
        private const string QueueName = "igniteQueueTest";

        /** */
        private const string InternalCacheTask = "org.apache.ignite.platform.PlatformInternalCacheTask";

        public QueueTest() : base("config\\compute\\compute-grid1.xml", "config\\compute\\compute-grid2.xml")
        {
            // No-op.
        }

        /// <summary>
        /// Before the test.
        /// </summary>
        [SetUp]
        public void BeforeTest()
        {
            var q = Grid.GetQueue<int>(QueueName, 0, null);

            if (q != null && !q.IsClosed())
                q.Close();
        }

        /// <summary>
        /// Tests create and close.
        /// </summary>
        [Test]
        public void TestCreateClose()
        {
            // Nonexistent queue returns null
            Assert.IsNull(Grid.GetQueue<int>(QueueName, 10, null));

            // Create new
            var q1 = Grid.GetQueue<int>(QueueName, 0, new CollectionConfiguration());
            Assert.IsTrue(q1.TryAdd(10));
            Assert.AreEqual(new[] { 10 }, q1.ToArray());
            Assert.AreEqual(QueueName, q1.Name);

            // Get existing
            var q2 = Grid.GetQueue<int>(QueueName, 0, null);
            Assert.AreEqual(QueueName, q2.Name);
            Assert.AreEqual(new[] {10}, q2.ToArray());
            
            // Get existing with different configuration
            Assert.Throws<IgniteException>(() => Grid.GetQueue<int>(QueueName, 3, new CollectionConfiguration()));

            q2.Close();

            Assert.IsTrue(q1.IsClosed());
            Assert.IsTrue(q2.IsClosed());
            Assert.IsNull(Grid.GetQueue<int>(QueueName, 10, null));
        }

        /// <summary>
        /// Tests the serializable item.
        /// </summary>
        [Test]
        public void TestSerializable()
        {
            var q = Grid.GetQueue<NodeFilter>(QueueName, 0, new CollectionConfiguration());

            var obj = new NodeFilter {AllowedNode = Guid.NewGuid()};

            Assert.IsTrue(q.TryAdd(obj));

            Assert.AreEqual(obj.AllowedNode, q.Single().AllowedNode);
        }

        /// <summary>
        /// Tests the binarizable item.
        /// </summary>
        [Test]
        public void TestBinarizable()
        {
            var q = Grid.GetQueue<Binarizable>(QueueName, 0, new CollectionConfiguration());

            var obj = new Binarizable {Foo = 99};

            Assert.IsTrue(q.TryAdd(obj));

            Assert.AreEqual(obj.Foo, q.Single().Foo);
        }

        /// <summary>
        /// Tests the enumerator.
        /// </summary>
        [Test]
        public void TestEnumerator()
        {
            var q = Grid.GetQueue<int>(QueueName, 0, new CollectionConfiguration());

            //q.TryAddAll()
        }


        /// <summary>
        /// Tests the configuration.
        /// </summary>
        [Test]
        public void TestConfiguration()
        {
            // New cache is created when there is a new Queue 
            // with different cache settings than all existing data structures

            // Get all cache configs
            var origConfigs = GetCacheConfigurations(Grid).ToArray();

            // Create a new queue
            var cfg = new CollectionConfiguration
            {
                CacheMode = CacheMode.Local,
                AtomicityMode = CacheAtomicityMode.Transactional,
                Backups = 3,
                OffHeapMaxMemory = 1024*1024,
                MemoryMode = CacheMemoryMode.OffheapValues,
                NodeFilter = new NodeFilter { AllowedNode = Grid.GetCluster().GetLocalNode().Id }
            };

            var q = Grid.GetQueue<int>(Guid.NewGuid().ToString(), 0, cfg);

            // Get configs again and find the new one
            var cacheConfig = GetCacheConfigurations(Grid).Single(x => origConfigs.All(y => x.Name != y.Name));

            q.Close();

            // Validate config
            Assert.AreEqual(cfg.CacheMode, cacheConfig.CacheMode);
            Assert.AreEqual(cfg.AtomicityMode, cacheConfig.AtomicityMode);
            Assert.AreEqual(cfg.Backups, cacheConfig.Backups);
            Assert.AreEqual(cfg.OffHeapMaxMemory, cacheConfig.OffHeapMaxMemory);
            Assert.AreEqual(cfg.MemoryMode, cacheConfig.MemoryMode);

            // Validate node deployment
            var remoteCaches = GetCacheConfigurations(Grid2).Select(x => x.Name).ToArray();
            Assert.IsFalse(remoteCaches.Contains(cacheConfig.Name));
        }

        /// <summary>
        /// Gets all cache configurations.
        /// </summary>
        private static IEnumerable<CacheConfiguration> GetCacheConfigurations(IIgnite grid)
        {
            var data = grid.GetCompute().ExecuteJavaTask<byte[]>(InternalCacheTask, null);

            using (var stream = new BinaryHeapStream(data))
            {
                var size = stream.ReadInt();

                var reader = BinaryUtils.Marshaller.StartUnmarshal(stream);

                for (var i = 0; i < size; i++)
                {
                    yield return new CacheConfiguration(reader);
                }
            }
        }

        /** <inheritdoc /> */
        protected override IgniteConfiguration GetConfiguration(string springConfigUrl)
        {
            var cfg = base.GetConfiguration(springConfigUrl);

            cfg.BinaryConfiguration = new BinaryConfiguration(typeof(Binarizable));

            return cfg;
        }

        /// <summary>
        /// Test node filter.
        /// </summary>
        [Serializable]
        private class NodeFilter : IClusterNodeFilter
        {
            /// <summary>
            /// Gets or sets the allowed node.
            /// </summary>
            public Guid AllowedNode { get; set; }

            /** <inheritdoc /> */
            public bool Invoke(IClusterNode node)
            {
                return node.Id == AllowedNode;
            }
        }

        /// <summary>
        /// Test binarizable.
        /// </summary>
        private class Binarizable
        {
            /// <summary>
            /// Gets or sets the foo.
            /// </summary>
            public int Foo { get; set; }
        }
    }
}
