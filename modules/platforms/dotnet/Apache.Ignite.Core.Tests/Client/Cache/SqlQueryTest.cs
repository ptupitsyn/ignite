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

namespace Apache.Ignite.Core.Tests.Client.Cache
{
    using System.Linq;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Cache.Query;
    using NUnit.Framework;

    /// <summary>
    /// Tests SQL queries via thin client.
    /// </summary>
    public class SqlQueryTest : ClientTestBase
    {
        /// <summary>
        /// Cache item count.
        /// </summary>
        private const int Count = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanQueryTest"/> class.
        /// </summary>
        public SqlQueryTest() : base(2)
        {
            // No-op.
        }

        /// <summary>
        /// Sets up the test.
        /// </summary>
        public override void TestSetUp()
        {
            var cache = Ignition.GetIgnite().GetOrCreateCache<int, Person>(
                new CacheConfiguration(CacheName, typeof(Person)));

            cache.RemoveAll();

            cache.PutAll(Enumerable.Range(1, Count).ToDictionary(x => x, x => new Person(x)));
        }

        /// <summary>
        /// Tests the SQL query.
        /// </summary>
        [Test]
        public void TestSqlQuery()
        {
            var cache = GetClientCache<Person>();

            // All items.
            var qry = new SqlQuery(typeof(Person), "where 1 = 1");
            Assert.AreEqual(Count, cache.Query(qry).Count());

            // Filter.
            qry.Sql = "where Name like '%7'";
            Assert.AreEqual(7, cache.Query(qry).Single().Key);

            // Args.
            qry = new SqlQuery(typeof(Person), "where Id = ?", 3);
            Assert.AreEqual(3, cache.Query(qry).Single().Value.Id);
        }

        /// <summary>
        /// Tests the fields query.
        /// </summary>
        [Test]
        public void TestFieldsQuery()
        {
        }
    }
}
