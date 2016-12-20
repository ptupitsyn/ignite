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

namespace Apache.Ignite.Core.Tests.Plugin.Cache
{
    using System.Linq;
    using Apache.Ignite.Core.Cache.Configuration;
    using NUnit.Framework;

    /// <summary>
    /// Tests for cache plugins.
    /// </summary>
    public class CachePluginTest
    {
        /** */
        private const string CacheName = "staticCache";

        /// <summary>
        /// Tests the full plugin lifecycle.
        /// </summary>
        [Test]
        public void TestStartStop()
        {
            // TODO: Test with static and dynamic cache start.

            using (var ignite = Ignition.Start(GetConfig()))
            {
                // Check config.
                var cache = ignite.GetCache<int, int>(CacheName);
                var cacheCfg = cache.GetConfiguration();
                var plugCfg = cacheCfg.PluginConfigurations.Cast<CachePluginConfiguration>().Single();
                Assert.AreEqual("foo", plugCfg.TestProperty);

                // Check started plugin.
                var plugin = CachePlugin.GetInstances().Single();
                Assert.IsTrue(plugin.Started);
                Assert.IsTrue(plugin.IgniteStarted);
                Assert.IsNull(plugin.Stopped);
            }
        }

        /// <summary>
        /// Tests invalid plugins.
        /// </summary>
        [Test]
        public void TestInvalidPlugins()
        {
            // TODO: Return null from various places, throw error, non-serializable provider, etc.
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        private static IgniteConfiguration GetConfig()
        {
            return new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                CacheConfiguration = new[]
                {
                    new CacheConfiguration(CacheName)
                    {
                        PluginConfigurations = new[]
                        {
                            new CachePluginConfiguration {TestProperty = "foo"}
                        }
                    }
                }
            };
        }
    }
}
