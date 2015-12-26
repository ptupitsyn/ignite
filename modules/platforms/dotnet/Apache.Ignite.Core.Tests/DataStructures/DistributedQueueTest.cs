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
    using Apache.Ignite.Core.Configuration;
    using Apache.Ignite.Core.DataStructures;
    using NUnit.Framework;

    /// <summary>
    /// Tests for <see cref="IDistributedQueue{T}"/>.
    /// </summary>
    public class DistributedQueueTest : IgniteTestBase
    {
        private const string QueueName = "igniteQueueTest";

        public DistributedQueueTest() : base("config\\compute\\compute-grid1.xml")
        {
            // No-op.
        }

        [Test]
        public void TestCreateClose()
        {
            // Nonexistent queue returns null
            Assert.IsNull(Grid.GetQueue<int>(QueueName, 10, null));

            // Create new
            var q1 = Grid.GetQueue<int>(QueueName, 0, new CollectionConfiguration());
            Assert.IsTrue(q1.TryAdd(10));
            Assert.AreEqual(QueueName, q1.Name);

            // Get existing
            var q2 = Grid.GetQueue<int>(QueueName, 0, null);
            Assert.AreEqual(QueueName, q2.Name);
            Assert.AreEqual(new[] {10}, q2.ToArray());
            
            // Get existing with configuration
            var q3 = Grid.GetQueue<int>(QueueName, 3, new CollectionConfiguration());
            Assert.AreEqual(QueueName, q3.Name);
            Assert.AreEqual(new[] {10}, q3.ToArray());

            q3.Close();

            Assert.IsTrue(q1.IsClosed());
            Assert.IsTrue(q2.IsClosed());
            Assert.IsTrue(q3.IsClosed());
            Assert.IsNull(Grid.GetQueue<int>(QueueName, 10, null));
        }
    }
}
