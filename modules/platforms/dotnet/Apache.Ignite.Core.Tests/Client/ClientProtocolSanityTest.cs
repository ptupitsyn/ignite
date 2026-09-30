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

namespace Apache.Ignite.Core.Tests.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Cache.Event;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Client;
    using Apache.Ignite.Core.Client.Cache;
    using Apache.Ignite.Core.Client.Cache.Query.Continuous;
    using Apache.Ignite.Core.Client.Datastream;
    using Apache.Ignite.Core.Client.DataStructures;
    using Apache.Ignite.Core.Log;
    using Apache.Ignite.Core.Platform;
    using Apache.Ignite.Core.Services;
    using Apache.Ignite.Core.Transactions;
    using Apache.Ignite.Linq;
    using NUnit.Framework;

    /// <summary>
    /// Protocol sanity test for the .NET thin client against an external cluster, for example a GridGain server.
    /// The test examines most of the protocol operations.
    /// <para />
    /// The test does not start a cluster. The cluster must be available at <see cref="Address"/> before the test
    /// starts. The cluster must have these items (IgniteClientCompatNodeRunnerTest in GridGain deploys them):
    /// <list type="bullet">
    /// <item><description>Compute tasks CompatEchoTask, CompatSleepTask, CompatFailTask.</description></item>
    /// <item><description>Service CompatService (CompatServiceImpl).</description></item>
    /// <item><description>Max active compute tasks per connection that is more than zero.</description></item>
    /// </list>
    /// The class skips itself unless the <see cref="RunFlag"/> environment variable is "true".
    /// </summary>
    public class ClientProtocolSanityTest
    {
        /** Environment variable that permits this class to run. */
        public const string RunFlag = "IGNITE_DOTNET_CLIENT_PROTOCOL_SANITY_TEST_RUN";

        /** Address of the cluster under test. */
        private const string Address = "127.0.0.1:10800";

        /** Prefix of all caches, atomic longs and sets. */
        private const string Prefix = "thinProtoSanityNet_";

        /** Cache that the plain cache operation tests share. The test clears it before each test. */
        private const string DefaultCache = Prefix + "cache";

        /** Table of the cache that the SQL tests use. */
        private const string QueryTable = "THIN_PROTO_SANITY";

        /** Value type of <see cref="QueryTable"/>. There is no .NET class, the tests use keep binary. */
        private const string QueryValueType = "ThinProtoSanityValue";

        /** Number of rows that the query tests insert. It is more than the page size, thus paging occurs. */
        private const int QueryRows = 10;

        /** Page size of the query tests. */
        private const int PageSize = 2;

        /** Class name of the task that gives back its argument. */
        private const string EchoTaskClass = "org.apache.ignite.client.CompatEchoTask";

        /** Task name of <see cref="EchoTaskClass"/>, from its ComputeTaskName annotation. */
        private const string EchoTaskName = "CompatEchoTask";

        /** Class name of the task that sleeps. */
        private const string SleepTaskClass = "org.apache.ignite.client.CompatSleepTask";

        /** Class name of the task that always fails. */
        private const string FailTaskClass = "org.apache.ignite.client.CompatFailTask";

        /** Error message of <see cref="FailTaskClass"/>. */
        private const string FailTaskErrorMessage = "Compat compute task failure.";

        /** Argument of <see cref="SleepTaskClass"/>, in milliseconds. It is longer than all waits of the tests. */
        private const long SleepTaskDuration = 30_000L;

        /** Timeout that the timeout test sets on the task. */
        private static readonly TimeSpan TaskTimeout = TimeSpan.FromMilliseconds(500);

        /** Time that a test waits for an asynchronous result. */
        private static readonly TimeSpan ResultWait = TimeSpan.FromSeconds(10);

        /** Name of the service that the service tests call. */
        private const string ServiceName = "CompatService";

        /** Error message of <see cref="ICompatService.fail"/>. */
        private const string ServiceErrorMessage = "Compat service failure.";

        /** Shared client. */
        private IIgniteClient _client;

        /// <summary>
        /// Skips the class when <see cref="RunFlag"/> is not set, else starts the shared client.
        /// </summary>
        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable(RunFlag), "true", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Ignore("The " + RunFlag + " environment variable is not true, the test is skipped.");
            }

            _client = Ignition.StartClient(GetClientConfiguration());
        }

        /// <summary>
        /// Removes all caches that the test made, then closes the shared client.
        /// </summary>
        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                foreach (var name in _client.GetCacheNames().Where(n => n.StartsWith(Prefix, StringComparison.Ordinal)))
                {
                    _client.DestroyCache(name);
                }
            }
            finally
            {
                _client.Dispose();
                _client = null;
            }
        }

        /// <summary>
        /// Clears the shared cache.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _client.GetOrCreateCache<object, object>(DefaultCache).RemoveAll();
        }

        /// <summary>
        /// Tested operations: handshake, <c>BinaryConfigurationGet</c>, <c>CacheGetNames</c>.
        /// </summary>
        [Test]
        public void TestHandshake()
        {
            using var client = Ignition.StartClient(GetClientConfiguration());

            Assert.IsNotNull(client.GetCacheNames());
            Assert.IsNotNull(client.RemoteEndPoint);
        }

        /// <summary>
        /// Tested operations: <c>Heartbeat</c> and <c>GetIdleTimeout</c>.
        /// </summary>
        [Test]
        public void TestHeartbeat()
        {
            var cfg = GetClientConfiguration();
            cfg.EnableHeartbeats = true;
            cfg.HeartbeatInterval = TimeSpan.FromMilliseconds(500);

            using var client = Ignition.StartClient(cfg);

            Thread.Sleep(1_500);

            Assert.IsNotNull(client.GetCacheNames());
        }

        /// <summary>
        /// Tested operation: <c>ResourceClose</c>. The client sends it only while the server keeps more pages.
        /// </summary>
        [Test]
        public void TestResourceClose()
        {
            var cache = FillCache(100);

            using (var cur = cache.Query(new ScanQuery<int, int> { PageSize = 1 }))
            {
                using var enumerator = cur.GetEnumerator();

                Assert.IsTrue(enumerator.MoveNext());
            }

            // The connection is still usable.
            Assert.IsNotNull(_client.GetCacheNames());
        }

        /// <summary>
        /// Tested operation: <c>CacheCreateWithName</c>.
        /// </summary>
        [Test]
        public void TestCacheCreateWithName()
        {
            var cache = _client.CreateCache<int, int>(CacheName());

            Assert.AreEqual(CacheName(), cache.Name);
        }

        /// <summary>
        /// Tested operation: <c>CacheGetOrCreateWithName</c>.
        /// </summary>
        [Test]
        public void TestCacheGetOrCreateWithName()
        {
            Assert.AreEqual(CacheName(), _client.GetOrCreateCache<int, int>(CacheName()).Name);

            // The second call gets the existing cache.
            Assert.AreEqual(CacheName(), _client.GetOrCreateCache<int, int>(CacheName()).Name);
        }

        /// <summary>
        /// Tested operation: <c>CacheCreateWithConfiguration</c>.
        /// </summary>
        [Test]
        public void TestCacheCreateWithConfiguration()
        {
            var cfg = new CacheClientConfiguration(CacheName()) { Backups = 1 };

            Assert.AreEqual(CacheName(), _client.CreateCache<int, int>(cfg).Name);
        }

        /// <summary>
        /// Tested operation: <c>CacheGetOrCreateWithConfiguration</c>.
        /// </summary>
        [Test]
        public void TestCacheGetOrCreateWithConfiguration()
        {
            var cfg = new CacheClientConfiguration(CacheName()) { Backups = 1 };

            Assert.AreEqual(CacheName(), _client.GetOrCreateCache<int, int>(cfg).Name);

            // The second call gets the existing cache.
            Assert.AreEqual(CacheName(), _client.GetOrCreateCache<int, int>(cfg).Name);
        }

        /// <summary>
        /// Tested operation: <c>CacheGetNames</c>.
        /// </summary>
        [Test]
        public void TestCacheGetNames()
        {
            _client.GetOrCreateCache<int, int>(CacheName());

            CollectionAssert.Contains(_client.GetCacheNames(), CacheName());
        }

        /// <summary>
        /// Tested operation: <c>CacheGetConfiguration</c>. The test compares many properties,
        /// because the configuration format has different fields in different versions.
        /// </summary>
        [Test]
        public void TestCacheGetConfiguration()
        {
            var cfg = new CacheClientConfiguration(CacheName())
            {
                Backups = 1,
                AtomicityMode = CacheAtomicityMode.Transactional,
                CacheMode = CacheMode.Partitioned,
                WriteSynchronizationMode = CacheWriteSynchronizationMode.FullSync,
                ReadFromBackup = false,
                CopyOnRead = false,
                EagerTtl = false,
                MaxQueryIteratorsCount = 7,
                QueryDetailMetricsSize = 3,
                QueryParallelism = 2,
                SqlSchema = "SANITY_SCHEMA",
                GroupName = Prefix + "group"
            };

            var res = _client.GetOrCreateCache<int, int>(cfg).GetConfiguration();

            Assert.AreEqual(CacheName(), res.Name);
            Assert.AreEqual(1, res.Backups);
            Assert.AreEqual(CacheAtomicityMode.Transactional, res.AtomicityMode);
            Assert.AreEqual(CacheMode.Partitioned, res.CacheMode);
            Assert.AreEqual(CacheWriteSynchronizationMode.FullSync, res.WriteSynchronizationMode);
            Assert.IsFalse(res.ReadFromBackup);
            Assert.IsFalse(res.CopyOnRead);
            Assert.IsFalse(res.EagerTtl);
            Assert.AreEqual(7, res.MaxQueryIteratorsCount);
            Assert.AreEqual(3, res.QueryDetailMetricsSize);
            Assert.AreEqual(2, res.QueryParallelism);
            Assert.AreEqual("SANITY_SCHEMA", res.SqlSchema);
            Assert.AreEqual(Prefix + "group", res.GroupName);
        }

        /// <summary>
        /// Tested operation: <c>CacheDestroy</c>.
        /// </summary>
        [Test]
        public void TestCacheDestroy()
        {
            _client.GetOrCreateCache<int, int>(CacheName());

            CollectionAssert.Contains(_client.GetCacheNames(), CacheName());

            _client.DestroyCache(CacheName());

            CollectionAssert.DoesNotContain(_client.GetCacheNames(), CacheName());
        }

        /// <summary>
        /// Tested operation: <c>CachePut</c>.
        /// </summary>
        [Test]
        public void TestCachePut()
        {
            var cache = GetDefaultCache();

            cache.Put(1, "1");

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheGet</c>.
        /// </summary>
        [Test]
        public void TestCacheGet()
        {
            var cache = GetDefaultCache();

            Assert.IsFalse(cache.TryGet(1, out _));
            Assert.Throws<KeyNotFoundException>(() => cache.Get(1));

            cache.Put(1, "1");

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheContainsKey</c>.
        /// </summary>
        [Test]
        public void TestCacheContainsKey()
        {
            var cache = GetDefaultCache();

            Assert.IsFalse(cache.ContainsKey(1));

            cache.Put(1, "1");

            Assert.IsTrue(cache.ContainsKey(1));
        }

        /// <summary>
        /// Tested operation: <c>CachePutIfAbsent</c>.
        /// </summary>
        [Test]
        public void TestCachePutIfAbsent()
        {
            var cache = GetDefaultCache();

            Assert.IsTrue(cache.PutIfAbsent(1, "1"));
            Assert.IsFalse(cache.PutIfAbsent(1, "2"));

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheGetAndPut</c>.
        /// </summary>
        [Test]
        public void TestCacheGetAndPut()
        {
            var cache = GetDefaultCache();

            Assert.IsFalse(cache.GetAndPut(1, "1").Success);
            Assert.AreEqual("1", cache.GetAndPut(1, "2").Value);

            Assert.AreEqual("2", cache.Get(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheGetAndPutIfAbsent</c>.
        /// </summary>
        [Test]
        public void TestCacheGetAndPutIfAbsent()
        {
            var cache = GetDefaultCache();

            Assert.IsFalse(cache.GetAndPutIfAbsent(1, "1").Success);
            Assert.AreEqual("1", cache.GetAndPutIfAbsent(1, "2").Value);

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheGetAndReplace</c>.
        /// </summary>
        [Test]
        public void TestCacheGetAndReplace()
        {
            var cache = GetDefaultCache();

            Assert.IsFalse(cache.GetAndReplace(1, "1").Success);
            Assert.IsFalse(cache.ContainsKey(1));

            cache.Put(1, "1");

            Assert.AreEqual("1", cache.GetAndReplace(1, "2").Value);
            Assert.AreEqual("2", cache.Get(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheGetAndRemove</c>.
        /// </summary>
        [Test]
        public void TestCacheGetAndRemove()
        {
            var cache = GetDefaultCache();

            Assert.IsFalse(cache.GetAndRemove(1).Success);

            cache.Put(1, "1");

            Assert.AreEqual("1", cache.GetAndRemove(1).Value);
            Assert.IsFalse(cache.ContainsKey(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheReplace</c>.
        /// </summary>
        [Test]
        public void TestCacheReplace()
        {
            var cache = GetDefaultCache();

            Assert.IsFalse(cache.Replace(1, "1"));

            cache.Put(1, "1");

            Assert.IsTrue(cache.Replace(1, "2"));
            Assert.AreEqual("2", cache.Get(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheReplaceIfEquals</c>.
        /// </summary>
        [Test]
        public void TestCacheReplaceIfEquals()
        {
            var cache = GetDefaultCache();

            cache.Put(1, "1");

            Assert.IsFalse(cache.Replace(1, "wrong", "2"));
            Assert.AreEqual("1", cache.Get(1));

            Assert.IsTrue(cache.Replace(1, "1", "2"));
            Assert.AreEqual("2", cache.Get(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheRemoveKey</c>.
        /// </summary>
        [Test]
        public void TestCacheRemoveKey()
        {
            var cache = GetDefaultCache();

            Assert.IsFalse(cache.Remove(1));

            cache.Put(1, "1");

            Assert.IsTrue(cache.Remove(1));
            Assert.IsFalse(cache.ContainsKey(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheRemoveIfEquals</c>.
        /// </summary>
        [Test]
        public void TestCacheRemoveIfEquals()
        {
            var cache = GetDefaultCache();

            cache.Put(1, "1");

            Assert.IsFalse(cache.Remove(1, "wrong"));
            Assert.IsTrue(cache.ContainsKey(1));

            Assert.IsTrue(cache.Remove(1, "1"));
            Assert.IsFalse(cache.ContainsKey(1));
        }

        /// <summary>
        /// Tested operation: <c>CacheClearKey</c>.
        /// </summary>
        [Test]
        public void TestCacheClearKey()
        {
            var cache = GetDefaultCache();

            cache.Put(1, "1");
            cache.Put(2, "2");

            cache.Clear(1);

            Assert.IsFalse(cache.ContainsKey(1));
            Assert.IsTrue(cache.ContainsKey(2));
        }

        /// <summary>
        /// Tested operation: <c>CachePutAll</c>.
        /// </summary>
        [Test]
        public void TestCachePutAll()
        {
            var cache = GetDefaultCache();

            var data = Enumerable.Range(0, 10).ToDictionary(x => x, x => x.ToString());

            cache.PutAll(data);

            var res = cache.GetAll(data.Keys).ToDictionary(e => e.Key, e => e.Value);

            CollectionAssert.AreEquivalent(data, res);
        }

        /// <summary>
        /// Tested operation: <c>CacheGetAll</c>.
        /// </summary>
        [Test]
        public void TestCacheGetAll()
        {
            var cache = GetDefaultCache();

            cache.Put(1, "1");
            cache.Put(2, "2");

            var res = cache.GetAll(new[] { 1, 2, 3 }).ToDictionary(e => e.Key, e => e.Value);

            Assert.AreEqual(2, res.Count);
            Assert.AreEqual("1", res[1]);
            Assert.AreEqual("2", res[2]);
        }

        /// <summary>
        /// Tested operation: <c>CacheContainsKeys</c>.
        /// </summary>
        [Test]
        public void TestCacheContainsKeys()
        {
            var cache = GetDefaultCache();

            var keys = new[] { 1, 2 };

            Assert.IsFalse(cache.ContainsKeys(keys));

            cache.Put(1, "1");
            cache.Put(2, "2");

            Assert.IsTrue(cache.ContainsKeys(keys));
        }

        /// <summary>
        /// Tested operation: <c>CacheClearKeys</c>.
        /// </summary>
        [Test]
        public void TestCacheClearKeys()
        {
            var cache = GetDefaultCache();

            cache.Put(1, "1");
            cache.Put(2, "2");
            cache.Put(3, "3");

            cache.ClearAll(new[] { 1, 2 });

            Assert.IsFalse(cache.ContainsKey(1));
            Assert.IsFalse(cache.ContainsKey(2));
            Assert.IsTrue(cache.ContainsKey(3));
        }

        /// <summary>
        /// Tested operation: <c>CacheRemoveKeys</c>.
        /// </summary>
        [Test]
        public void TestCacheRemoveKeys()
        {
            var cache = GetDefaultCache();

            cache.Put(1, "1");
            cache.Put(2, "2");
            cache.Put(3, "3");

            cache.RemoveAll(new[] { 1, 2 });

            Assert.IsFalse(cache.ContainsKey(1));
            Assert.IsFalse(cache.ContainsKey(2));
            Assert.IsTrue(cache.ContainsKey(3));
        }

        /// <summary>
        /// Tested operation: <c>CacheGetSize</c>.
        /// </summary>
        [Test]
        public void TestCacheGetSize()
        {
            var cache = GetDefaultCache();

            Assert.AreEqual(0, cache.GetSize());

            for (var i = 0; i < 10; i++)
            {
                cache.Put(i, i.ToString());
            }

            Assert.AreEqual(10, cache.GetSize());
            Assert.AreEqual(10, cache.GetSize(CachePeekMode.Primary));
        }

        /// <summary>
        /// Tested operation: <c>CacheClear</c>.
        /// </summary>
        [Test]
        public void TestCacheClear()
        {
            var cache = GetDefaultCache();

            cache.Put(1, "1");
            cache.Put(2, "2");

            cache.Clear();

            Assert.AreEqual(0, cache.GetSize());
        }

        /// <summary>
        /// Tested operation: <c>CacheRemoveAll</c>.
        /// </summary>
        [Test]
        public void TestCacheRemoveAll()
        {
            var cache = GetDefaultCache();

            cache.Put(1, "1");
            cache.Put(2, "2");

            cache.RemoveAll();

            Assert.AreEqual(0, cache.GetSize());
        }

        /// <summary>
        /// Tested operation: <c>CacheGet</c> and other key operations, through the async API.
        /// </summary>
        [Test]
        public async Task TestCacheAsyncOperations()
        {
            var cache = GetDefaultCache();

            await cache.PutAsync(1, "1");

            Assert.AreEqual("1", await cache.GetAsync(1));
            Assert.IsTrue(await cache.ContainsKeyAsync(1));
            Assert.AreEqual("1", (await cache.GetAndRemoveAsync(1)).Value);
            Assert.IsFalse((await cache.TryGetAsync(1)).Success);
        }

        /// <summary>
        /// Tested operation: <c>CachePut</c> with an expiry policy.
        /// </summary>
        [Test]
        public void TestCachePutWithExpiryPolicy()
        {
            var cache = GetDefaultCache();

            var ttl = TimeSpan.FromMilliseconds(300);

            cache.WithExpiryPolicy(new Core.Cache.Expiry.ExpiryPolicy(ttl, ttl, ttl)).Put(1, "1");

            Assert.IsTrue(cache.ContainsKey(1));

            TestUtils.WaitForTrueCondition(() => !cache.ContainsKey(1), 5_000);
        }

        /// <summary>
        /// Tested operation: <c>CachePartitions</c>. The client sends it when partition awareness is on.
        /// </summary>
        [Test]
        public void TestCachePartitions()
        {
            var cfg = GetClientConfiguration();
            cfg.EnablePartitionAwareness = true;

            using var client = Ignition.StartClient(cfg);

            var cache = client.GetOrCreateCache<int, int>(CacheName());

            for (var i = 0; i < 100; i++)
            {
                cache.Put(i, i);
            }

            for (var i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, cache.Get(i));
            }
        }

        /// <summary>
        /// Tested operations: <c>QueryScan</c> and <c>QueryScanCursorGetPage</c>.
        /// </summary>
        [Test]
        public void TestQueryScan()
        {
            var cache = FillCache(QueryRows);

            using var cur = cache.Query(new ScanQuery<int, int> { PageSize = PageSize });

            var res = cur.GetAll();

            Assert.AreEqual(QueryRows, res.Count);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, QueryRows), res.Select(e => e.Value));
        }

        /// <summary>
        /// Tested operations: <c>QuerySql</c> and <c>QuerySqlCursorGetPage</c>.
        /// </summary>
        [Test]
        public void TestQuerySql()
        {
            var cache = GetQueryCache().WithKeepBinary<int, IBinaryObject>();

#pragma warning disable 618
            var qry = new SqlQuery(QueryValueType, "B >= ?", 0) { PageSize = PageSize };

            using var cur = cache.Query(qry);
#pragma warning restore 618

            var res = cur.GetAll();

            Assert.AreEqual(QueryRows, res.Count);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, QueryRows), res.Select(e => e.Value.GetField<int>("B")));
        }

        /// <summary>
        /// Tested operations: <c>QuerySqlFields</c> and <c>QuerySqlFieldsCursorGetPage</c>.
        /// </summary>
        [Test]
        public void TestQuerySqlFields()
        {
            var cache = GetQueryCache();

            var qry = new SqlFieldsQuery("select A, B from " + QueryTable + " order by A") { PageSize = PageSize };

            using var cur = cache.Query(qry);

            var res = cur.GetAll();

            Assert.AreEqual(new[] { "A", "B" }, cur.FieldNames);
            Assert.AreEqual(QueryRows, res.Count);
            Assert.AreEqual(Enumerable.Range(0, QueryRows), res.Select(r => (int)r[1]));
        }

        /// <summary>
        /// Tested operation: <c>QuerySqlFields</c>, for DDL and DML with many column types.
        /// </summary>
        [Test]
        public void TestQuerySqlFieldsDdlDml()
        {
            var cache = _client.GetOrCreateCache<int, int>(DefaultCache);

            var table = "SANITY_DDL";

            // The default cache has no SQL schema, thus all queries use the PUBLIC schema.
            IFieldsQueryCursor Sql(string sql, params object[] args) =>
                cache.Query(new SqlFieldsQuery(sql, args) { Schema = "PUBLIC" });

            Sql("drop table if exists " + table).GetAll();

            Sql(
                "create table " + table + " (ID int primary key, NAME varchar, PRICE decimal(10, 2), " +
                "TS timestamp, UID uuid, FLAG boolean, DBL double) with \"cache_name=" + Prefix + "ddl\"").GetAll();

            try
            {
                var ts = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
                var uid = Guid.NewGuid();

                var inserted = Sql(
                    "insert into " + table + " (ID, NAME, PRICE, TS, UID, FLAG, DBL) values (?, ?, ?, ?, ?, ?, ?)",
                    1, "name", 12.34m, ts, uid, true, 1.5d).GetAll().Single()[0];

                Assert.AreEqual(1L, inserted);

                var row = Sql("select NAME, PRICE, TS, UID, FLAG, DBL from " + table + " where ID = ?", 1)
                    .GetAll().Single();

                Assert.AreEqual("name", row[0]);
                Assert.AreEqual(12.34m, row[1]);
                Assert.AreEqual(ts, row[2]);
                Assert.AreEqual(uid, row[3]);
                Assert.AreEqual(true, row[4]);
                Assert.AreEqual(1.5d, row[5]);

                var updated = Sql("update " + table + " set NAME = ? where ID = ?", "x", 1).GetAll().Single()[0];

                Assert.AreEqual(1L, updated);
            }
            finally
            {
                Sql("drop table if exists " + table).GetAll();
            }
        }

        /// <summary>
        /// Tested operation: <c>QuerySqlFields</c>, through LINQ.
        /// </summary>
        [Test]
        public void TestQueryLinq()
        {
            var cfg = new CacheClientConfiguration(CacheName(), new QueryEntity(typeof(int), typeof(SqlPerson)));

            var cache = _client.GetOrCreateCache<int, SqlPerson>(cfg);

            for (var i = 0; i < QueryRows; i++)
            {
                cache.Put(i, new SqlPerson { Id = i, Name = "Person " + i });
            }

            var qry = cache.AsCacheQueryable();

            Assert.AreEqual(QueryRows, qry.Count());
            Assert.AreEqual("Person 7", qry.Single(p => p.Value.Name.EndsWith("7")).Value.Name);
            Assert.AreEqual(QueryRows - 1, qry.Max(p => p.Value.Id));

            var ids = qry.Where(p => p.Value.Id > 5).OrderBy(p => p.Value.Id).Select(p => p.Value.Id).ToArray();
            Assert.AreEqual(new[] { 6, 7, 8, 9 }, ids);
        }

        /// <summary>
        /// Tested operations: <c>QueryContinuous</c> and <c>QueryContinuousEventNotification</c>.
        /// </summary>
        [Test]
        public void TestQueryContinuous()
        {
            var cache = _client.GetOrCreateCache<int, int>(CacheName());
            cache.RemoveAll();

            var events = new List<ICacheEntryEvent<int, int>>();
            var qry = new ContinuousQueryClient<int, int>(new Listener<int, int>(e =>
            {
                lock (events)
                {
                    events.Add(e);
                }
            }));

            using (cache.QueryContinuous(qry))
            {
                cache.Put(1, 1);
                cache.Put(1, 2);
                cache.Remove(1);

                TestUtils.WaitForTrueCondition(() =>
                {
                    lock (events)
                    {
                        return events.Count == 3;
                    }
                }, (int)ResultWait.TotalMilliseconds);
            }

            // The test does not depend on the sequence of the events. It finds each event by its type.
            lock (events)
            {
                var created = events.Single(e => e.EventType == CacheEntryEventType.Created);
                Assert.AreEqual(1, created.Key);
                Assert.AreEqual(1, created.Value);

                var updated = events.Single(e => e.EventType == CacheEntryEventType.Updated);
                Assert.AreEqual(1, updated.OldValue);
                Assert.AreEqual(2, updated.Value);

                var removed = events.Single(e => e.EventType == CacheEntryEventType.Removed);
                Assert.AreEqual(1, removed.Key);
            }
        }

        /// <summary>
        /// Tested operations: <c>BinaryTypePut</c> and <c>BinaryTypeNamePut</c>. A new client has no binary metadata,
        /// thus a put of a user object sends both operations.
        /// </summary>
        [Test]
        public void TestPutBinaryTypeAndRegisterBinaryTypeName()
        {
            using var client = Ignition.StartClient(GetClientConfiguration());

            var cache = client.GetOrCreateCache<int, Person>(CacheName());

            var person = new Person { Id = 1, Name = "Joe" };

            cache.Put(1, person);

            Assert.AreEqual(person, cache.Get(1));
        }

        /// <summary>
        /// Tested operation: <c>BinaryTypeGet</c>. One client writes the type, the other client reads it.
        /// </summary>
        [Test]
        public void TestGetBinaryType()
        {
            _client.GetOrCreateCache<int, Person>(CacheName()).Put(1, new Person { Id = 1, Name = "Joe" });

            using var client = Ignition.StartClient(GetClientConfiguration());

            var type = client.GetBinary().GetBinaryType(typeof(Person));

            Assert.IsNotNull(type);
            Assert.AreEqual(typeof(Person).FullName, type.TypeName);
            CollectionAssert.AreEquivalent(new[] { "Id", "Name" }, type.Fields);
            Assert.AreEqual(BinaryTypeNames.TypeNameInt, type.GetFieldTypeName("Id"));
            Assert.AreEqual(BinaryTypeNames.TypeNameString, type.GetFieldTypeName("Name"));
        }

        /// <summary>
        /// Tested operation: <c>BinaryTypeNameGet</c>. A new client reads a value of a type that it did not see.
        /// </summary>
        [Test]
        public void TestGetBinaryTypeName()
        {
            var person = new Person { Id = 1, Name = "Joe" };

            _client.GetOrCreateCache<int, Person>(CacheName()).Put(1, person);

            using var client = Ignition.StartClient(GetClientConfiguration());

            Assert.AreEqual(person, client.GetCache<int, Person>(CacheName()).Get(1));
        }

        /// <summary>
        /// Tested operations: <c>BinaryTypePut</c> and <c>BinaryTypeGet</c>, for an enum and a type with many field
        /// types. Wrong binary metadata can cause incorrect data and no error, thus the test compares all fields.
        /// </summary>
        [Test]
        public void TestBinaryAllFieldTypes()
        {
            var obj = AllTypes.Create();

            _client.GetOrCreateCache<int, AllTypes>(CacheName()).Put(1, obj);

            using var client = Ignition.StartClient(GetClientConfiguration());

            var res = client.GetCache<int, AllTypes>(CacheName()).Get(1);
            obj.AssertEqual(res);

            // Read the same object as binary: the field values must be the same.
            var bin = client.GetCache<int, AllTypes>(CacheName()).WithKeepBinary<int, IBinaryObject>().Get(1);

            Assert.AreEqual(typeof(AllTypes).FullName, bin.GetBinaryType().TypeName);
            Assert.AreEqual(obj.Int, bin.GetField<int>(nameof(AllTypes.Int)));
            Assert.AreEqual(obj.String, bin.GetField<string>(nameof(AllTypes.String)));
            Assert.AreEqual(obj.Guid, bin.GetField<Guid>(nameof(AllTypes.Guid)));
            Assert.AreEqual(obj.Enum, bin.GetField<IBinaryObject>(nameof(AllTypes.Enum)).Deserialize<TestEnum>());
            Assert.AreEqual(
                obj.Nested.Name,
                bin.GetField<IBinaryObject>(nameof(AllTypes.Nested)).GetField<string>(nameof(Person.Name)));

            var enumType = client.GetBinary().GetBinaryType(typeof(TestEnum));
            Assert.IsTrue(enumType.IsEnum);
        }

        /// <summary>
        /// Tested operations: <c>BinaryTypePut</c> and <c>CachePut</c>, for an object that the client builds.
        /// </summary>
        [Test]
        public void TestBinaryObjectBuilder()
        {
            var typeName = Prefix + "BuilderType";

            var obj = _client.GetBinary().GetBuilder(typeName)
                .SetField("A", 1)
                .SetField("B", "b")
                .Build();

            var cache = _client.GetOrCreateCache<int, object>(CacheName()).WithKeepBinary<int, IBinaryObject>();
            cache.Put(1, obj);

            using var client = Ignition.StartClient(GetClientConfiguration());

            var res = client.GetCache<int, object>(CacheName()).WithKeepBinary<int, IBinaryObject>().Get(1);

            Assert.AreEqual(typeName, res.GetBinaryType().TypeName);
            Assert.AreEqual(1, res.GetField<int>("A"));
            Assert.AreEqual("b", res.GetField<string>("B"));
        }

        /// <summary>
        /// Tested operations: <c>TxStart</c> and <c>TxEnd</c>, with commit.
        /// </summary>
        [Test]
        public void TestTxStartCommit()
        {
            var cache = GetTxCache();

            using (var tx = _client.GetTransactions().TxStart())
            {
                cache.Put(1, "1");

                tx.Commit();
            }

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested operations: <c>TxStart</c> and <c>TxEnd</c>, with rollback.
        /// </summary>
        [Test]
        public void TestTxStartRollback()
        {
            var cache = GetTxCache();

            cache.Put(1, "1");

            using (var tx = _client.GetTransactions().TxStart())
            {
                cache.Put(1, "2");

                tx.Rollback();
            }

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested operation: <c>TxStart</c>, with all concurrency modes, isolation levels, a timeout and a label.
        /// </summary>
        [Test]
        public void TestTxStartWithParameters()
        {
            var cache = GetTxCache();
            var key = 0;

            foreach (var concurrency in new[] { TransactionConcurrency.Optimistic, TransactionConcurrency.Pessimistic })
            {
                foreach (var isolation in new[]
                         {
                             TransactionIsolation.ReadCommitted,
                             TransactionIsolation.RepeatableRead,
                             TransactionIsolation.Serializable
                         })
                {
                    key++;

                    using (var tx = _client.GetTransactions().WithLabel("sanity")
                               .TxStart(concurrency, isolation, TimeSpan.FromSeconds(10)))
                    {
                        Assert.AreEqual(concurrency, tx.Concurrency);
                        Assert.AreEqual(isolation, tx.Isolation);

                        cache.Put(key, key.ToString());

                        tx.Commit();
                    }

                    Assert.AreEqual(key.ToString(), cache.Get(key), $"{concurrency} {isolation}");
                }
            }
        }

        /// <summary>
        /// Tested operation: <c>ClusterIsActive</c>.
        /// </summary>
        [Test]
        public void TestClusterGetState()
        {
            Assert.IsTrue(_client.GetCluster().IsActive());
        }

        /// <summary>
        /// Tested operation: <c>ClusterChangeState</c>.
        /// </summary>
        [Test]
        public void TestClusterChangeState()
        {
            var cluster = _client.GetCluster();

            cluster.SetActive(true);

            Assert.IsTrue(cluster.IsActive());
        }

        /// <summary>
        /// Tested operations: <c>ClusterGroupGetNodeIds</c> and <c>ClusterGroupGetNodesInfo</c>.
        /// </summary>
        [Test]
        public void TestClusterGroupNodes()
        {
            var nodes = _client.GetCluster().GetNodes();

            CollectionAssert.IsNotEmpty(nodes);

            foreach (var node in nodes)
            {
                Assert.AreNotEqual(Guid.Empty, node.Id);
                Assert.IsNotNull(node.Version);
                CollectionAssert.IsNotEmpty(node.Addresses);
                CollectionAssert.IsNotEmpty(node.Attributes);
            }

            CollectionAssert.IsNotEmpty(_client.GetCluster().ForServers().GetNodes());
        }

        /// <summary>
        /// Tested operation: <c>ClusterGroupGetNodesEndpoints</c>. The client sends it when cluster discovery is on.
        /// </summary>
        [Test]
        public void TestClusterGroupGetNodesEndpoints()
        {
            var cfg = GetClientConfiguration();
            cfg.EnablePartitionAwareness = true;
            cfg.EnableClusterDiscovery = true;

            using var client = Ignition.StartClient(cfg);

            var cache = client.GetOrCreateCache<int, int>(CacheName());
            cache.Put(1, 1);

            TestUtils.WaitForTrueCondition(() => client.GetConnections().Any(), (int)ResultWait.TotalMilliseconds);
        }

        /// <summary>
        /// Tested operation: <c>ClusterGetWalState</c>.
        /// </summary>
        [Test]
        public void TestClusterGetWalState()
        {
            _client.GetOrCreateCache<int, int>(CacheName());

            // The cluster has no persistence, thus the WAL is off.
            Assert.IsFalse(_client.GetCluster().IsWalEnabled(CacheName()));
        }

        /// <summary>
        /// Tested operation: <c>ServiceGetDescriptors</c>.
        /// </summary>
        [Test]
        public void TestServiceGetDescriptors()
        {
            var descs = _client.GetServices().GetServiceDescriptors();

            Assert.IsNotNull(descs);
            CollectionAssert.Contains(descs.Select(d => d.Name), ServiceName);
        }

        /// <summary>
        /// Tested operation: <c>ServiceGetDescriptor</c>.
        /// </summary>
        [Test]
        public void TestServiceGetDescriptor()
        {
            var desc = _client.GetServices().GetServiceDescriptor(ServiceName);

            Assert.AreEqual(ServiceName, desc.Name);
            Assert.IsNotNull(desc.ServiceClass);
            Assert.AreEqual(1, desc.TotalCount);
            Assert.IsNotNull(desc.OriginNodeId);
            Assert.AreEqual(PlatformType.Java, desc.PlatformType);
        }

        /// <summary>
        /// Tested operation: <c>ServiceInvoke</c>.
        /// </summary>
        [Test]
        public void TestServiceInvoke()
        {
            var svc = _client.GetServices().GetServiceProxy<ICompatService>(ServiceName);

            Assert.AreEqual("ping", svc.echo("ping"));
            Assert.AreEqual(5, svc.add(2, 3));
        }

        /// <summary>
        /// Tested operation: <c>ServiceInvoke</c>, for a method that gives back null.
        /// </summary>
        [Test]
        public void TestServiceInvokeNullResult()
        {
            var svc = _client.GetServices().GetServiceProxy<ICompatService>(ServiceName);

            Assert.IsNull(svc.echo(null));
        }

        /// <summary>
        /// Tested operation: <c>ServiceInvoke</c>, for a cluster group.
        /// </summary>
        [Test]
        public void TestServiceInvokeOnClusterGroup()
        {
            var svc = _client.GetCluster().ForServers().GetServices().GetServiceProxy<ICompatService>(ServiceName);

            Assert.AreEqual("ping", svc.echo("ping"));
        }

        /// <summary>
        /// Tested operation: <c>ServiceInvoke</c>, for a method that fails.
        /// </summary>
        [Test]
        public void TestServiceInvokeFailure()
        {
            var svc = _client.GetServices().GetServiceProxy<ICompatService>(ServiceName);

            var ex = Assert.Throws<IgniteClientException>(() => svc.fail());

            StringAssert.Contains(ServiceErrorMessage, ex.ToString());
        }

        /// <summary>
        /// Tested operation: <c>ServiceInvoke</c>, with a caller context.
        /// </summary>
        [Test]
        [Ignore("https://ggsystems.atlassian.net/browse/GG-51378")]
        public void TestServiceInvokeWithCallerContext()
        {
            var callCtx = new ServiceCallContextBuilder().Set("key", "value").Build();

            var svc = _client.GetServices().GetServiceProxy<ICompatService>(ServiceName, callCtx);

            Assert.AreEqual("ping", svc.echo("ping"));
        }

        /// <summary>
        /// Tested operations: <c>ComputeTaskExecute</c> and <c>ComputeTaskFinished</c>.
        /// </summary>
        [Test]
        public void TestComputeTaskExecute()
        {
            Assert.AreEqual("ping", _client.GetCompute().ExecuteJavaTask<string>(EchoTaskClass, "ping"));
        }

        /// <summary>
        /// Tested operation: <c>ComputeTaskExecute</c>, with the task name.
        /// </summary>
        [Test]
        public void TestComputeTaskExecuteByName()
        {
            Assert.AreEqual("ping", _client.GetCompute().ExecuteJavaTask<string>(EchoTaskName, "ping"));
        }

        /// <summary>
        /// Tested operations: <c>ComputeTaskExecute</c> and <c>ComputeTaskFinished</c>, through the async API.
        /// </summary>
        [Test]
        public void TestComputeTaskExecuteAsync()
        {
            var task = _client.GetCompute().ExecuteJavaTaskAsync<string>(EchoTaskClass, "ping");

            Assert.IsTrue(task.Wait(ResultWait));
            Assert.AreEqual("ping", task.Result);
        }

        /// <summary>
        /// Tested operation: <c>ComputeTaskExecute</c>, for a cluster group.
        /// </summary>
        [Test]
        public void TestComputeTaskExecuteOnClusterGroup()
        {
            var compute = _client.GetCluster().ForServers().GetCompute();

            Assert.AreEqual("ping", compute.ExecuteJavaTask<string>(EchoTaskClass, "ping"));
        }

        /// <summary>
        /// Tested operation: <c>ComputeTaskExecute</c>, with the no-failover flag.
        /// </summary>
        [Test]
        public void TestComputeTaskExecuteWithNoFailover()
        {
            var res = _client.GetCompute().WithNoFailover().ExecuteJavaTask<string>(EchoTaskClass, "ping");

            Assert.AreEqual("ping", res);
        }

        /// <summary>
        /// Tested operation: <c>ComputeTaskExecute</c>, with the no-result-cache flag.
        /// The task gives back null when the server keeps no job results.
        /// </summary>
        [Test]
        public void TestComputeTaskExecuteWithNoResultCache()
        {
            var res = _client.GetCompute().WithNoResultCache().ExecuteJavaTask<string>(EchoTaskClass, "ping");

            Assert.IsNull(res);
        }

        /// <summary>
        /// Tested operation: <c>ComputeTaskExecute</c>, with the keep-binary flag.
        /// </summary>
        [Test]
        public void TestComputeTaskExecuteWithKeepBinary()
        {
            var res = _client.GetCompute().WithKeepBinary().ExecuteJavaTask<string>(EchoTaskClass, "ping");

            Assert.AreEqual("ping", res);
        }

        /// <summary>
        /// Tested operation: <c>ResourceClose</c>, for a compute task.
        /// </summary>
        [Test]
        public void TestComputeTaskCancel()
        {
            using var cts = new CancellationTokenSource();

            var task = _client.GetCompute().ExecuteJavaTaskAsync<object>(SleepTaskClass, SleepTaskDuration, cts.Token);

            cts.Cancel();

            Assert.IsTrue(task.IsCanceled);

            // The connection is still usable.
            Assert.IsNotNull(_client.GetCacheNames());
            Assert.AreEqual("ping", _client.GetCompute().ExecuteJavaTask<string>(EchoTaskClass, "ping"));
        }

        /// <summary>
        /// Tested operation: <c>ComputeTaskExecute</c>, with a timeout.
        /// </summary>
        [Test]
        public void TestComputeTaskWithTimeout()
        {
            var compute = _client.GetCompute().WithTimeout(TaskTimeout);

            // The server stops the task, thus the client gets an error.
            var ex = Assert.Throws<AggregateException>(
                () => compute.ExecuteJavaTask<object>(SleepTaskClass, SleepTaskDuration));

            Assert.IsInstanceOf<IgniteClientException>(ex.GetInnermostException());
        }

        /// <summary>
        /// Tested operations: <c>ComputeTaskExecute</c> and <c>ComputeTaskFinished</c>, with a task that fails.
        /// </summary>
        [Test]
        public void TestComputeTaskFailure()
        {
            var ex = Assert.Throws<AggregateException>(() =>
                _client.GetCompute().ExecuteJavaTask<object>(FailTaskClass, null));

            var clientEx = ex.GetInnermostException();

            Assert.IsInstanceOf<IgniteClientException>(clientEx);
            StringAssert.Contains(FailTaskErrorMessage, clientEx.ToString());
        }

        /// <summary>
        /// Tested operations: <c>DataStreamerStart</c> and <c>DataStreamerAddData</c>.
        /// </summary>
        [Test]
        public void TestDataStreamer()
        {
            var cache = _client.GetOrCreateCache<int, int>(CacheName());

            var options = new DataStreamerClientOptions { AllowOverwrite = true };

            using (var streamer = _client.GetDataStreamer<int, int>(CacheName(), options))
            {
                for (var i = 0; i < 1000; i++)
                {
                    streamer.Add(i, i);
                }

                streamer.Flush();

                streamer.Remove(0);
            }

            Assert.AreEqual(999, cache.GetSize());
            Assert.AreEqual(500, cache.Get(500));
        }

        /// <summary>
        /// Tested operation: <c>AtomicLongCreate</c>.
        /// </summary>
        [Test]
        public void TestAtomicLongCreate()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 42, true);

            try
            {
                Assert.AreEqual(42, atomic.Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>AtomicLongCreate</c>, with a configuration.
        /// </summary>
        [Test]
        public void TestAtomicLongCreateWithConfiguration()
        {
            var cfg = new AtomicClientConfiguration
            {
                Backups = 1,
                CacheMode = CacheMode.Partitioned,
                GroupName = Prefix + "atomics"
            };

            var atomic = _client.GetAtomicLong(AtomicName(), cfg, 1, true);

            try
            {
                Assert.AreEqual(1, atomic.Read());

                // The group name is part of the name lookup key.
                Assert.IsNull(_client.GetAtomicLong(AtomicName(), 0, false));
                Assert.AreEqual(1, _client.GetAtomicLong(AtomicName(), cfg, 0, false).Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>AtomicLongExists</c>.
        /// </summary>
        [Test]
        public void TestAtomicLongExists()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 0, true);

            try
            {
                Assert.IsFalse(atomic.IsClosed());
            }
            finally
            {
                atomic.Close();
            }

            Assert.IsTrue(atomic.IsClosed());
        }

        /// <summary>
        /// Tested operation: <c>AtomicLongValueGet</c>.
        /// </summary>
        [Test]
        public void TestAtomicLongValueGet()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 7, true);

            try
            {
                Assert.AreEqual(7, atomic.Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>AtomicLongValueAddAndGet</c>. Increment and decrement also use it.
        /// </summary>
        [Test]
        public void TestAtomicLongValueAddAndGet()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 0, true);

            try
            {
                Assert.AreEqual(5, atomic.Add(5));
                Assert.AreEqual(6, atomic.Increment());
                Assert.AreEqual(5, atomic.Decrement());
                Assert.AreEqual(2, atomic.Add(-3));
                Assert.AreEqual(2, atomic.Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>AtomicLongValueGetAndSet</c>.
        /// </summary>
        [Test]
        public void TestAtomicLongValueGetAndSet()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 1, true);

            try
            {
                Assert.AreEqual(1, atomic.Exchange(2));
                Assert.AreEqual(2, atomic.Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>AtomicLongValueCompareAndSetAndGet</c>. The .NET client uses it for compare-exchange,
        /// not <c>AtomicLongValueCompareAndSet</c>.
        /// </summary>
        [Test]
        public void TestAtomicLongValueCompareAndSetAndGet()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 1, true);

            try
            {
                Assert.AreEqual(1, atomic.CompareExchange(2, 99));
                Assert.AreEqual(1, atomic.Read());

                Assert.AreEqual(1, atomic.CompareExchange(2, 1));
                Assert.AreEqual(2, atomic.Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>AtomicLongRemove</c>.
        /// </summary>
        [Test]
        public void TestAtomicLongRemove()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 1, true);

            atomic.Close();

            Assert.IsTrue(atomic.IsClosed());
            Assert.IsNull(_client.GetAtomicLong(AtomicName(), 0, false));
        }

        /// <summary>
        /// Tested operation: <c>SetGetOrCreate</c>.
        /// </summary>
        [Test]
        public void TestSetGetOrCreate()
        {
            var set = GetSet<string>();

            try
            {
                Assert.IsNotNull(set);
                Assert.AreEqual(SetName(), set.Name);

                // Without a configuration the client gets the existing set.
                Assert.IsNotNull(_client.GetIgniteSet<string>(SetName(), null));
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>SetExists</c>.
        /// </summary>
        [Test]
        public void TestSetExists()
        {
            var set = GetSet<string>();

            try
            {
                Assert.IsFalse(set.IsClosed);
            }
            finally
            {
                set.Close();
            }

            Assert.IsTrue(set.IsClosed);
        }

        /// <summary>
        /// Tested operation: <c>SetValueAdd</c>.
        /// </summary>
        [Test]
        public void TestSetValueAdd()
        {
            var set = GetSet<string>();

            try
            {
                Assert.IsTrue(set.Add("a"));
                Assert.IsFalse(set.Add("a"));
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>SetValueAddAll</c>.
        /// </summary>
        [Test]
        public void TestSetValueAddAll()
        {
            var set = GetSet<string>();

            try
            {
                set.UnionWith(new[] { "a", "b" });

                Assert.AreEqual(2, set.Count);
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>SetValueContains</c>.
        /// </summary>
        [Test]
        public void TestSetValueContains()
        {
            var set = GetSet<string>();

            try
            {
                set.Add("a");

                Assert.IsTrue(set.Contains("a"));
                Assert.IsFalse(set.Contains("b"));
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>SetValueContainsAll</c>.
        /// </summary>
        [Test]
        public void TestSetValueContainsAll()
        {
            var set = GetSet<string>();

            try
            {
                set.UnionWith(new[] { "a", "b" });

                Assert.IsTrue(set.IsSupersetOf(new[] { "a", "b" }));
                Assert.IsFalse(set.IsSupersetOf(new[] { "a", "c" }));
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>SetValueRemove</c>.
        /// </summary>
        [Test]
        public void TestSetValueRemove()
        {
            var set = GetSet<string>();

            try
            {
                set.Add("a");

                Assert.IsTrue(set.Remove("a"));
                Assert.IsFalse(set.Remove("a"));
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>SetValueRemoveAll</c>.
        /// </summary>
        [Test]
        public void TestSetValueRemoveAll()
        {
            var set = GetSet<string>();

            try
            {
                set.UnionWith(new[] { "a", "b", "c" });

                set.ExceptWith(new[] { "a", "b" });

                Assert.AreEqual(1, set.Count);
                Assert.IsTrue(set.Contains("c"));
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>SetValueRetainAll</c>.
        /// </summary>
        [Test]
        public void TestSetValueRetainAll()
        {
            var set = GetSet<string>();

            try
            {
                set.UnionWith(new[] { "a", "b", "c" });

                set.IntersectWith(new[] { "a", "b" });

                Assert.AreEqual(2, set.Count);
                Assert.IsFalse(set.Contains("c"));
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>SetSize</c>.
        /// </summary>
        [Test]
        public void TestSetSize()
        {
            var set = GetSet<string>();

            try
            {
                Assert.AreEqual(0, set.Count);

                set.UnionWith(new[] { "a", "b" });

                Assert.AreEqual(2, set.Count);
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>SetClear</c>.
        /// </summary>
        [Test]
        public void TestSetClear()
        {
            var set = GetSet<string>();

            try
            {
                set.UnionWith(new[] { "a", "b" });

                set.Clear();

                Assert.AreEqual(0, set.Count);
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operations: <c>SetIteratorStart</c> and <c>SetIteratorGetPage</c>. The page size is less than the
        /// number of items, thus paging occurs.
        /// </summary>
        [Test]
        public void TestSetIterator()
        {
            var set = GetSet<int>();

            try
            {
                set.UnionWith(Enumerable.Range(0, 10));

                set.PageSize = PageSize;

                CollectionAssert.AreEquivalent(Enumerable.Range(0, 10), set.ToList());
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested operation: <c>SetClose</c>.
        /// </summary>
        [Test]
        public void TestSetClose()
        {
            var set = GetSet<string>();

            set.Add("a");

            set.Close();

            Assert.IsTrue(set.IsClosed);
        }

        /// <summary>
        /// Gets the configuration for the cluster under test.
        /// </summary>
        private static IgniteClientConfiguration GetClientConfiguration() =>
            new IgniteClientConfiguration(Address)
            {
                Logger = new ConsoleLogger { MinLevel = LogLevel.Warn }
            };

        /// <summary>
        /// Gets the cache name of the current test.
        /// </summary>
        private static string CacheName() => Prefix + TestContext.CurrentContext.Test.Name;

        /// <summary>
        /// Gets the atomic long name of the current test.
        /// </summary>
        private static string AtomicName() => Prefix + TestContext.CurrentContext.Test.Name;

        /// <summary>
        /// Gets the set name of the current test.
        /// </summary>
        private static string SetName() => Prefix + TestContext.CurrentContext.Test.Name;

        /// <summary>
        /// Makes the set of the current test. Sets and atomic longs share the default data structure cache group,
        /// and the server refuses caches with different backups in one group. The default backups of a set (0) and of
        /// an atomic long (1) are different, thus the set uses the backups of the atomic long.
        /// </summary>
        private IIgniteSetClient<T> GetSet<T>() =>
            _client.GetIgniteSet<T>(
                SetName(),
                new CollectionClientConfiguration { Backups = AtomicClientConfiguration.DefaultBackups });

        /// <summary>
        /// Gets the shared cache of the plain cache operation tests. It is empty at the start of each test.
        /// </summary>
        private ICacheClient<int, string> GetDefaultCache() => _client.GetCache<int, string>(DefaultCache);

        /// <summary>
        /// Makes the cache of the current test and puts the given number of entries into it.
        /// </summary>
        private ICacheClient<int, int> FillCache(int count)
        {
            var cache = _client.GetOrCreateCache<int, int>(CacheName());

            cache.RemoveAll();
            cache.PutAll(Enumerable.Range(0, count).ToDictionary(x => x, x => x));

            return cache;
        }

        /// <summary>
        /// Makes the transactional cache of the current test.
        /// </summary>
        private ICacheClient<int, string> GetTxCache() =>
            _client.GetOrCreateCache<int, string>(new CacheClientConfiguration(CacheName())
            {
                AtomicityMode = CacheAtomicityMode.Transactional
            });

        /// <summary>
        /// Makes the cache of the SQL tests and puts the data into it.
        /// </summary>
        private ICacheClient<int, object> GetQueryCache()
        {
            var qryEntity = new QueryEntity
            {
                TableName = QueryTable,
                KeyType = typeof(int),
                ValueTypeName = QueryValueType,
                KeyFieldName = "A",
                Fields = new[]
                {
                    new QueryField("A", typeof(int)),
                    new QueryField("B", typeof(int))
                },
                Indexes = new[] { new QueryIndex("B") }
            };

            var cache = _client.GetOrCreateCache<int, object>(new CacheClientConfiguration(CacheName(), qryEntity));

            cache.RemoveAll();

            for (var i = 0; i < QueryRows; i++)
            {
                cache.Query(new SqlFieldsQuery("insert into " + QueryTable + "(A, B) values (?, ?)", i, i)).GetAll();
            }

            return cache;
        }

        /// <summary>
        /// Proxy interface of the Java service CompatServiceImpl. The method names are the same as in Java.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public interface ICompatService
        {
            /** Gives back the value. */
            string echo(string val);

            /** Gives back the sum of the two values. */
            int add(int a, int b);

            /** Always throws an error. */
            void fail();
        }

        /// <summary>
        /// Continuous query listener that calls a delegate for each event.
        /// </summary>
        private sealed class Listener<TK, TV> : ICacheEntryEventListener<TK, TV>
        {
            /** */
            private readonly Action<ICacheEntryEvent<TK, TV>> _action;

            /** */
            public Listener(Action<ICacheEntryEvent<TK, TV>> action) => _action = action;

            /** <inheritdoc /> */
            public void OnEvent(IEnumerable<ICacheEntryEvent<TK, TV>> events)
            {
                foreach (var evt in events)
                {
                    _action(evt);
                }
            }
        }

        /// <summary>
        /// User type for the binary tests.
        /// </summary>
        public sealed class Person : IEquatable<Person>
        {
            /** */
            public int Id { get; set; }

            /** */
            public string Name { get; set; }

            /** <inheritdoc /> */
            public bool Equals(Person other) => other != null && Id == other.Id && Name == other.Name;

            /** <inheritdoc /> */
            public override bool Equals(object obj) => Equals(obj as Person);

            /** <inheritdoc /> */
            public override int GetHashCode() => HashCode.Combine(Id, Name);
        }

        /// <summary>
        /// User type for the LINQ test.
        /// </summary>
        public sealed class SqlPerson
        {
            /** */
            [QuerySqlField]
            public int Id { get; set; }

            /** */
            [QuerySqlField]
            public string Name { get; set; }
        }

        /// <summary>
        /// User enum for the binary tests.
        /// </summary>
        public enum TestEnum
        {
            /** */
            A,

            /** */
            B,

            /** */
            C
        }

        /// <summary>
        /// User type with many field types for the binary tests.
        /// </summary>
        public sealed class AllTypes
        {
            /** */
            public byte Byte { get; set; }

            /** */
            public short Short { get; set; }

            /** */
            public int Int { get; set; }

            /** */
            public long Long { get; set; }

            /** */
            public float Float { get; set; }

            /** */
            public double Double { get; set; }

            /** */
            public decimal Decimal { get; set; }

            /** */
            public bool Bool { get; set; }

            /** */
            public char Char { get; set; }

            /** */
            public string String { get; set; }

            /** */
            public Guid Guid { get; set; }

            /** */
            public DateTime DateTime { get; set; }

            /** */
            public int? NullableInt { get; set; }

            /** */
            public TestEnum Enum { get; set; }

            /** */
            public int[] IntArray { get; set; }

            /** */
            public string[] StringArray { get; set; }

            /** */
            public Guid[] GuidArray { get; set; }

            /** */
            public List<int> List { get; set; }

            /** */
            public Dictionary<string, int> Dictionary { get; set; }

            /** */
            public Person Nested { get; set; }

            /** */
            public object Obj { get; set; }

            /// <summary>
            /// Makes an instance with values in all fields.
            /// </summary>
            public static AllTypes Create() => new AllTypes
            {
                Byte = 1,
                Short = -2,
                Int = int.MaxValue,
                Long = long.MinValue,
                Float = 1.25f,
                Double = -3.5d,
                Decimal = 1234567.891m,
                Bool = true,
                Char = 'Ж',
                String = "Строка 文字",
                Guid = Guid.NewGuid(),
                DateTime = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc),
                NullableInt = null,
                Enum = TestEnum.C,
                IntArray = new[] { 1, 2, 3 },
                StringArray = new[] { "a", null, "c" },
                GuidArray = new[] { Guid.NewGuid(), Guid.Empty },
                List = new List<int> { 4, 5 },
                Dictionary = new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 },
                Nested = new Person { Id = 9, Name = "Nested" },
                Obj = new Person { Id = 10, Name = "Obj" }
            };

            /// <summary>
            /// Compares all fields with the other instance.
            /// </summary>
            public void AssertEqual(AllTypes other)
            {
                Assert.IsNotNull(other);
                Assert.AreEqual(Byte, other.Byte);
                Assert.AreEqual(Short, other.Short);
                Assert.AreEqual(Int, other.Int);
                Assert.AreEqual(Long, other.Long);
                Assert.AreEqual(Float, other.Float);
                Assert.AreEqual(Double, other.Double);
                Assert.AreEqual(Decimal, other.Decimal);
                Assert.AreEqual(Bool, other.Bool);
                Assert.AreEqual(Char, other.Char);
                Assert.AreEqual(String, other.String);
                Assert.AreEqual(Guid, other.Guid);
                Assert.AreEqual(DateTime, other.DateTime);
                Assert.AreEqual(NullableInt, other.NullableInt);
                Assert.AreEqual(Enum, other.Enum);
                Assert.AreEqual(IntArray, other.IntArray);
                Assert.AreEqual(StringArray, other.StringArray);
                Assert.AreEqual(GuidArray, other.GuidArray);
                Assert.AreEqual(List, other.List);
                CollectionAssert.AreEquivalent(Dictionary, other.Dictionary);
                Assert.AreEqual(Nested, other.Nested);
                Assert.AreEqual(Obj, other.Obj);
            }
        }
    }
}
