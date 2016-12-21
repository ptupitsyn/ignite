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

        /** */
        private IIgnite _grid1;

        /** */
        private IIgnite _grid2;

        /// <summary>
        /// Fixture set up.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _grid1 = Ignition.Start(GetConfig("grid1"));
            _grid2 = Ignition.Start(GetConfig("grid2"));
        }

        /// <summary>
        /// Fixture tear down.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            TestUtils.AssertHandleRegistryHasItems(10, 2, _grid1, _grid2);
            Ignition.StopAll(true);
        }

        /// <summary>
        /// Tests with static cache.
        /// </summary>
        [Test]
        public void TestStaticCache()
        {
            foreach (var ignite in new[] {_grid1, _grid2})
            {
                // Check config.
                var plugCfg = ignite.GetCache<int, int>(CacheName).GetConfiguration()
                    .PluginConfigurations.Cast<CachePluginConfiguration>().Single();
                Assert.AreEqual("foo", plugCfg.TestProperty);

                // Check started plugin.
                var plugin = CachePlugin.GetInstances().Single(x => x.Context.Ignite == ignite);
                Assert.IsTrue(plugin.Started);
                Assert.IsTrue(plugin.IgniteStarted);
                Assert.IsNull(plugin.Stopped);

                var ctx = plugin.Context;
                Assert.AreEqual(ignite.Name, ctx.IgniteConfiguration.GridName);
                Assert.AreEqual(CacheName, ctx.CacheConfiguration.Name);
                Assert.AreEqual("foo", ((CachePluginConfiguration) ctx.CachePluginConfiguration).TestProperty);
            }
        }

        /// <summary>
        /// Tests with dynamic cache.
        /// </summary>
        [Test]
        public void TestDynamicCache()
        {
            // TODO
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
        private static IgniteConfiguration GetConfig(string name)
        {
            return new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                GridName = name,
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
