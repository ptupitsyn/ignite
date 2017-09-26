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

namespace Apache.Ignite.Core.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Events;
    using Apache.Ignite.Core.Impl.Events;
    using NUnit.Framework;

    /// <summary>
    /// Tests <see cref="IgniteConfiguration.LocalEventListeners" />.
    /// </summary>
    public class EventsTestLocalListeners
    {
        /** Cache name. */
        private const string CacheName = "cache";

        /// <summary>
        /// Tests the rebalance events which occur during node startup.
        /// </summary>
        [Test]
        public void TestRebalanceEvents()
        {
            var listener = new Listener<CacheRebalancingEvent>();

            using (Ignition.Start(GetConfig(listener, EventType.CacheRebalanceAll)))
            {
                var events = listener.GetEvents();

                Assert.AreEqual(2, events.Count);

                var rebalanceStart = events.First();

                Assert.AreEqual(CacheName, rebalanceStart.CacheName);
                Assert.AreEqual(EventType.CacheRebalanceStarted, rebalanceStart.Type);

                var rebalanceStop = events.Last();

                Assert.AreEqual(CacheName, rebalanceStop.CacheName);
                Assert.AreEqual(EventType.CacheRebalanceStopped, rebalanceStop.Type);
            }
        }

        /// <summary>
        /// Tests the unsubscription.
        /// </summary>
        [Test]
        public void TestUnsubscribe()
        {
            var listener = new Listener<CacheEvent>();

            using (var ignite = Ignition.Start(GetConfig(listener, EventType.CacheAll)))
            {
                Assert.AreEqual(0, listener.GetEvents().Count);

                var cache = ignite.GetCache<int, int>(CacheName);
                
                // Put causes 3 events: EntryCreated, ObjectPut, EntryDestroyed.
                cache.Put(1, 1);
                Assert.AreEqual(3, listener.GetEvents().Count);

                // Remove listener from one of the event types.
                ignite.GetEvents().StopLocalListen(listener, EventType.CacheEntryCreated);

                cache.Put(2, 2);
                Assert.AreEqual(2, listener.GetEvents().Count);
            }
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        private static IgniteConfiguration GetConfig<T>(IEventListener<T> listener, ICollection<int> eventTypes) 
            where T : IEvent
        {
            return new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                LocalEventListeners = new[]
                {
                    new LocalEventListener<T>
                    {
                        Listener = listener,
                        EventTypes = eventTypes
                    }
                },
                IncludedEventTypes = eventTypes,
                CacheConfiguration = new[] { new CacheConfiguration(CacheName) }
            };
        }

        /// <summary>
        /// Listener.
        /// </summary>
        private class Listener<T> : IEventListener<T> where T : IEvent
        {
            /** Listen action. */
            private readonly List<T> _events = new List<T>();

            /// <summary>
            /// Gets the events.
            /// </summary>
            public ICollection<T> GetEvents()
            {
                lock (_events)
                {
                    var res = _events.ToArray();

                    _events.Clear();

                    return res;
                }
            }

            /** <inheritdoc /> */
            public bool Invoke(T evt)
            {
                lock (_events)
                {
                    _events.Add(evt);
                }

                return true;
            }
        }
    }
}