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
    using Apache.Ignite.Linq;
    using NUnit.Framework;

    /// <summary>
    /// Tests LINQ in thin client.
    /// </summary>
    public class LinqTest : SqlQueryTestBase
    {
        /// <summary>
        /// Tests basic queries.
        /// </summary>
        [Test]
        public void TestBasicQueries()
        {
            var cache = GetClientCache<Person>();

            // All items.
            var qry = cache.AsCacheQueryable();
            Assert.AreEqual(Count, qry.Count());

            // Filter.
            qry = cache.AsCacheQueryable().Where(x => x.Value.Name.EndsWith("7"));
            Assert.AreEqual(7, qry.Single().Key);
            Assert.AreEqual("select _T0._KEY, _T0._VAL from \"cache\".PERSON as _T0 where (_T0.NAME like '%' || ?) ",
                qry.ToCacheQueryable().GetFieldsQuery().Sql);

            // DateTime.
            var arg = DateTime.UtcNow.AddDays(Count - 1);
            var qry2 = cache.AsCacheQueryable().Where(x => x.Value.DateTime > arg).Select(x => x.Key);
            Assert.AreEqual(Count, qry2.Single());
        }

        /// <summary>
        /// Tests joins.
        /// </summary>
        [Test]
        public void TestJoins()
        {
            // TODO: both syntax, distributed and not
            
        }
    }
}
