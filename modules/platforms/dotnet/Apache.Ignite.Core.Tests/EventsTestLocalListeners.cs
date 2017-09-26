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

            var cfg = new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                LocalEventListeners = new[]
                {
                    new LocalEventListener<CacheRebalancingEvent>
                    {
                        Listener = listener,
                        EventTypes = EventType.CacheRebalanceAll
                    }
                },
                IncludedEventTypes = EventType.CacheRebalanceAll,
                CacheConfiguration = new[] { new CacheConfiguration(CacheName) }
            };

            using (Ignition.Start(cfg))
            {
                var events = listener.Events;

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
        /// Listener.
        /// </summary>
        private class Listener<T> : IEventListener<T> where T : IEvent
        {
            /** Listen action. */
            private readonly List<T> _events = new List<T>();

            /// <summary>
            /// Gets the events.
            /// </summary>
            public ICollection<T> Events
            {
                get
                {
                    lock (_events)
                    {
                        return _events.ToArray();
                    }
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