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

namespace Apache.Ignite.Core.Tests.Client.Cache
{
    using System;
    using System.Linq;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Query;
    using NUnit.Framework;

    /// <summary>
    /// Tests scan queries.
    /// </summary>
    public class ScanQueryTest : ClientTestBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScanQueryTest"/> class.
        /// </summary>
        public ScanQueryTest() : base(2)
        {
            // No-op.
        }

        /// <summary>
        /// Tests scan query without filter.
        /// </summary>
        [Test]
        public void TestNoFilter()
        {
            var cache = GetPersonCache();

            using (var client = GetClient())
            {
                var clientCache = client.GetCache<int, Person>(CacheName);

                var query = new ScanQuery<int, Person>();

                // GetAll.
                var cursor = clientCache.Query(query);
                var res = cursor.GetAll().Select(x => x.Value.Name).OrderBy(x => x).ToArray();
                Assert.AreEqual(cache.Select(x => x.Value.Name).OrderBy(x => x).ToArray(), res);

                // Calling GetAll twice is not allowed.
                Assert.Throws<InvalidOperationException>(() => cursor.GetAll());

                // Iterator.
                using (cursor = clientCache.Query(query))
                {
                    res = cursor.Select(x => x.Value.Name).OrderBy(x => x).ToArray();
                    Assert.AreEqual(cache.Select(x => x.Value.Name).OrderBy(x => x).ToArray(), res);

                    // Can't use GetAll after using iterator.
                    Assert.Throws<InvalidOperationException>(() => cursor.GetAll());
                }

                // Partial iterator.
                using (cursor = clientCache.Query(query))
                {
                    var item = cursor.First();
                    Assert.AreEqual(item.Key.ToString(), item.Value.Name);
                }

                // Local.
                query.Local = true;
                var localRes = clientCache.Query(query).GetAll();
                Assert.Less(localRes.Count, cache.GetSize());
            }
        }

        /// <summary>
        /// Gets the string cache.
        /// </summary>
        private static ICache<int, Person> GetPersonCache()
        {
            var cache = GetCache<Person>();

            cache.RemoveAll();
            cache.PutAll(Enumerable.Range(1, 10000).ToDictionary(x => x, x => new Person
            {
                Id = x,
                Name = x.ToString()
            }));

            return cache;
        }
    }
}
