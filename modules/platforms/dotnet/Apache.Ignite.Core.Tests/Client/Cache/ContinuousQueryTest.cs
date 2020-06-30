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
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Cache.Event;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Cache.Query.Continuous;
    using Apache.Ignite.Core.Client.Cache;
    using Apache.Ignite.Core.Impl.Cache.Event;
    using NUnit.Framework;

    /// <summary>
    /// Tests for thin client continuous queries.
    /// </summary>
    public class ContinuousQueryTest : ClientTestBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ContinuousQueryTest"/>.
        /// </summary>
        public ContinuousQueryTest() : base(2)
        {
            // No-op.
        }

        /// <summary>
        /// Basic continuous query test.
        /// </summary>
        [Test]
        public void TestContinuousQueryCallsLocalListenerWithCorrectEvent()
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);

            var events = new List<ICacheEntryEvent<int, int>>();
            var qry = new ContinuousQuery<int, int>(new DelegateListener<int, int>(events.Add));
            using (cache.QueryContinuous(qry))
            {
                // Create.
                cache.Put(1, 1);
                TestUtils.WaitForTrueCondition(() => events.Count == 1);

                var evt = events.Single();
                Assert.AreEqual(CacheEntryEventType.Created, evt.EventType);
                Assert.IsFalse(evt.HasOldValue);
                Assert.IsTrue(evt.HasValue);
                Assert.AreEqual(1, evt.Key);
                Assert.AreEqual(1, evt.Value);

                // Update.
                cache.Put(1, 2);
                TestUtils.WaitForTrueCondition(() => events.Count == 2);

                evt = events.Last();
                Assert.AreEqual(CacheEntryEventType.Updated, evt.EventType);
                Assert.IsTrue(evt.HasOldValue);
                Assert.IsTrue(evt.HasValue);
                Assert.AreEqual(1, evt.Key);
                Assert.AreEqual(2, evt.Value);
                Assert.AreEqual(1, evt.OldValue);

                // Remove.
                cache.Remove(1);
                TestUtils.WaitForTrueCondition(() => events.Count == 3);

                evt = events.Last();
                Assert.AreEqual(CacheEntryEventType.Removed, evt.EventType);
                Assert.IsTrue(evt.HasOldValue);
                Assert.IsTrue(evt.HasValue);
                Assert.AreEqual(1, evt.Key);
                Assert.AreEqual(2, evt.Value);
                Assert.AreEqual(2, evt.OldValue);
            }
        }

        /// <summary>
        /// Tests that Compute notifications and Continuous Query notifications work together correctly.
        /// </summary>
        [Test]
        public void TestComputeWorksWhenContinuousQueryIsActive()
        {
            // TODO: Start multiple queries with different filters,
            // do cache updates and compute calls in multiple threads.
        }

        /// <summary>
        /// Tests that continuous query with filter receives only matching events.
        /// </summary>
        [Test]
        public void TestContinuousQueryWithFilterReceivesOnlyMatchingEvents()
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);

            ICacheEntryEvent<int, int> lastEvt = null;
            var listener = new DelegateListener<int, int>(e => lastEvt = e);

            var qry = new ContinuousQuery<int,int>(listener)
            {
                Filter = new OddKeyFilter()
            };

            using (cache.QueryContinuous(qry))
            {
                cache.Put(0, 0);
                TestUtils.WaitForTrueCondition(() => OddKeyFilter.LastKey == 0);
                Assert.IsNull(lastEvt);

                cache.Put(5, 5);
                TestUtils.WaitForTrueCondition(() => OddKeyFilter.LastKey == 5);
                TestUtils.WaitForTrueCondition(() => lastEvt != null);
                Assert.IsNotNull(lastEvt);
                Assert.AreEqual(5, lastEvt.Key);

                cache.Put(8, 8);
                TestUtils.WaitForTrueCondition(() => OddKeyFilter.LastKey == 8);
                Assert.AreEqual(5, lastEvt.Key);
            }
        }

        /// <summary>
        /// Tests that continuous query with Java filter receives only matching events.
        /// </summary>
        [Test]
        public void TestContinuousQueryWithJavaFilterReceivesOnlyMatchingEvents()
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);

            ICacheEntryEvent<int, int> lastEvt = null;
            var listener = new DelegateListener<int, int>(e => lastEvt = e);

            var qry = new ContinuousQuery<int,int>(listener)
            {
                Filter = new JavaCacheEntryEventFilter<int, int>("TODO", null)
            };

            // TODO
        }

        /// <summary>
        /// Tests that server starts sending notifications only when client is ready to receive them.
        /// There is a brief moment when server starts the continuous query,
        /// but client has not received the response with query ID yet - server should not send notifications.
        /// </summary>
        [Test]
        public void TestContinuousQueryWithInitialQueryDoesNotMissCacheUpdates()
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);
            var serverCache = Ignition.GetIgnite().GetCache<int, int>(cache.Name);

            var keys = new ConcurrentBag<int>();
            var listener = new DelegateListener<int, int>(e => keys.Add(e.Key));

            var enableCacheUpdates = true;
            long key = 0;

            var generatorTask = Task.Factory.StartNew(() =>
            {
                // ReSharper disable once AccessToModifiedClosure
                while (Volatile.Read(ref enableCacheUpdates))
                {
                    // ReSharper disable once AccessToModifiedClosure
                    serverCache[(int)Interlocked.Increment(ref key)] = 1;
                }
            });

            TestUtils.WaitForTrueCondition(() => Interlocked.Read(ref key) > 10);

            var qry = new ContinuousQuery<int, int>(listener);
            var initialQry = new ScanQuery<int, int>();

            using (var handle = cache.QueryContinuous(qry, initialQry))
            {
                foreach (var e in handle.GetInitialQueryCursor())
                {
                    keys.Add(e.Key);
                }

                // Wait for some more events.
                var currentKey = Interlocked.Read(ref key);
                TestUtils.WaitForTrueCondition(() => Interlocked.Read(ref key) > currentKey);

                // Stop generator.
                Volatile.Write(ref enableCacheUpdates, false);
                generatorTask.Wait();
            }

            var lastKey = Interlocked.Read(ref key);
            Assert.AreEqual(lastKey, keys.Max());

            // Initial query can overlap the events (same with thick client) => use Distinct().
            CollectionAssert.AreEquivalent(Enumerable.Range(1, (int) lastKey), keys.Distinct());
        }

        /// <summary>
        /// Tests that SqlFieldsQuery works as initial query, returns both data and metadata.
        /// </summary>
        [Test]
        public void TestInitialSqlQueryReturnsRowDataAndColumnNames()
        {
            var queryEntity = new QueryEntity(typeof(int), typeof(int))
            {
                TableName = TestUtils.TestName
            };

            var cacheCfg = new CacheClientConfiguration(TestUtils.TestName, queryEntity);
            var cache = Client.GetOrCreateCache<int, int>(cacheCfg);
            var qry = new ContinuousQuery<int,int>(new DelegateListener<int, int>());

            cache[1] = 1;

            using (var handle = cache.QueryContinuous(qry, new SqlFieldsQuery("select _key from " + TestUtils.TestName)))
            {
                using (var cur = handle.GetInitialQueryCursor())
                {
                    CollectionAssert.AreEqual(new[]{"_KEY"}, cur.FieldNames);

                    CollectionAssert.AreEqual(new[] {1}, cur.Single());
                }
            }
        }

        [Test]
        public void TestClientContinuousQueryReceivesEventsFromServerCache()
        {
            const int count = 10000;

            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);

            var receiveCount = 0;

            var listener = new DelegateListener<int, int>(e =>
            {
                Interlocked.Increment(ref receiveCount);
            });

            using (cache.QueryContinuous(new ContinuousQuery<int, int>(listener)))
            {
                var serverCache = Ignition.GetIgnite("1").GetCache<int, int>(cache.Name);

                for (var i = 0; i < count; i++)
                {
                    serverCache.Put(i, i);
                }

                TestUtils.WaitForTrueCondition(() => receiveCount == count);
            }
        }

        /// <summary>
        /// Tests that initial Scan query returns data that existed before the Continuous query has started.
        /// </summary>
        [Test]
        public void TestInitialScanQuery([Values(true, false)]bool getAll)
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);

            var initialKeys = Enumerable.Range(1, 10).ToArray();
            cache.PutAll(initialKeys.ToDictionary(x => x, x => x));

            ICacheEntryEvent<int, int> lastEvt = null;
            var qry = new ContinuousQuery<int,int>(new DelegateListener<int, int>(e => lastEvt = e));
            var initialQry = new ScanQuery<int, int>();

            using (var handle = cache.QueryContinuous(qry, initialQry))
            {
                using (var cursor = handle.GetInitialQueryCursor())
                {
                    var initialItems = getAll ? cursor.GetAll() : cursor.ToList();
                    CollectionAssert.AreEquivalent(initialKeys, initialItems.Select(e => e.Key));
                }

                cache.Put(20, 20);

                TestUtils.WaitForTrueCondition(() => lastEvt != null);
                Assert.AreEqual(20, lastEvt.Key);

                cache.Put(21, 21);
                TestUtils.WaitForTrueCondition(() => 21 == lastEvt.Key);
            }
        }

        /// <summary>
        /// Tests that initial Scan query returns data that existed before the Continuous query has started
        /// in binary mode.
        /// </summary>
        [Test]
        public void TestInitialScanQueryInBinaryMode([Values(true, false)] bool getAll)
        {
            // TODO:
        }

        [Test]
        public void TestInitialScanQueryWithFilter()
        {
            // TODO
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);
            cache.PutAll(Enumerable.Range(1, 10).ToDictionary(x => x, x => x));

            var initialQry = new ScanQuery<int, int>();
        }

        [Test]
        public void TestContinuousQueryInBinaryMode()
        {
            // TODO
        }

        [Test]
        public void TestContinuousQueryWithFilterInBinaryMode()
        {
            // TODO: filter operates on binary objects.
        }

        [Test]
        public void TestExceptionInFilterResultsInCorrectErrorMessage()
        {
            // TODO
        }

        [Test]
        public void TestExceptionInInitialQueryFilterResultsInCorrectErrorMessage()
        {
            // TODO
        }

        [Test]
        public void TestInvalidInitialSqlQueryResultsInCorrectErrorMessage()
        {
            // TODO
        }

        /// <summary>
        /// Tests that listener gets notified about connection failure with a special event.
        /// </summary>
        [Test]
        public void TestDisconnectCausesExceptionInContinuousQueryListener()
        {
            ICacheEntryEvent<int, int> lastEvt = null;
            var qry = new ContinuousQuery<int,int>(
                new DelegateListener<int, int>(e => lastEvt = e));

            using (var client = GetClient())
            {
                var cache = client.GetOrCreateCache<int, int>(TestUtils.TestName);

                cache.QueryContinuous(qry);
            }

            Assert.IsNotNull(lastEvt);
        }

        /** */
        private class DelegateListener<TK, TV> : ICacheEntryEventListener<TK, TV>
        {
            /** */
            private Action<ICacheEntryEvent<TK, TV>> _action;

            /** */
            public DelegateListener(Action<ICacheEntryEvent<TK, TV>> action = null)
            {
                _action = action ?? (_ => {});
            }

            /** */
            public void OnEvent(IEnumerable<ICacheEntryEvent<TK, TV>> evts)
            {
                foreach (var evt in evts)
                {
                    _action(evt);
                }
            }
        }

        private class OddKeyFilter : ICacheEntryEventFilter<int, int>
        {
            public static int LastKey;

            public bool Evaluate(ICacheEntryEvent<int, int> evt)
            {
                LastKey = evt.Key;

                return evt.Key % 2 == 1;
            }
        }
    }
}