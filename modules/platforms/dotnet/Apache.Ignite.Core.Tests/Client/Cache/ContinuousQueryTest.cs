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
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Cache.Event;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Client;
    using Apache.Ignite.Core.Client.Cache;
    using Apache.Ignite.Core.Client.Cache.Query.Continuous;
    using Apache.Ignite.Core.Configuration;
    using Apache.Ignite.Core.Impl.Cache.Event;
    using Apache.Ignite.Core.Impl.Client;
    using Apache.Ignite.Core.Log;
    using Apache.Ignite.Core.Tests.Compute;
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
        /// Executes after every test.
        /// </summary>
        [TearDown]
        public void TestTearDown()
        {
            var listeners = GetActiveNotificationListeners();

            Assert.IsEmpty(listeners);
        }

        /// <summary>
        /// Basic continuous query test.
        /// </summary>
        [Test]
        public void TestContinuousQueryCallsLocalListenerWithCorrectEvent()
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);

            var events = new List<ICacheEntryEvent<int, int>>();
            var qry = new ContinuousQueryClient<int, int>(new DelegateListener<int, int>(events.Add));
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
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);

            var receivedKeysAll = new ConcurrentBag<int>();
            var receivedKeysOdd = new ConcurrentBag<int>();

            var qry1 = new ContinuousQueryClient<int, int>
            {
                Listener = new DelegateListener<int, int>(e => receivedKeysOdd.Add(e.Key)),
                Filter = new OddKeyFilter()
            };

            var qry2 = new ContinuousQueryClient<int, int>
            {
                Listener = new DelegateListener<int, int>(e => receivedKeysAll.Add(e.Key)),
            };

            var cts = new CancellationTokenSource();

            var computeRunnerTask = Task.Factory.StartNew(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var res = Client.GetCompute().ExecuteJavaTask<int>(
                        ComputeApiTest.EchoTask, ComputeApiTest.EchoTypeInt);

                    Assert.AreEqual(1, res);
                }
            }, cts.Token);

            const int count = 10000;

            using (cache.QueryContinuous(qry1))
            using (cache.QueryContinuous(qry2))
            {
                for (var i = 0; i < count; i++)
                {
                    cache[i] = i;
                }
            }

            cts.Cancel();
            computeRunnerTask.Wait();

            // TODO: Check actual keys.
            TestUtils.WaitForTrueCondition(() => receivedKeysAll.Count == count);
            Assert.AreEqual(count / 2, receivedKeysOdd.Count);
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

            var qry = new ContinuousQueryClient<int,int>(listener)
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

            var evts = new ConcurrentBag<int>();
            var listener = new DelegateListener<int, int>(e => evts.Add(e.Key));

            var qry = new ContinuousQueryClient<int,int>(listener)
            {
                Filter = new JavaCacheEntryEventFilter<int, int>(
                    "org.apache.ignite.platform.PlatformCacheEntryEvenKeyEventFilter", null)
            };

            using (cache.QueryContinuous(qry))
            {
                cache.PutAll(Enumerable.Range(1, 5).ToDictionary(x => x, x => x));
            }

            TestUtils.WaitForTrueCondition(() => evts.Count == 2);
            CollectionAssert.AreEquivalent(new[] {2, 4}, evts);
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

            var qry = new ContinuousQueryClient<int, int>(listener);
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

            var qry = new ContinuousQueryClient<int,int>(new DelegateListener<int, int>());
            var initialQry = new SqlFieldsQuery("select _key from " + TestUtils.TestName);

            cache[1] = 1;

            using (var handle = cache.QueryContinuous(qry, initialQry))
            {
                using (var cur = handle.GetInitialQueryCursor())
                {
                    CollectionAssert.AreEqual(new[]{"_KEY"}, cur.FieldNames);

                    CollectionAssert.AreEqual(new[] {1}, cur.Single());
                }
            }
        }

        /// <summary>
        /// Tests that server-side updates are sent to the client.
        /// </summary>
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

            using (cache.QueryContinuous(new ContinuousQueryClient<int, int>(listener)))
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
        public void TestInitialScanQuery([Values(true, false, null)] bool? getAll)
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);

            var initialKeys = Enumerable.Range(1, 10).ToArray();
            cache.PutAll(initialKeys.ToDictionary(x => x, x => x));

            ICacheEntryEvent<int, int> lastEvt = null;

            var qry = new ContinuousQueryClient<int, int>
            {
                Listener = new DelegateListener<int, int>(e => lastEvt = e),
                BufferSize = 2
            };

            var initialQry = new ScanQuery<int, int>();

            using (var handle = cache.QueryContinuous(qry, initialQry))
            {
                using (var cursor = handle.GetInitialQueryCursor())
                {
                    if (getAll != null)
                    {
                        var initialItems = getAll == true ? cursor.GetAll() : cursor.ToList();
                        CollectionAssert.AreEquivalent(initialKeys, initialItems.Select(e => e.Key));
                    }
                    else
                    {
                        var initialItem = cursor.First();
                        CollectionAssert.Contains(initialKeys, initialItem.Key);
                    }
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

        /// <summary>
        /// Tests that initial scan query with filter returns filtered entries.
        /// </summary>
        [Test]
        public void TestInitialScanQueryWithFilter()
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);
            cache.PutAll(Enumerable.Range(1, 8).ToDictionary(x => x, x => x));

            var initialQry = new ScanQuery<int, int>
            {
                Filter = new OddKeyFilter()
            };

            var qry = new ContinuousQueryClient<int, int>(new DelegateListener<int, int>());

            using (var handle = cache.QueryContinuous(qry, initialQry))
            {
                var entries = handle.GetInitialQueryCursor().GetAll();

                CollectionAssert.AreEquivalent(new[] {1, 3, 5, 7}, entries.Select(e => e.Key));
            }
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

        /// <summary>
        /// Tests that exception in continuous query remote filter is logged and event is delivered anyway.
        /// </summary>
        [Test]
        public void TestExceptionInFilterIsLoggedAndFilterIsIgnored()
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);

            var evts = new ConcurrentBag<int>();
            var qry = new ContinuousQueryClient<int, int>(new DelegateListener<int, int>(e => evts.Add(e.Key)))
            {
                Filter = new ExceptionalFilter()
            };

            using (cache.QueryContinuous(qry))
            {
                cache[1] = 1;
            }

            Assert.AreEqual(1, cache[1]);

            // Assert: error is logged.
            var error = GetLoggers()
                .SelectMany(x => x.Entries)
                .Where(e => e.Level >= LogLevel.Warn)
                .Select(e => e.Message)
                .LastOrDefault();

            Assert.AreEqual(
                "CacheEntryEventFilter failed: javax.cache.event.CacheEntryListenerException: " +
                ExceptionalFilter.Error,
                error);

            // Assert: continuous query event is delivered.
            Assert.AreEqual(new[] {1}, evts);
        }

        /// <summary>
        /// Tests that exception in initial query filter causes exception in initial query cursor.
        /// </summary>
        [Test]
        public void TestExceptionInInitialQueryFilterResultsInCorrectErrorMessage()
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);

            ICacheEntryEvent<int, int> lastEvt = null;
            var qry = new ContinuousQueryClient<int, int>(
                new DelegateListener<int, int>(e => lastEvt = e));

            var initialQry = new ScanQuery<int, int>(new ExceptionalFilter());

            cache[1] = 1;

            using (var handle = cache.QueryContinuous(qry, initialQry))
            {
                // Listener works:
                cache[2] = 2;
                TestUtils.WaitForTrueCondition(() => lastEvt != null && lastEvt.Key == 2);

                // TODO: GetAll causes cursor disposal - add a separate test for this.
                var ex = Assert.Throws<IgniteClientException>(() => handle.GetInitialQueryCursor().GetAll());
                StringAssert.Contains("Foo failed because of bar", ex.Message);

                // Listener still works:
                cache[3] = 3;
                TestUtils.WaitForTrueCondition(() => lastEvt != null && lastEvt.Key == 3);
            }
        }

        /// <summary>
        /// Tests that invalid SQL query used as initial query provides a correct error message.
        /// </summary>
        [Test]
        public void TestInvalidInitialSqlQueryResultsInCorrectErrorMessage()
        {
            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);
            var qry = new ContinuousQueryClient<int,int>(new DelegateListener<int, int>());
            var initialQry = new SqlFieldsQuery("select a from b");

            var ex = Assert.Throws<IgniteClientException>(() => cache.QueryContinuous(qry, initialQry));
            StringAssert.StartsWith("Failed to parse query", ex.Message);
        }

        /// <summary>
        /// Tests that users can subscribe to continuous query disconnected event.
        /// </summary>
        [Test]
        public void TestClientDisconnectRaisesDisconnectedEventOnQueryHandle()
        {
            ICacheEntryEvent<int, int> lastEvt = null;
            var qry = new ContinuousQueryClient<int,int>(
                new DelegateListener<int, int>(e => lastEvt = e));

            var client = GetClient();
            var cache = client.GetOrCreateCache<int, int>(TestUtils.TestName);
            var handle = cache.QueryContinuous(qry);

            ContinuousQueryClientDisconnectedEventArgs disconnectedEventArgs = null;

            handle.Disconnected += (sender, args) =>
            {
                disconnectedEventArgs = args;

                // ReSharper disable once AccessToDisposedClosure (disposed state does not matter)
                Assert.AreEqual(handle, sender);
            };

            cache[1] = 1;
            TestUtils.WaitForTrueCondition(() => lastEvt != null);

            client.Dispose();

            // Assert: disconnected event has been raised.
            TestUtils.WaitForTrueCondition(() => disconnectedEventArgs != null);
            Assert.IsNotNull(disconnectedEventArgs.Exception);

            StringAssert.StartsWith("Cannot access a disposed object", disconnectedEventArgs.Exception.Message);

            // Multiple dispose is allowed.
            handle.Dispose();
            handle.Dispose();
        }

        /// <summary>
        /// Tests that continuous query disconnected event is not raised on a disposed handle.
        /// </summary>
        [Test]
        public void TestClientDisconnectDoesNotRaiseDisconnectedEventOnDisposedQueryHandle()
        {
            var qry = new ContinuousQueryClient<int,int>(new DelegateListener<int, int>());

            var client = GetClient();
            var cache = client.GetOrCreateCache<int, int>(TestUtils.TestName);
            ContinuousQueryClientDisconnectedEventArgs disconnectedEventArgs = null;

            using (var handle = cache.QueryContinuous(qry))
            {
                handle.Disconnected += (sender, args) => disconnectedEventArgs = args;
            }

            client.Dispose();

            // Assert: disconnected event has NOT been raised.
            Assert.IsNull(disconnectedEventArgs);
        }

        /// <summary>
        /// Tests that client does not receive updates for a stopped continuous query.
        /// </summary>
        [Test]
        public void TestDisposedQueryHandleDoesNotReceiveUpdates()
        {
            // Stop the query before the batch is sent out by time interval.
            var interval = TimeSpan.FromSeconds(1);
            TestBatches(keyCount: 1, bufferSize: 10, interval, (keys, res) => { });
            Thread.Sleep(interval);

            // Check that socket has no dangling notifications.
            Assert.IsEmpty(GetActiveNotificationListeners());
        }

        /// <summary>
        /// Tests that when custom <see cref="ContinuousQueryClient{TK,TV}.BufferSize"/> is set,
        /// events are sent in batches, not 1 by 1.
        /// </summary>
        [Test]
        public void TestCustomBufferSizeResultsInBatchedUpdates()
        {
            TestBatches(keyCount: 8, bufferSize: 3, TimeSpan.Zero, (keys, res) =>
            {
                TestUtils.WaitForTrueCondition(() => res.Count == 2, () => res.Count.ToString());

                var resOrdered = res.OrderBy(x => x.FirstOrDefault()).ToList();
                CollectionAssert.AreEquivalent(keys.Take(3), resOrdered.First());
                CollectionAssert.AreEquivalent(keys.Skip(3).Take(3), resOrdered.Last());
            });
        }

        /// <summary>
        /// Tests that when custom <see cref="ContinuousQueryClient{TK,TV}.TimeInterval"/> is set,
        /// and <see cref="ContinuousQueryClient{TK,TV}.BufferSize"/> is greater than 1,
        /// batches are sent out before buffer is full when the time interval passes.
        /// </summary>
        [Test]
        public void TestCustomTimeIntervalCausesIncompleteBatches()
        {
            TestBatches(keyCount: 2, bufferSize: 4, interval: TimeSpan.FromSeconds(1), (keys, res) =>
            {
                TestUtils.WaitForTrueCondition(() => res.Count == 1, () => res.Count.ToString(), 2000);

                CollectionAssert.AreEquivalent(keys.Take(2), res.Single());
            });
        }

        /// <summary>
        /// Tests batching behavior.
        /// </summary>
        private void TestBatches(int keyCount, int bufferSize, TimeSpan interval,
            Action<List<int>, IReadOnlyCollection<List<int>>> assert)
        {
            var res = new ConcurrentQueue<List<int>>();

            var qry = new ContinuousQueryClient<int, int>
            {
                Listener = new DelegateBatchListener<int, int>(evts =>
                    res.Enqueue(evts.Select(e => e.Key).ToList())),
                BufferSize = bufferSize,
                TimeInterval = interval
            };

            var cache = Client.GetOrCreateCache<int, int>(TestUtils.TestName);
            var server = Ignition.GetIgnite("1");
            var serverCache = server.GetCache<int, int>(cache.Name);

            // Use primary keys for "remote" node to ensure batching.
            // Client is connected to another server node, so it will receive batches as expected.
            var keys = TestUtils.GetPrimaryKeys(server, cache.Name).Take(keyCount).ToList();

            using (cache.QueryContinuous(qry))
            {
                keys.ForEach(k => serverCache.Put(k, k));

                assert(keys, res);
            }
        }

        /// <summary>
        /// Gets the active notification listeners on the main socket.
        /// </summary>
        private ICollection GetActiveNotificationListeners()
        {
            var failoverSocket = TestUtils.GetPrivateField<ClientFailoverSocket>(Client, "_socket");
            var socket = TestUtils.GetPrivateField<ClientSocket>(failoverSocket, "_socket");
            return TestUtils.GetPrivateField<ICollection>(socket, "_notificationListeners");
        }

        /** <inheritdoc /> */
        protected override IgniteConfiguration GetIgniteConfiguration()
        {
            return new IgniteConfiguration(base.GetIgniteConfiguration())
            {
                ClientConnectorConfiguration = new ClientConnectorConfiguration
                {
                    ThinClientConfiguration = new ThinClientConfiguration
                    {
                        MaxActiveComputeTasksPerConnection = 100
                    }
                }
            };
        }

        /** */
        private class DelegateListener<TK, TV> : ICacheEntryEventListener<TK, TV>
        {
            /** */
            private readonly Action<ICacheEntryEvent<TK, TV>> _action;

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

        /** */
        private class DelegateBatchListener<TK, TV> : ICacheEntryEventListener<TK, TV>
        {
            /** */
            private readonly Action<IEnumerable<ICacheEntryEvent<TK, TV>>> _action;

            /** */
            public DelegateBatchListener(Action<IEnumerable<ICacheEntryEvent<TK, TV>>> action = null)
            {
                _action = action ?? (_ => {});
            }

            /** */
            public void OnEvent(IEnumerable<ICacheEntryEvent<TK, TV>> evts)
            {
                _action(evts);
            }
        }

        private class OddKeyFilter : ICacheEntryEventFilter<int, int>, ICacheEntryFilter<int, int>
        {
            public static int LastKey;

            public bool Evaluate(ICacheEntryEvent<int, int> evt)
            {
                LastKey = evt.Key;

                return evt.Key % 2 == 1;
            }

            public bool Invoke(ICacheEntry<int, int> entry)
            {
                LastKey = entry.Key;

                return entry.Key % 2 == 1;
            }
        }

        private class ExceptionalFilter : ICacheEntryEventFilter<int, int>, ICacheEntryFilter<int, int>
        {
            public const string Error = "Foo failed because of bar";

            public bool Evaluate(ICacheEntryEvent<int, int> evt)
            {
                throw new Exception(Error);
            }

            public bool Invoke(ICacheEntry<int, int> entry)
            {
                throw new Exception(Error);
            }
        }
    }
}
