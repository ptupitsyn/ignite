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

namespace Apache.Ignite.Core.Tests.Cache.Query
{
    using System.Linq;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Query;
    using NUnit.Framework;

    public sealed class CacheQueriesRestartServerTest
    {
        private IIgnite _client;

        private IIgnite _server;

        [SetUp]
        public void BeforeTest()
        {
            _server = StartGrid(0);
            _client = StartGrid(0, true);

            TestUtils.WaitForCondition(() => _server.GetCluster().GetNodes().Count == 2, 1000);
        }

        [Test]
        public void Test([Values(true, false)] bool emptyFilterObject)
        {
            var cache = _client.GetOrCreateCache<int, Item>("Test");
            cache.Put(1, new Item { Id = 10, Title = "test" });

            Ignition.Stop(_server.Name, false);
            _server = StartGrid(0);
            _client.GetCluster().ClientReconnectTask.Wait();

            cache = _client.GetOrCreateCache<int, Item>("Test");
            cache.Put(1, new Item { Id = 10, Title = "test" });

            var filter = emptyFilterObject
                ? (ICacheEntryFilter<int, Item>) new TestFilter()
                : new TestFilterWithField {TestValue = 9};

            var cursor = cache.Query(new ScanQuery<int, Item>(filter));
            var items = cursor.GetAll();

            Assert.AreEqual(10, items.Single().Value.Id);
        }

        [TearDown]
        public void AfterTest()
        {
            Ignition.StopAll(true);
        }

        private IIgnite StartGrid(int i, bool client = false)
        {
            return Ignition.Start(new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                BinaryConfiguration = new BinaryConfiguration
                {
                    NameMapper = GetNameMapper()
                },

                ClientMode = client,

                IgniteInstanceName = client ? "client-" + i : "grid-" + i
            });
        }

        private IBinaryNameMapper GetNameMapper()
        {
            return new BinaryBasicNameMapper {IsSimpleName = false};
        }

        private class TestFilter : ICacheEntryFilter<int, Item>
        {
            public bool Invoke(ICacheEntry<int, Item> entry)
            {
                return entry.Value.Id > 10;
            }
        }

        private class TestFilterWithField : ICacheEntryFilter<int, Item>
        {
            public int TestValue { get; set; }

            public bool Invoke(ICacheEntry<int, Item> entry)
            {
                return entry.Value.Id > TestValue;
            }
        }

        private class Item : IBinarizable
        {
            public int Id { get; set; }

            public string Title { get; set; }

            public void WriteBinary(IBinaryWriter writer)
            {
                writer.WriteInt("Id", Id);
                writer.WriteString("Title", Title);
            }

            public void ReadBinary(IBinaryReader reader)
            {
                Id = reader.ReadInt("Id");
                Title = reader.ReadString("Title");
            }
        }
    }
}
