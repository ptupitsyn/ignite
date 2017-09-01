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

namespace Apache.Ignite.Core.Tests.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Apache.Ignite.Core.Cache;
    using NUnit.Framework;

    /// <summary>
    /// Thin client cache test.
    /// </summary>
    public class CacheTest
    {
        /** Cache name. */
        private const string CacheName = "cache";

        /// <summary>
        /// Fixture tear down.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            Ignition.Start(TestUtils.GetTestConfiguration());
        }

        /// <summary>
        /// Fixture tear down.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            Ignition.StopAll(true);
        }

        /// <summary>
        /// Tests the cache put / get with primitive data types.
        /// </summary>
        [Test]
        public void TestPutGetPrimitives()
        {
            using (var client = Ignition.GetClient())
            {
                GetCache<string>().Put(1, "foo");

                var clientCache = client.GetCache<int, string>(CacheName);

                // Existing key.
                Assert.AreEqual("foo", clientCache.Get(1));
                Assert.AreEqual("foo", clientCache[1]);

                // Missing key.
                Assert.Throws<KeyNotFoundException>(() => clientCache.Get(2));
            }
        }

        /// <summary>
        /// Tests the cache put / get with user data types.
        /// </summary>
        [Test]
        public void TestPutGetUserObjects()
        {
            using (var client = Ignition.GetClient())
            {
                var person = new Person {Id = 100, Name = "foo"};
                var person2 = new Person2 {Id = 200, Name = "bar"};
                
                var serverCache = GetCache<Person>();
                var clientCache = client.GetCache<int, Person>(CacheName);

                // Put through server cache.
                serverCache.Put(1, person);

                // Put through client cache.
                clientCache.Put(2, person2);
                clientCache[3] = person2;

                // Read from client cache.
                Assert.AreEqual("foo", clientCache.Get(1).Name);
                Assert.AreEqual(100, clientCache[1].Id);
                Assert.AreEqual(200, clientCache[2].Id);
                Assert.AreEqual(200, clientCache[3].Id);

                // Read from server cache.
                Assert.AreEqual("foo", serverCache.Get(1).Name);
                Assert.AreEqual(100, serverCache[1].Id);
                Assert.AreEqual(200, serverCache[2].Id);
                Assert.AreEqual(200, serverCache[3].Id);
            }
        }

        /// <summary>
        /// Tests client get in multiple threads with a single client.
        /// </summary>
        [Test]
        [Category(TestUtils.CategoryIntensive)]
        public void TestGetMultithreadedSingleClient()
        {
            GetCache<string>().Put(1, "foo");

            using (var client = Ignition.GetClient())
            {
                var clientCache = client.GetCache<int, string>(CacheName);

                TestUtils.RunMultiThreaded(() => Assert.AreEqual("foo", clientCache.Get(1)),
                    Environment.ProcessorCount, 5);
            }
        }

        /// <summary>
        /// Tests client get in multiple threads with multiple clients.
        /// </summary>
        [Test]
        [Category(TestUtils.CategoryIntensive)]
        public void TestGetMultithreadedMultiClient()
        {
            GetCache<string>().Put(1, "foo");

            // One client per thread.
            ConcurrentDictionary<int, IIgnite> clients = new ConcurrentDictionary<int, IIgnite>();

            TestUtils.RunMultiThreaded(() =>
                {
                    var client = clients.GetOrAdd(Thread.CurrentThread.ManagedThreadId, _ => Ignition.GetClient());

                    var clientCache = client.GetCache<int, string>(CacheName);

                    Assert.AreEqual("foo", clientCache.Get(1));
                },
                Environment.ProcessorCount, 5);

            clients.ToList().ForEach(x => x.Value.Dispose());
        }

        /// <summary>
        /// Gets the cache.
        /// </summary>
        private static ICache<int, T> GetCache<T>()
        {
            return Ignition.GetIgnite().GetOrCreateCache<int, T>(CacheName);
        }
    }
}
