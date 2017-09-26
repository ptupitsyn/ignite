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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
            var events = new List<CacheRebalancingEvent>();

            var cfg = new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                LocalEventListeners = new[]
                {
                    new LocalEventListener<CacheRebalancingEvent>
                    {
                        Listener = new Listener<CacheRebalancingEvent>(e =>
                        {
                            events.Add(e);
                            return true;
                        }),
                        EventTypes = EventType.CacheRebalanceAll
                    }
                },
                IncludedEventTypes = EventType.CacheRebalanceAll,
                CacheConfiguration = new[] { new CacheConfiguration(CacheName) }
            };

            using (Ignition.Start(cfg))
            {
                Assert.AreEqual(2, events.Count);

                var rebalanceStart = events.First();

                Assert.AreEqual(CacheName, rebalanceStart.CacheName);
                Assert.AreEqual(EventType.CacheRebalanceStarted, rebalanceStart.Type);

                var rebalanceStop = events.Last();

                Assert.AreEqual(CacheName, rebalanceStop.CacheName);
                Assert.AreEqual(EventType.CacheRebalanceStopped, rebalanceStart.Type);
            }
        }

        /// <summary>
        /// Listener.
        /// </summary>
        private class Listener<T> : IEventListener<T> where T : IEvent
        {
            /** Listen action. */
            private readonly Func<T, bool> _listener;

            /// <summary>
            /// Initializes a new instance of the <see cref="Listener{T}"/> class.
            /// </summary>
            public Listener(Func<T, bool> listener)
            {
                Debug.Assert(listener != null);

                _listener = listener;
            }

            /** <inheritdoc /> */
            public bool Invoke(T evt)
            {
                return _listener(evt);
            }
        }
    }
}