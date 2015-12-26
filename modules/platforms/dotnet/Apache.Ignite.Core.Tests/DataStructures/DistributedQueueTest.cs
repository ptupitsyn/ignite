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
            // Nonexistent long returns null
            Assert.IsNull(Grid.GetQueue<int>(QueueName, 10, null));
        }
    }
}
