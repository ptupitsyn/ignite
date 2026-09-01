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
    using System.Linq;
    using System.Net.Sockets;
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
    using Apache.Ignite.Core.Client.DataStructures;
    using Apache.Ignite.Core.Client.Services;
    using Apache.Ignite.Core.Services;
    using Apache.Ignite.Core.Tests.Client.Cache;
    using NUnit.Framework;

    /// <summary>
    /// Protocol sanity test for the .NET thin client. It has one test for each <c>ClientOp</c> code that a plain
    /// running cluster can reach, one test per operation where the operation maps to a distinct API call.
    /// <para />
    /// Like its Java counterpart, this class does <b>not</b> start a cluster. It connects to a cluster that is
    /// already running on <see cref="Addr"/>. When nothing answers on that address, the whole fixture is skipped, so
    /// a full test run does not fail because of it.
    /// <para />
    /// Cluster prerequisites:
    /// <list type="bullet">
    /// <item>Reachable on <see cref="Addr"/> and active.</item>
    /// <item>Indexing module present - the SQL tests need it.</item>
    /// <item>Data in memory, without persistence - the WAL state test looks for a cache group with WAL off.</item>
    /// <item>Thin client compute on, with the tasks named by <see cref="EchoTaskCls"/>, <see cref="SleepTaskCls"/> and
    /// <see cref="FailTaskCls"/> on the server classpath, deployed under their task names.</item>
    /// <item>A service with the methods of <see cref="ICompatService"/>, deployed under the name
    /// <see cref="SvcName"/>.</item>
    /// </list>
    /// Every cache, atomic long and set that this test makes has a name that starts with <see cref="Prefix"/>, and
    /// is removed again when the fixture completes.
    /// <para />
    /// The .NET thin client protocol surface is a subset of the Java one. These operations have no .NET test for
    /// that reason:
    /// <list type="bullet">
    /// <item><c>QueryIndex</c>, <c>QueryIndexCursorGetPage</c> - the .NET client has no index query API.</item>
    /// <item><c>CachePutAllConflict</c>, <c>CacheRemoveAllConflict</c> - the .NET client has no data replication
    /// API.</item>
    /// <item>Cache entry processor ops - the .NET client has no <c>Invoke</c>/<c>InvokeAll</c> API.</item>
    /// <item><c>ServiceGetTopology</c> - used only as an internal side effect of partition awareness.</item>
    /// <item><c>ClusterChangeWalState</c> - it changes the state of the cluster, and WAL goes off only for a cache
    /// with persistence.</item>
    /// <item><c>ClusterGetDcNodes</c> - the .NET client has no such op at all.</item>
    /// <item>A warm-up stop op - the .NET client has no such API at all.</item>
    /// <item><c>AtomicLongValueCompareAndSet</c> - <c>AtomicLongClient</c> never sends the plain form, only
    /// <c>AtomicLongValueCompareAndSetAndGet</c> through <see cref="IAtomicLongClient.CompareExchange"/>. This is
    /// the mirror image of the Java client, which only ever sends the plain form.</item>
    /// <item><c>CachePartitions</c>, <c>ClusterGroupGetNodesEndpoints</c>, <c>BinaryConfigurationGet</c> - the
    /// client sends these on its own account, not in answer to an API call.</item>
    /// </list>
    /// The .NET services client also has no per-call timeout, unlike the Java one, so there is no separate test for
    /// it - the plain <see cref="TestServiceInvoke"/> already covers the wire format of the timeout field.
    /// </summary>
    public class ThinClientProtocolSanityTest
    {
        /** Address of the cluster under test. */
        private const string Addr = "127.0.0.1:10800";

        /** Host part of <see cref="Addr"/>. */
        private const string Host = "127.0.0.1";

        /** Port part of <see cref="Addr"/>. */
        private const int Port = 10800;

        /** Timeout of the connect probe that decides whether the cluster is there at all, in milliseconds. */
        private const int ProbeTimeoutMs = 3000;

        /** Prefix of every cache, atomic long and set that this test makes. */
        private const string Prefix = "thinProtoSanity_";

        /** Cache that the plain cache operation tests share. It is cleared before each test. */
        private const string DfltCacheName = Prefix + "cache";

        /** Table of the cache that the SQL tests share. */
        private const string QryTbl = "THIN_PROTO_SANITY_NET";

        /** Value type of <see cref="QryTbl"/>. There is no .NET class behind it, so its cache is used with keep
         * binary for the SQL test. */
        private const string QryValType = "ThinProtoSanityValue";

        /** Number of rows that the query tests insert. Bigger than any page size they use, so paging is forced. */
        private const int QryRows = 10;

        /** Class name of the task that gives its argument back. The compute tests execute it. */
        private const string EchoTaskCls = "org.apache.ignite.client.CompatEchoTask";

        /** Task name of <see cref="EchoTaskCls"/>. */
        private const string EchoTaskName = "CompatEchoTask";

        /** Class name of the task that stays busy. The cancel and the timeout test execute it. */
        private const string SleepTaskCls = "org.apache.ignite.client.CompatSleepTask";

        /** Class name of the task that always fails. */
        private const string FailTaskCls = "org.apache.ignite.client.CompatFailTask";

        /** Message that <see cref="FailTaskCls"/> puts into its error. */
        private const string FailTaskErrMsg = "Compat compute task failure.";

        /** Argument of <see cref="SleepTaskCls"/>, in milliseconds. Longer than any wait of the compute tests. */
        private const long SleepTaskDurationMs = 30_000L;

        /** Timeout that the timeout test puts on the task. */
        private static readonly TimeSpan TaskTimeout = TimeSpan.FromMilliseconds(500);

        /** Name of the service that the service tests call. */
        private const string SvcName = "CompatService";

        /** Message that a call to the failing method of <see cref="SvcName"/> puts into its error. */
        private const string SvcErrMsg = "Compat service failure.";

        /** Backups for every set this test makes. Sets and atomic longs share the same default cache group unless
         * told otherwise, and every cache in a group must agree on its backup count. This must match
         * <see cref="AtomicClientConfiguration.DefaultBackups"/>, or whichever kind runs first in a given session
         * decides the group's backup count and the other kind fails with a "Backups mismatch" error. */
        private const int SetBackups = AtomicClientConfiguration.DefaultBackups;

        /** Client that the tests share. Tests that need their own configuration open a short-lived one instead. */
        private static IIgniteClient _client;

        /// <summary>
        /// Skips the fixture when no cluster answers on <see cref="Addr"/>, otherwise opens the shared client.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            if (!ClusterAvailable())
            {
                Assert.Ignore("No Ignite cluster on " + Addr + ", skipping protocol sanity test.");
            }

            _client = Ignition.StartClient(GetClientConfiguration());
        }

        /// <summary>
        /// Removes everything the test made, then closes the shared client.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                foreach (var name in _client.GetCacheNames())
                {
                    if (name.StartsWith(Prefix, StringComparison.Ordinal))
                    {
                        _client.DestroyCache(name);
                    }
                }
            }
            finally
            {
                _client.Dispose();
                _client = null;
            }
        }

        /// <summary>
        /// Puts the shared cache back into a known empty state.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _client.GetOrCreateCache<object, object>(DfltCacheName).RemoveAll();
        }

        /// <summary>
        /// Tested op: Handshake. A client that finishes its constructor has completed the handshake, and the
        /// following call proves the channel is usable.
        /// </summary>
        [Test]
        public void TestHandshake()
        {
            using (var cli = Ignition.StartClient(GetClientConfiguration()))
            {
                Assert.IsNotNull(cli.GetCacheNames());
            }
        }

        /// <summary>
        /// Tested ops: Heartbeat and GetIdleTimeout. The latter is sent while the channel starts up. The sleep spans
        /// several heartbeat intervals, and the call afterwards shows the channel survived them.
        /// </summary>
        [Test]
        public void TestHeartbeat()
        {
            var cfg = GetClientConfiguration();
            cfg.EnableHeartbeats = true;
            cfg.HeartbeatInterval = TimeSpan.FromMilliseconds(500);

            using (var cli = Ignition.StartClient(cfg))
            {
                Thread.Sleep(1500);

                Assert.IsNotNull(cli.GetCacheNames());
            }
        }

        /// <summary>
        /// Tested op: ResourceClose. The query cursor sends it only while the server still holds pages, so the
        /// cursor is abandoned after a single entry.
        /// </summary>
        [Test]
        public void TestResourceClose()
        {
            var cache = FillCache(100);

            var cur = cache.Query(new ScanQuery<int, int> { PageSize = 1 });

            var it = cur.GetEnumerator();

            Assert.IsTrue(it.MoveNext());

            cur.Dispose();
        }

        /// <summary>
        /// Tested op: CacheCreateWithName.
        /// </summary>
        [Test]
        public void TestCacheCreateWithName()
        {
            var cache = _client.CreateCache<int, int>(CacheName());

            Assert.AreEqual(CacheName(), cache.Name);
        }

        /// <summary>
        /// Tested op: CacheGetOrCreateWithName.
        /// </summary>
        [Test]
        public void TestCacheGetOrCreateWithName()
        {
            Assert.AreEqual(CacheName(), _client.GetOrCreateCache<int, int>(CacheName()).Name);

            // Second call takes the existing cache.
            Assert.AreEqual(CacheName(), _client.GetOrCreateCache<int, int>(CacheName()).Name);
        }

        /// <summary>
        /// Tested op: CacheCreateWithConfiguration.
        /// </summary>
        [Test]
        public void TestCacheCreateWithConfiguration()
        {
            var ccfg = new CacheClientConfiguration(CacheName()) { Backups = 1 };

            Assert.AreEqual(CacheName(), _client.CreateCache<int, int>(ccfg).Name);
        }

        /// <summary>
        /// Tested op: CacheGetOrCreateWithConfiguration.
        /// </summary>
        [Test]
        public void TestCacheGetOrCreateWithConfiguration()
        {
            var ccfg = new CacheClientConfiguration(CacheName()) { Backups = 1 };

            Assert.AreEqual(CacheName(), _client.GetOrCreateCache<int, int>(ccfg).Name);

            // Second call takes the existing cache.
            Assert.AreEqual(CacheName(), _client.GetOrCreateCache<int, int>(ccfg).Name);
        }

        /// <summary>
        /// Tested op: CacheGetNames.
        /// </summary>
        [Test]
        public void TestCacheGetNames()
        {
            _client.GetOrCreateCache<int, int>(CacheName());

            Assert.IsTrue(_client.GetCacheNames().Contains(CacheName()));
        }

        /// <summary>
        /// Tested op: CacheGetConfiguration.
        /// </summary>
        [Test]
        public void TestCacheGetConfiguration()
        {
            var ccfg = new CacheClientConfiguration(CacheName()) { Backups = 1 };

            var readCfg = _client.GetOrCreateCache<int, int>(ccfg).GetConfiguration();

            Assert.AreEqual(CacheName(), readCfg.Name);
            Assert.AreEqual(1, readCfg.Backups);
        }

        /// <summary>
        /// Tested op: CacheDestroy.
        /// </summary>
        [Test]
        public void TestCacheDestroy()
        {
            _client.GetOrCreateCache<int, int>(CacheName());

            Assert.IsTrue(_client.GetCacheNames().Contains(CacheName()));

            _client.DestroyCache(CacheName());

            Assert.IsFalse(_client.GetCacheNames().Contains(CacheName()));
        }

        /// <summary>
        /// Tested op: CachePut.
        /// </summary>
        [Test]
        public void TestCachePut()
        {
            var cache = DfltCache<int, string>();

            cache.Put(1, "1");

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested op: CacheGet.
        /// </summary>
        [Test]
        public void TestCacheGet()
        {
            var cache = DfltCache<int, string>();

            string val;
            Assert.IsFalse(cache.TryGet(1, out val));

            cache.Put(1, "1");

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested op: CacheContainsKey.
        /// </summary>
        [Test]
        public void TestCacheContainsKey()
        {
            var cache = DfltCache<int, string>();

            Assert.IsFalse(cache.ContainsKey(1));

            cache.Put(1, "1");

            Assert.IsTrue(cache.ContainsKey(1));
        }

        /// <summary>
        /// Tested op: CachePutIfAbsent.
        /// </summary>
        [Test]
        public void TestCachePutIfAbsent()
        {
            var cache = DfltCache<int, string>();

            Assert.IsTrue(cache.PutIfAbsent(1, "1"));
            Assert.IsFalse(cache.PutIfAbsent(1, "2"));

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested op: CacheGetAndPut.
        /// </summary>
        [Test]
        public void TestCacheGetAndPut()
        {
            var cache = DfltCache<int, string>();

            Assert.IsFalse(cache.GetAndPut(1, "1").Success);
            Assert.AreEqual("1", cache.GetAndPut(1, "2").Value);

            Assert.AreEqual("2", cache.Get(1));
        }

        /// <summary>
        /// Tested op: CacheGetAndPutIfAbsent.
        /// </summary>
        [Test]
        public void TestCacheGetAndPutIfAbsent()
        {
            var cache = DfltCache<int, string>();

            Assert.IsFalse(cache.GetAndPutIfAbsent(1, "1").Success);
            Assert.AreEqual("1", cache.GetAndPutIfAbsent(1, "2").Value);

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested op: CacheGetAndReplace.
        /// </summary>
        [Test]
        public void TestCacheGetAndReplace()
        {
            var cache = DfltCache<int, string>();

            Assert.IsFalse(cache.GetAndReplace(1, "1").Success);

            cache.Put(1, "1");

            Assert.AreEqual("1", cache.GetAndReplace(1, "2").Value);
            Assert.AreEqual("2", cache.Get(1));
        }

        /// <summary>
        /// Tested op: CacheGetAndRemove.
        /// </summary>
        [Test]
        public void TestCacheGetAndRemove()
        {
            var cache = DfltCache<int, string>();

            Assert.IsFalse(cache.GetAndRemove(1).Success);

            cache.Put(1, "1");

            Assert.AreEqual("1", cache.GetAndRemove(1).Value);
            Assert.IsFalse(cache.ContainsKey(1));
        }

        /// <summary>
        /// Tested op: CacheReplace.
        /// </summary>
        [Test]
        public void TestCacheReplace()
        {
            var cache = DfltCache<int, string>();

            Assert.IsFalse(cache.Replace(1, "1"));

            cache.Put(1, "1");

            Assert.IsTrue(cache.Replace(1, "2"));
            Assert.AreEqual("2", cache.Get(1));
        }

        /// <summary>
        /// Tested op: CacheReplaceIfEquals.
        /// </summary>
        [Test]
        public void TestCacheReplaceIfEquals()
        {
            var cache = DfltCache<int, string>();

            cache.Put(1, "1");

            Assert.IsFalse(cache.Replace(1, "wrong", "2"));
            Assert.AreEqual("1", cache.Get(1));

            Assert.IsTrue(cache.Replace(1, "1", "2"));
            Assert.AreEqual("2", cache.Get(1));
        }

        /// <summary>
        /// Tested op: CacheRemoveKey.
        /// </summary>
        [Test]
        public void TestCacheRemoveKey()
        {
            var cache = DfltCache<int, string>();

            Assert.IsFalse(cache.Remove(1));

            cache.Put(1, "1");

            Assert.IsTrue(cache.Remove(1));
            Assert.IsFalse(cache.ContainsKey(1));
        }

        /// <summary>
        /// Tested op: CacheRemoveIfEquals.
        /// </summary>
        [Test]
        public void TestCacheRemoveIfEquals()
        {
            var cache = DfltCache<int, string>();

            cache.Put(1, "1");

            Assert.IsFalse(cache.Remove(1, "wrong"));
            Assert.IsTrue(cache.ContainsKey(1));

            Assert.IsTrue(cache.Remove(1, "1"));
            Assert.IsFalse(cache.ContainsKey(1));
        }

        /// <summary>
        /// Tested op: CacheClearKey.
        /// </summary>
        [Test]
        public void TestCacheClearKey()
        {
            var cache = DfltCache<int, string>();

            cache.Put(1, "1");
            cache.Put(2, "2");

            cache.Clear(1);

            Assert.IsFalse(cache.ContainsKey(1));
            Assert.IsTrue(cache.ContainsKey(2));
        }

        /// <summary>
        /// Tested op: CachePutAll.
        /// </summary>
        [Test]
        public void TestCachePutAll()
        {
            var cache = DfltCache<int, string>();

            var data = new Dictionary<int, string>();

            for (var i = 0; i < 10; i++)
            {
                data[i] = i.ToString();
            }

            cache.PutAll(data);

            var res = cache.GetAll(data.Keys).ToDictionary(e => e.Key, e => e.Value);

            Assert.AreEqual(data.Count, res.Count);

            foreach (var kv in data)
            {
                Assert.AreEqual(kv.Value, res[kv.Key]);
            }
        }

        /// <summary>
        /// Tested op: CacheGetAll.
        /// </summary>
        [Test]
        public void TestCacheGetAll()
        {
            var cache = DfltCache<int, string>();

            cache.Put(1, "1");
            cache.Put(2, "2");

            var res = cache.GetAll(new[] { 1, 2, 3 }).ToDictionary(e => e.Key, e => e.Value);

            Assert.AreEqual(2, res.Count);
            Assert.AreEqual("1", res[1]);
            Assert.AreEqual("2", res[2]);
        }

        /// <summary>
        /// Tested op: CacheContainsKeys.
        /// </summary>
        [Test]
        public void TestCacheContainsKeys()
        {
            var cache = DfltCache<int, string>();

            var keys = new[] { 1, 2 };

            Assert.IsFalse(cache.ContainsKeys(keys));

            cache.Put(1, "1");
            cache.Put(2, "2");

            Assert.IsTrue(cache.ContainsKeys(keys));
        }

        /// <summary>
        /// Tested op: CacheClearKeys.
        /// </summary>
        [Test]
        public void TestCacheClearKeys()
        {
            var cache = DfltCache<int, string>();

            cache.Put(1, "1");
            cache.Put(2, "2");
            cache.Put(3, "3");

            cache.ClearAll(new[] { 1, 2 });

            Assert.IsFalse(cache.ContainsKey(1));
            Assert.IsFalse(cache.ContainsKey(2));
            Assert.IsTrue(cache.ContainsKey(3));
        }

        /// <summary>
        /// Tested op: CacheRemoveKeys.
        /// </summary>
        [Test]
        public void TestCacheRemoveKeys()
        {
            var cache = DfltCache<int, string>();

            cache.Put(1, "1");
            cache.Put(2, "2");
            cache.Put(3, "3");

            cache.RemoveAll(new[] { 1, 2 });

            Assert.IsFalse(cache.ContainsKey(1));
            Assert.IsFalse(cache.ContainsKey(2));
            Assert.IsTrue(cache.ContainsKey(3));
        }

        /// <summary>
        /// Tested op: CacheGetSize.
        /// </summary>
        [Test]
        public void TestCacheGetSize()
        {
            var cache = DfltCache<int, string>();

            Assert.AreEqual(0L, cache.GetSize());

            for (var i = 0; i < 10; i++)
            {
                cache.Put(i, i.ToString());
            }

            Assert.AreEqual(10L, cache.GetSize());
            Assert.AreEqual(10L, cache.GetSize(CachePeekMode.Primary));
        }

        /// <summary>
        /// Tested op: CacheClear.
        /// </summary>
        [Test]
        public void TestCacheClear()
        {
            var cache = DfltCache<int, string>();

            cache.Put(1, "1");
            cache.Put(2, "2");

            cache.Clear();

            Assert.AreEqual(0L, cache.GetSize());
        }

        /// <summary>
        /// Tested op: CacheRemoveAll.
        /// </summary>
        [Test]
        public void TestCacheRemoveAll()
        {
            var cache = DfltCache<int, string>();

            cache.Put(1, "1");
            cache.Put(2, "2");

            cache.RemoveAll();

            Assert.AreEqual(0L, cache.GetSize());
        }

        /// <summary>
        /// Tested ops: QueryScan and QueryScanCursorGetPage. The query carries no filter, so nothing has to be
        /// deployed on the server. The page size is smaller than the entry count, which forces the paging
        /// operation.
        /// </summary>
        [Test]
        public void TestQueryScan()
        {
            var cache = FillCache(QryRows);

            using (var cur = cache.Query(new ScanQuery<int, int> { PageSize = 2 }))
            {
                Assert.AreEqual(QryRows, cur.GetAll().Count);
            }
        }

        /// <summary>
        /// Tested ops: QuerySql and QuerySqlCursorGetPage.
        /// </summary>
        [Test]
        public void TestQuerySql()
        {
            var cache = QueryCache<int, object>().WithKeepBinary<int, IBinaryObject>();

#pragma warning disable 618
            var qry = new SqlQuery(QryValType, "B >= ?", 0) { PageSize = 2 };

            using (var cur = cache.Query(qry))
            {
                Assert.AreEqual(QryRows, cur.GetAll().Count);
            }
#pragma warning restore 618
        }

        /// <summary>
        /// Tested ops: QuerySqlFields and QuerySqlFieldsCursorGetPage.
        /// </summary>
        [Test]
        public void TestQuerySqlFields()
        {
            var cache = QueryCache<int, object>();

            var qry = new SqlFieldsQuery("select A, B from " + QryTbl + " order by A") { PageSize = 2 };

            using (var cur = cache.Query(qry))
            {
                Assert.AreEqual(QryRows, cur.GetAll().Count);
            }
        }

        /// <summary>
        /// Tested ops: QueryContinuous and QueryContinuousEventNotification. Only a local listener is set, so no
        /// remote filter needs deploying. Reaching the wait handle proves the notification was decoded.
        /// </summary>
        [Test]
        public void TestQueryContinuous()
        {
            var cache = _client.GetOrCreateCache<int, int>(CacheName());

            using (var latch = new ManualResetEventSlim(false))
            {
                var qry = new ContinuousQueryClient<int, int>(new EntryEventListener<int, int>(() => latch.Set()));

                using (cache.QueryContinuous(qry))
                {
                    cache.Put(1, 1);

                    Assert.IsTrue(latch.Wait(TimeSpan.FromSeconds(10)), "Continuous query event was not delivered.");
                }
            }
        }

        /// <summary>
        /// Tested ops: BinaryTypePut and BinaryTypeNamePut. A fresh client has an empty metadata cache and an empty
        /// marshaller context, so writing a <see cref="Person"/> makes it send both.
        /// </summary>
        [Test]
        public void TestPutBinaryTypeAndRegisterBinaryTypeName()
        {
            using (var cli = Ignition.StartClient(GetClientConfiguration()))
            {
                var cache = cli.GetOrCreateCache<int, Person>(CacheName());

                var person = new Person(1);

                cache.Put(1, person);

                var res = cache.Get(1);

                Assert.AreEqual(person.Id, res.Id);
                Assert.AreEqual(person.Name, res.Name);
            }
        }

        /// <summary>
        /// Tested op: BinaryTypeGet. The type is written by one client and asked for by another, whose metadata
        /// cache does not hold it yet.
        /// </summary>
        [Test]
        public void TestGetBinaryType()
        {
            _client.GetOrCreateCache<int, Person>(CacheName()).Put(1, new Person(1));

            using (var cli = Ignition.StartClient(GetClientConfiguration()))
            {
                var type = cli.GetBinary().GetBinaryType(typeof(Person));

                Assert.IsNotNull(type);
                Assert.IsTrue(type.Fields.Contains("Name"));
            }
        }

        /// <summary>
        /// Tested op: BinaryTypeNameGet. A fresh client reads a value of a type it never wrote, so its marshaller
        /// has to ask the server for the class name of the type id.
        /// </summary>
        [Test]
        public void TestGetBinaryTypeName()
        {
            var person = new Person(1);

            _client.GetOrCreateCache<int, Person>(CacheName()).Put(1, person);

            using (var cli = Ignition.StartClient(GetClientConfiguration()))
            {
                var res = cli.GetCache<int, Person>(CacheName()).Get(1);

                Assert.AreEqual(person.Id, res.Id);
                Assert.AreEqual(person.Name, res.Name);
            }
        }

        /// <summary>
        /// Tested ops: TxStart and TxEnd, committing.
        /// </summary>
        [Test]
        public void TestTxStartCommit()
        {
            var cache = TxCache<int, string>();

            using (var tx = _client.GetTransactions().TxStart())
            {
                cache.Put(1, "1");

                tx.Commit();
            }

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested ops: TxStart and TxEnd, rolling back.
        /// </summary>
        [Test]
        public void TestTxStartRollback()
        {
            var cache = TxCache<int, string>();

            cache.Put(1, "1");

            using (var tx = _client.GetTransactions().TxStart())
            {
                cache.Put(1, "2");

                tx.Rollback();
            }

            Assert.AreEqual("1", cache.Get(1));
        }

        /// <summary>
        /// Tested op: ClusterIsActive.
        /// </summary>
        [Test]
        public void TestClusterGetState()
        {
            _client.GetCluster().IsActive();
        }

        /// <summary>
        /// Tested op: ClusterChangeState. The current state is written back unchanged, so the cluster is left
        /// exactly as it was found.
        /// </summary>
        [Test]
        public void TestClusterChangeState()
        {
            var active = _client.GetCluster().IsActive();

            _client.GetCluster().SetActive(active);

            Assert.AreEqual(active, _client.GetCluster().IsActive());
        }

        /// <summary>
        /// Tested op: ClusterGroupGetNodes. <c>GetNodes</c> sends the node ids and then the node info in turn.
        /// </summary>
        [Test]
        public void TestClusterGroupNodes()
        {
            var nodes = _client.GetCluster().GetNodes();

            Assert.IsNotEmpty(nodes, "Cluster reported no nodes.");

            foreach (var node in nodes)
            {
                Assert.AreNotEqual(Guid.Empty, node.Id);
            }
        }

        /// <summary>
        /// Tested op: ClusterGetWalState. The operation only reads, thus it leaves the cluster as it was found. WAL
        /// belongs to persistence: the server gives <c>false</c> for a cache group without it.
        /// </summary>
        [Test]
        public void TestClusterGetWalState()
        {
            _client.GetOrCreateCache<int, int>(CacheName());

            Assert.IsFalse(_client.GetCluster().IsWalEnabled(CacheName()));
        }

        /// <summary>
        /// Tested op: ServiceGetDescriptors.
        /// </summary>
        [Test]
        public void TestServiceGetDescriptors()
        {
            var descs = _client.GetServices().GetServiceDescriptors();

            Assert.IsNotNull(descs);

            foreach (var desc in descs)
            {
                if (SvcName == desc.Name)
                {
                    return;
                }
            }

            Assert.Fail("Service " + SvcName + " is not in the descriptors.");
        }

        /// <summary>
        /// Tested op: ServiceGetDescriptor.
        /// </summary>
        [Test]
        public void TestServiceGetDescriptor()
        {
            var desc = _client.GetServices().GetServiceDescriptor(SvcName);

            Assert.AreEqual(SvcName, desc.Name);
            Assert.IsNotNull(desc.ServiceClass);
            Assert.AreEqual(1, desc.TotalCount);
            Assert.IsNotNull(desc.OriginNodeId);
        }

        /// <summary>
        /// Tested op: ServiceInvoke. The proxy sends the name of the method and the arguments, thus the server
        /// needs no class of this test.
        /// </summary>
        [Test]
        public void TestServiceInvoke()
        {
            var svc = _client.GetServices().GetServiceProxy<ICompatService>(SvcName);

            Assert.AreEqual("ping", svc.echo("ping"));
            Assert.AreEqual(5, svc.add(2, 3));
        }

        /// <summary>
        /// Tested op: ServiceInvoke, for a method that gives <c>null</c> back.
        /// </summary>
        [Test]
        public void TestServiceInvokeNullResult()
        {
            var svc = _client.GetServices().GetServiceProxy<ICompatService>(SvcName);

            Assert.IsNull(svc.echo(null));
        }

        /// <summary>
        /// Tested op: ServiceInvoke, for a cluster group. The request then holds the node ids of the group in the
        /// place of an empty list.
        /// </summary>
        [Test]
        public void TestServiceInvokeOnClusterGroup()
        {
            var svc = _client.GetCluster().ForServers().GetServices().GetServiceProxy<ICompatService>(SvcName);

            Assert.AreEqual("ping", svc.echo("ping"));
        }

        /// <summary>
        /// Tested op: ServiceInvoke, for a method that fails. The error text of the server comes back in the
        /// answer.
        /// </summary>
        [Test]
        public void TestServiceInvokeFailure()
        {
            var svc = _client.GetServices().GetServiceProxy<ICompatService>(SvcName);

            var ex = Assert.Throws<IgniteClientException>(() => svc.fail());

            Assert.IsTrue(ex.Message.Contains(SvcErrMsg), "Unexpected error: " + ex);
        }

        /// <summary>
        /// A caller context needs the <c>ServiceInvokeCtx</c> feature. Apache Ignite gives this feature id 10.
        /// GridGain gives it id 34. A GridGain server does not give the feature to an Apache Ignite client, and the
        /// client throws before it sends a request.
        /// </summary>
        [Test]
        [Ignore("IgniteClientException: Passing caller context to the service is not supported by the server")]
        public void TestServiceInvokeWithCallerContext()
        {
            var callCtx = new ServiceCallContextBuilder().Set("key", "value").Build();

            var svc = _client.GetServices().GetServiceProxy<ICompatService>(SvcName, callCtx);

            svc.echo("ping");
        }

        /// <summary>
        /// Methods of the service that must be deployed under <see cref="SvcName"/>. The method names travel over
        /// the wire as plain strings taken from <c>MethodBase.Name</c>, so they must match the deployed service's
        /// method names exactly, including case.
        /// </summary>
        // ReSharper disable InconsistentNaming
        public interface ICompatService
        {
            /// <summary>
            /// Gives the given value back.
            /// </summary>
            string echo(string val);

            /// <summary>
            /// Gives the sum of the two values.
            /// </summary>
            int add(int a, int b);

            /// <summary>
            /// Always throws an error.
            /// </summary>
            void fail();
        }
        // ReSharper enable InconsistentNaming

        /// <summary>
        /// Tested op: ComputeTaskExecute. The task is named by its class, thus the server finds the class on its
        /// classpath and no deployment is necessary.
        /// </summary>
        [Test]
        public void TestComputeTaskExecute()
        {
            var res = _client.GetCompute().ExecuteJavaTask<string>(EchoTaskCls, "ping");

            Assert.AreEqual("ping", res);
        }

        /// <summary>
        /// Tested op: ComputeTaskExecute, with the task name in the place of the class name. The server resolves
        /// the name only because the cluster deployed the task under it.
        /// </summary>
        [Test]
        public void TestComputeTaskExecuteByName()
        {
            var res = _client.GetCompute().ExecuteJavaTask<string>(EchoTaskName, "ping");

            Assert.AreEqual("ping", res);
        }

        /// <summary>
        /// Tested op: ComputeTaskExecute, through the asynchronous API. The result comes in the notification, thus
        /// the task gives it only after the server sends the notification.
        /// </summary>
        [Test]
        public async Task TestComputeTaskExecuteAsync()
        {
            var res = await _client.GetCompute().ExecuteJavaTaskAsync<string>(EchoTaskCls, "ping");

            Assert.AreEqual("ping", res);
        }

        /// <summary>
        /// Tested op: ComputeTaskExecute, for a cluster group. The request then holds the node ids of the group in
        /// the place of an empty list.
        /// </summary>
        [Test]
        public void TestComputeTaskExecuteOnClusterGroup()
        {
            var compute = _client.GetCluster().ForServers().GetCompute();

            Assert.AreEqual("ping", compute.ExecuteJavaTask<string>(EchoTaskCls, "ping"));
        }

        /// <summary>
        /// Tested op: ComputeTaskExecute, with the no-failover flag. The flag travels in the flag byte of the
        /// request.
        /// </summary>
        [Test]
        public void TestComputeTaskExecuteWithNoFailover()
        {
            var res = _client.GetCompute().WithNoFailover().ExecuteJavaTask<string>(EchoTaskCls, "ping");

            Assert.AreEqual("ping", res);
        }

        /// <summary>
        /// Tested op: ComputeTaskExecute, with the no-result-cache flag. The server then keeps no job results, so
        /// the task gives <c>null</c> back. A result other than <c>null</c> shows that the flag did not reach the
        /// server.
        /// </summary>
        [Test]
        public void TestComputeTaskExecuteWithNoResultCache()
        {
            var res = _client.GetCompute().WithNoResultCache().ExecuteJavaTask<string>(EchoTaskCls, "ping");

            Assert.IsNull(res);
        }

        /// <summary>
        /// Tested op: ResourceClose, for a compute task. Cancelling the token sends it once the task id is known.
        /// The call after the cancel shows that the channel is still good.
        /// </summary>
        [Test]
        public void TestComputeTaskCancel()
        {
            var cts = new CancellationTokenSource();

            var task = _client.GetCompute().ExecuteJavaTaskAsync<object>(SleepTaskCls, SleepTaskDurationMs, cts.Token);

            cts.Cancel();

            Assert.IsInstanceOf<OperationCanceledException>(Assert.CatchAsync(() => task));

            Assert.IsNotNull(_client.GetCacheNames());
        }

        /// <summary>
        /// Tested op: ComputeTaskExecute, with the timeout field. The task is longer than the timeout, thus the
        /// server stops the task and sends an error.
        /// </summary>
        [Test]
        public void TestComputeTaskWithTimeout()
        {
            var compute = _client.GetCompute().WithTimeout(TaskTimeout);

            // The server stops the task, thus the client gets an error.
            Assert.Throws<AggregateException>(() => compute.ExecuteJavaTask<object>(SleepTaskCls, SleepTaskDurationMs));
        }

        /// <summary>
        /// Tested op: ComputeTaskExecute, with a task that fails. The error text of the server travels in the
        /// notification, thus the failure path of the compute protocol is also tested.
        /// </summary>
        [Test]
        public void TestComputeTaskFailure()
        {
            var ex = Assert.Throws<AggregateException>(
                () => _client.GetCompute().ExecuteJavaTask<string>(FailTaskCls, null));

            Assert.IsTrue(ex.GetBaseException().Message.Contains(FailTaskErrMsg), "Unexpected error: " + ex);
        }

        /// <summary>
        /// Tested op: AtomicLongCreate.
        /// </summary>
        [Test]
        public void TestAtomicLongCreate()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 42L, true);

            try
            {
                Assert.AreEqual(42L, atomic.Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested op: AtomicLongExists.
        /// </summary>
        [Test]
        public void TestAtomicLongExists()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 0L, true);

            Assert.IsFalse(atomic.IsClosed());

            atomic.Close();

            Assert.IsTrue(atomic.IsClosed());
        }

        /// <summary>
        /// Tested op: AtomicLongValueGet.
        /// </summary>
        [Test]
        public void TestAtomicLongValueGet()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 7L, true);

            try
            {
                Assert.AreEqual(7L, atomic.Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested op: AtomicLongValueAddAndGet, which also carries increment and decrement: both funnel through the
        /// same add-and-get call on the .NET client.
        /// </summary>
        [Test]
        public void TestAtomicLongValueAddAndGet()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 0L, true);

            try
            {
                Assert.AreEqual(5L, atomic.Add(5L));
                Assert.AreEqual(6L, atomic.Increment());
                Assert.AreEqual(5L, atomic.Decrement());
                Assert.AreEqual(5L, atomic.Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested op: AtomicLongValueGetAndSet.
        /// </summary>
        [Test]
        public void TestAtomicLongValueGetAndSet()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 1L, true);

            try
            {
                Assert.AreEqual(1L, atomic.Exchange(2L));
                Assert.AreEqual(2L, atomic.Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested op: AtomicLongValueCompareAndSetAndGet. Unlike the Java client, <see cref="IAtomicLongClient"/>
        /// has no plain compare-and-set: <see cref="IAtomicLongClient.CompareExchange"/> is the only compare op, and
        /// it always asks for the previous value back.
        /// </summary>
        [Test]
        public void TestAtomicLongValueCompareAndSetAndGet()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 1L, true);

            try
            {
                Assert.AreEqual(1L, atomic.CompareExchange(2L, 99L));
                Assert.AreEqual(1L, atomic.Read());

                Assert.AreEqual(1L, atomic.CompareExchange(2L, 1L));
                Assert.AreEqual(2L, atomic.Read());
            }
            finally
            {
                atomic.Close();
            }
        }

        /// <summary>
        /// Tested op: AtomicLongRemove.
        /// </summary>
        [Test]
        public void TestAtomicLongRemove()
        {
            var atomic = _client.GetAtomicLong(AtomicName(), 1L, true);

            atomic.Close();

            Assert.IsTrue(atomic.IsClosed());
        }

        /// <summary>
        /// Tested op: SetGetOrCreate.
        /// </summary>
        [Test]
        public void TestSetGetOrCreate()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

            try
            {
                Assert.IsNotNull(set);
                Assert.AreEqual(SetName(), set.Name);

                // Without a configuration the existing set is taken.
                Assert.IsNotNull(_client.GetIgniteSet<string>(SetName(), null));
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested op: SetExists.
        /// </summary>
        [Test]
        public void TestSetExists()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

            Assert.IsFalse(set.IsClosed);

            set.Close();

            Assert.IsTrue(set.IsClosed);
        }

        /// <summary>
        /// Tested op: SetValueAdd.
        /// </summary>
        [Test]
        public void TestSetValueAdd()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

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
        /// Tested op: SetValueAddAll.
        /// </summary>
        [Test]
        public void TestSetValueAddAll()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

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
        /// Tested op: SetValueContains.
        /// </summary>
        [Test]
        public void TestSetValueContains()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

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
        /// Tested op: SetValueContainsAll.
        /// </summary>
        [Test]
        public void TestSetValueContainsAll()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

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
        /// Tested op: SetValueRemove.
        /// </summary>
        [Test]
        public void TestSetValueRemove()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

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
        /// Tested op: SetValueRemoveAll.
        /// </summary>
        [Test]
        public void TestSetValueRemoveAll()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

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
        /// Tested op: SetValueRetainAll.
        /// </summary>
        [Test]
        public void TestSetValueRetainAll()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

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
        /// Tested op: SetSize.
        /// </summary>
        [Test]
        public void TestSetSize()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

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
        /// Tested op: SetClear.
        /// </summary>
        [Test]
        public void TestSetClear()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

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
        /// Tested ops: SetIteratorStart and SetIteratorGetPage. The page size is smaller than the element count,
        /// which forces the paging operation.
        /// </summary>
        [Test]
        public void TestSetIterator()
        {
            var set = _client.GetIgniteSet<int>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

            try
            {
                for (var i = 0; i < 10; i++)
                {
                    set.Add(i);
                }

                set.PageSize = 2;

                var cnt = 0;

                foreach (var ignored in set)
                {
                    cnt++;
                }

                Assert.AreEqual(10, cnt);
            }
            finally
            {
                set.Close();
            }
        }

        /// <summary>
        /// Tested op: SetClose.
        /// </summary>
        [Test]
        public void TestSetClose()
        {
            var set = _client.GetIgniteSet<string>(SetName(), new CollectionClientConfiguration { Backups = SetBackups });

            set.Add("a");

            set.Close();

            Assert.IsTrue(set.IsClosed);
        }

        /// <summary>
        /// Gets a configuration pointed at the cluster under test.
        /// </summary>
        private static IgniteClientConfiguration GetClientConfiguration()
        {
            return new IgniteClientConfiguration(Addr);
        }

        /// <summary>
        /// Checks whether something answers on <see cref="Addr"/>.
        /// </summary>
        private static bool ClusterAvailable()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    var result = socket.BeginConnect(Host, Port, null, null);

                    if (!result.AsyncWaitHandle.WaitOne(ProbeTimeoutMs, true) || !socket.Connected)
                    {
                        return false;
                    }

                    socket.EndConnect(result);

                    return true;
                }
                catch (SocketException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets a cache name unique to the running test.
        /// </summary>
        private static string CacheName()
        {
            return Prefix + TestContext.CurrentContext.Test.Name;
        }

        /// <summary>
        /// Gets an atomic long name unique to the running test.
        /// </summary>
        private static string AtomicName()
        {
            return Prefix + TestContext.CurrentContext.Test.Name;
        }

        /// <summary>
        /// Gets a set name unique to the running test.
        /// </summary>
        private static string SetName()
        {
            return Prefix + TestContext.CurrentContext.Test.Name;
        }

        /// <summary>
        /// Gets the shared cache that the plain cache operation tests use. It is empty at the start of every test.
        /// </summary>
        private static ICacheClient<TK, TV> DfltCache<TK, TV>()
        {
            return _client.GetCache<TK, TV>(DfltCacheName);
        }

        /// <summary>
        /// Makes a cache of the running test and fills it with the given number of entries.
        /// </summary>
        private static ICacheClient<int, int> FillCache(int cnt)
        {
            var cache = _client.GetOrCreateCache<int, int>(CacheName());

            cache.RemoveAll();

            var data = new Dictionary<int, int>();

            for (var i = 0; i < cnt; i++)
            {
                data[i] = i;
            }

            cache.PutAll(data);

            return cache;
        }

        /// <summary>
        /// Gets the transactional cache of the running test.
        /// </summary>
        private static ICacheClient<TK, TV> TxCache<TK, TV>()
        {
            return _client.GetOrCreateCache<TK, TV>(new CacheClientConfiguration(CacheName())
            {
                AtomicityMode = CacheAtomicityMode.Transactional
            });
        }

        /// <summary>
        /// Makes the cache the SQL tests use and fills it through SQL. The value type is named by string alone and
        /// has no .NET class behind it, so a query reaches it without any class on the server.
        /// </summary>
        private static ICacheClient<TK, TV> QueryCache<TK, TV>()
        {
            var qryEntity = new QueryEntity
            {
                TableName = QryTbl,
                KeyType = typeof(int),
                ValueTypeName = QryValType,
                KeyFieldName = "A",
                Fields = new[]
                {
                    new QueryField("A", typeof(int)),
                    new QueryField("B", typeof(int))
                }
            };

            var cache = _client.GetOrCreateCache<TK, TV>(new CacheClientConfiguration(CacheName())
            {
                QueryEntities = new[] { qryEntity }
            });

            cache.RemoveAll();

            for (var i = 0; i < QryRows; i++)
            {
                cache.Query(new SqlFieldsQuery("insert into " + QryTbl + "(A, B) values (?, ?)", i, i)).GetAll();
            }

            return cache;
        }

        /// <summary>
        /// Cache entry event listener that runs a plain callback, ignoring the event payload. Used by
        /// <see cref="TestQueryContinuous"/> in place of a lambda, since .NET has no built-in delegate adapter for
        /// <see cref="ICacheEntryEventListener{TK,TV}"/>.
        /// </summary>
        private class EntryEventListener<TK, TV> : ICacheEntryEventListener<TK, TV>
        {
            /** */
            private readonly Action _action;

            /// <summary>
            /// Initializes a new instance of <see cref="EntryEventListener{TK,TV}"/>.
            /// </summary>
            public EntryEventListener(Action action)
            {
                _action = action;
            }

            /** <inheritdoc /> */
            public void OnEvent(IEnumerable<ICacheEntryEvent<TK, TV>> evts)
            {
                foreach (var evt in evts)
                {
                    _action();
                }
            }
        }
    }
}
