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
    using Apache.Ignite.Core.Cache.Configuration;
    using NUnit.Framework;

    /// <summary>
    /// Tests the plugin with Java part.
    /// </summary>
    public class CacheJavaPluginTest
    {
                /** */
        private const string CacheName = "staticCache";

        /** */
        private const string DynCacheName = "dynamicCache";

        /** */
        private IIgnite _grid;

        /// <summary>
        /// Fixture set up.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            var cfg = new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                CacheConfiguration = new[]
                {
                    new CacheConfiguration(CacheName)
                    {
                        PluginConfigurations = new[] {new CacheJavaPluginConfiguration()}
                    }
                }
            };

            _grid = Ignition.Start(cfg);
        }

        /// <summary>
        /// Fixture tear down.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            Ignition.StopAll(true);
        }

        /// <summary>
        /// Tests that cache plugin works with static cache.
        /// </summary>
        [Test]
        public void TestStaticCache()
        {
            var cache = _grid.GetCache<int, int>(CacheName);

            Assert.IsNull(cache.GetConfiguration().PluginConfigurations);  // Java cache plugins are not returned.

            // TODO: throw an error from unwrapCacheEntry on some condition to verify that plugin works.
        }
    }
}
