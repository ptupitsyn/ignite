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

// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Apache.Ignite.Core.Tests.Cache.Query
{
    using System.Linq;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Configuration;
    using NUnit.Framework;

    /// <summary>
    /// Tests queries with in-code configuration.
    /// </summary>
    public class CacheQueriesCodeConfigurationTest
    {
        const string CacheName = "personCache";

        /// <summary>
        /// Tests the SQL query.
        /// </summary>
        [Test]
        public void TestQueryEntityConfiguration()
        {

            var cfg = new IgniteConfiguration
            {
                JvmOptions = TestUtils.TestJavaOptions(),
                JvmClasspath = TestUtils.CreateTestClasspath(),
                BinaryConfiguration = new BinaryConfiguration(typeof (QueryPerson)),
                CacheConfiguration = new[]
                {
                    new CacheConfiguration
                    {
                        Name = CacheName,
                        QueryEntities = new[]
                        {
                            new QueryEntity
                            {
                                KeyType = typeof (int),
                                ValueType = typeof (QueryPerson),
                                Fields = new[]
                                {
                                    new QueryField("Name", typeof (string)),
                                    new QueryField("Age", typeof (int))
                                },
                                Indexes = new[]
                                {
                                    new QueryIndex("Name", QueryIndexType.FullText), new QueryIndex("Age")
                                }
                            }
                        }
                    }
                }
            };

            using (var ignite = Ignition.Start(cfg))
            {
                var cache = ignite.GetCache<int, QueryPerson>(CacheName);

                Assert.IsNotNull(cache);

                cache[1] = new QueryPerson("Arnold", 10);
                cache[2] = new QueryPerson("John", 20);

                using (var cursor = cache.Query(new SqlQuery(typeof (QueryPerson), "age > 10")))
                {
                    Assert.AreEqual(2, cursor.GetAll().Single().Key);
                }

                using (var cursor = cache.Query(new TextQuery(typeof (QueryPerson), "Ar*")))
                {
                    Assert.AreEqual(1, cursor.GetAll().Single().Key);
                }
            }
        }

        [Test]
        public void TestAttributeConfiguration()
        {
            var cfg = new IgniteConfiguration
            {
                JvmOptions = TestUtils.TestJavaOptions(),
                JvmClasspath = TestUtils.CreateTestClasspath(),
                BinaryConfiguration = new BinaryConfiguration(typeof(AttributeQueryPerson)),
                CacheConfiguration = new[]
                {
                    new CacheConfiguration
                    {
                        Name = CacheName,
                        QueryEntities = new[]
                        {
                            new QueryEntity
                            {
                                KeyType = typeof (int),
                                ValueType = typeof (QueryPerson)
                                // Fields/indexes will be populated from attributes
                            }
                        }
                    }
                }
            };

            // TODO: Test nested types

            using (var ignite = Ignition.Start(cfg))
            {
                var cache = ignite.GetOrCreateCache<int, AttributeQueryPerson>(CacheName);

                Assert.IsNotNull(cache);

                cache[1] = new AttributeQueryPerson("Arnold", 10)
                {
                    Address = new AttributeQueryAddress {Country = "USA", Street = "Pine Tree road"}
                };

                cache[2] = new AttributeQueryPerson("John", 20);

                using (var cursor = cache.Query(new SqlQuery(typeof(AttributeQueryPerson), "age > 10")))
                {
                    Assert.AreEqual(2, cursor.GetAll().Single().Key);
                }

                using (var cursor = cache.Query(new SqlQuery(typeof(AttributeQueryPerson), "Address.Country = 'USA'")))
                {
                    Assert.AreEqual(1, cursor.GetAll().Single().Key);
                }

                using (var cursor = cache.Query(new TextQuery(typeof(AttributeQueryPerson), "Ar*")))
                {
                    Assert.AreEqual(1, cursor.GetAll().Single().Key);
                }
            }
        }

        /// <summary>
        /// Person.
        /// </summary>
        private class AttributeQueryPerson
        {
            public AttributeQueryPerson(string name, int age)
            {
                Name = name;
                Age = age;
            }

            [QueryField(IsIndexed = true, IndexType = QueryIndexType.FullText)]
            public string Name { get; set; }

            [QueryField]
            public int Age { get; set; }

            [QueryField]
            public AttributeQueryAddress Address { get; set; }

            [QueryField]  // test recursive
            public AttributeQueryPerson Parent { get; set; }
        }

        private class AttributeQueryAddress
        {
            [QueryField]
            public string Country { get; set; }

            [QueryField]
            public string Street { get; set; }
        }
    }
}
