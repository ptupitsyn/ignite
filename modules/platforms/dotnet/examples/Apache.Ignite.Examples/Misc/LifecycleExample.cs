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

namespace Apache.Ignite.Examples.Misc
{
    using System;
    using System.Collections.Generic;
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Discovery.Tcp;
    using Apache.Ignite.Core.Discovery.Tcp.Static;
    using Apache.Ignite.Core.Lifecycle;

    /// <summary>
    /// This example shows how to provide your own <see cref="ILifecycleEventHandler"/> implementation
    /// to be able to hook into Apache lifecycle. Example handler will output occurred lifecycle 
    /// events to the console.
    /// <para />
    /// 1) Build the project Apache.Ignite.ExamplesDll (select it -> right-click -> Build).
    ///    Apache.Ignite.ExamplesDll.dll must appear in %IGNITE_HOME%/platforms/dotnet/examples/Apache.Ignite.ExamplesDll/bin/${Platform]/${Configuration} folder.
    /// 2) Set this class as startup object (Apache.Ignite.Examples project -> right-click -> Properties ->
    ///     Application -> Startup object);
    /// 3) Start example (F5 or Ctrl+F5).
    /// </summary>
    public class LifecycleExample
    {
        /// <summary>
        /// Runs the example.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Console.WriteLine();
            Console.WriteLine(">>> Lifecycle example started.");

            // Create new configuration.
            var lifecycleExampleHandler = new LifecycleExampleEventHandler();

            var cfg = new IgniteConfiguration
            {
                DiscoverySpi = new TcpDiscoverySpi
                {
                    IpFinder = new TcpDiscoveryStaticIpFinder
                    {
                        Endpoints = new[] {"127.0.0.1:47500"}
                    }
                },
                LifecycleEventHandlers = new List<ILifecycleEventHandler> {lifecycleExampleHandler}
            };

            // Provide lifecycle handler to configuration.
            using (Ignition.Start(cfg))
            {
                // Make sure that lifecycle handler was notified about Ignite startup.
                Console.WriteLine();
                Console.WriteLine(">>> Started (should be true): " + lifecycleExampleHandler.Started);
            }

            // Make sure that lifecycle handler was notified about Ignite stop.
            Console.WriteLine();
            Console.WriteLine(">>> Started (should be false): " + lifecycleExampleHandler.Started);

            Console.WriteLine();
            Console.WriteLine(">>> Example finished, press any key to exit ...");
            Console.ReadKey();
        }

        /// <summary>
        /// Sample lifecycle handler implementation.
        /// </summary>
        private class LifecycleExampleEventHandler : ILifecycleEventHandler
        {
            /// <summary>
            /// Called when lifecycle event occurs.
            /// </summary>
            /// <param name="eventType">Event type.</param>
            /// <param name="ignite">Ignite instance.</param>
            public void OnLifecycleEvent(LifecycleEventType eventType, IIgnite ignite)
            {
                Console.WriteLine();
                Console.WriteLine(">>> Ignite lifecycle event occurred: " + eventType);
                Console.WriteLine(">>> Ignite name: " + (ignite != null ? ignite.Name : "not available"));

                if (eventType == LifecycleEventType.AfterNodeStart)
                    Started = true;
                else if (eventType == LifecycleEventType.AfterNodeStop)
                    Started = false;
            }

            /// <summary>
            /// Started flag.
            /// </summary>
            public bool Started { get; private set; }
        }
    }
}
