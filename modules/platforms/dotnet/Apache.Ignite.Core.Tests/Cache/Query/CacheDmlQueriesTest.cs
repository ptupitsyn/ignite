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
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Cache.Query;
    using NUnit.Framework;

    /// <summary>
    /// Tests Data Manipulation Language queries.
    /// </summary>
    public class CacheDmlQueriesTest
    {
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            //Environment.SetEnvironmentVariable("IGNITE_H2_DEBUG_CONSOLE", "true");

            var cfg = new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                BinaryConfiguration = new BinaryConfiguration(typeof (Key), typeof(Foo))
            };

            Ignition.Start(cfg);
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            //Thread.Sleep(Timeout.Infinite);

            Ignition.StopAll(true);
        }

        /// <summary>
        /// Tests primitive key.
        /// </summary>
        [Test]
        public void TestPrimitiveKey()
        {
            var cfg = new CacheConfiguration("primitive_key", new QueryEntity(typeof(int), typeof(Foo)));
            var cache = Ignition.GetIgnite().CreateCache<int, Foo>(cfg);

            var res = cache.QueryFields(new SqlFieldsQuery("insert into foo(_key, id, name) " +
                                                           "values (1, 2, 'John'), (2, 3, 'Mary')")).GetAll();

            //var res = cache.QueryFields(new SqlFieldsQuery("insert into foo(_key, id, name) " +
            //                                               "values (?, ?, ?), (?, ?, ?)",
            //    1, 2, "John", 3, 4, "Mary")).GetAll();

            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(1, res[0].Count);
            Assert.AreEqual(2, res[0][0]);  // 2 affected rows

            var foos = cache.OrderBy(x => x.Key).ToArray();

            Assert.AreEqual(2, foos.Length);
        }

        /// <summary>
        /// Tests composite key (which requires QueryField.IsKeyField).
        /// </summary>
        [Test]
        public void TestCompositeKey()
        {
            var cfg = new CacheConfiguration("composite_key", new QueryEntity(typeof(Key), typeof(Foo)));
            var cache = Ignition.GetIgnite().CreateCache<int, Foo>(cfg);

            var res = cache.QueryFields(new SqlFieldsQuery("insert into foo(lo, hi, id, name) " +
                                               "values (1, 2, 3, 'John'), (4, 5, 6, 'Mary')")).GetAll();

            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(1, res[0].Count);
            Assert.AreEqual(2, res[0][0]);  // 2 affected rows

            var foos = cache.OrderBy(x => x.Key).ToArray();

            Assert.AreEqual(2, foos.Length);
        }

        [Test]
        public void TestInvalidCompositeKey()
        {
            // TODO: Misconfigured key
        }

        [Test]
        public void TestBinaryMode()
        {
            // TODO: Create new cache, use binary-only mode?
        }

        private class Key
        {
            [QuerySqlField] public int Lo { get; set; }
            [QuerySqlField] public int Hi { get; set; }
        }

        private class Foo
        {
            [QuerySqlField] public int Id { get; set; }
            [QuerySqlField] public string Name { get; set; }
        }
    }
}
