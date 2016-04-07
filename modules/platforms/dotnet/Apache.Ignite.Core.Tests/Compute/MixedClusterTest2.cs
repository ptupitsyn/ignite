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

namespace Apache.Ignite.Core.Tests.Compute
{
    using System;
    using Apache.Ignite.Core.Events;
    using NUnit.Framework;

    /// <summary>
    /// Freshdesk test.
    /// </summary>
    public class MixedClusterTest2
    {
        [Test]
        public void Test()
        {
            using (var ignite = Ignition.Start("config\\freshdesk.xml"))
            {
                var listener = new EventListener();

                ignite.GetEvents().LocalListen(listener, EventType.CacheAll);

                const string javaTask = "org.apache.ignite.platform.PlatformRunContinuousQueryTask";
                ignite.GetCompute().ExecuteJavaTask<object>(javaTask, "myCache");

                var cache = ignite.GetCache<int, string>("myCache");
                for (int i = 0; i < 10; i++)
                    cache[i] = i + "!";
            }
        }

        private class EventListener : IEventListener<CacheEvent>
        {
            public bool Invoke(CacheEvent evt)
            {
                Console.WriteLine("Event: " + evt.Name);

                return true;
            }
        }
    }
}