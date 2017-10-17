﻿/*
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
    using System.Linq;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Client;
    using NUnit.Framework;

    /// <summary>
    /// Tests dynamic cache start from client nodes.
    /// </summary>
    public class CreateCacheTest : ClientTestBase
    {
        /** Template cache name. */
        private const string TemplateCacheName = "template-cache-*";

        /// <summary>
        /// Tears down the test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            DestroyCaches();
        }

        /// <summary>
        /// Destroys caches.
        /// </summary>
        private void DestroyCaches()
        {
            foreach (var cacheName in Client.GetCacheNames())
            {
                Client.DestroyCache(cacheName);
            }
        }

        /// <summary>
        /// Tests the GetCacheNames.
        /// </summary>
        [Test]
        public void TestGetCacheNames()
        {
            DestroyCaches();
            Assert.AreEqual(0, Client.GetCacheNames().Count);

            Client.CreateCache<int, int>("a");
            Assert.AreEqual("a", Client.GetCacheNames().Single());

            Client.CreateCache<int, int>("b");
            Assert.AreEqual(new[] {"a", "b"}, Client.GetCacheNames().OrderBy(x => x).ToArray());

            Client.DestroyCache("a");
            Assert.AreEqual("b", Client.GetCacheNames().Single());
        }

        /// <summary>
        /// Tests create from template.
        /// </summary>
        [Test]
        public void TestCreateFromTemplate()
        {
            // No template: default configuration.
            var cache = Client.CreateCache<int, int>("foobar");
            TestUtils.AssertReflectionEqual(new CacheConfiguration(), cache.GetConfiguration());

            // Create when exists.
            var ex = Assert.Throws<IgniteClientException>(() => Client.CreateCache<int, int>(cache.Name));
            Assert.AreEqual("", ex.Message);

            // Template: custom configuration.
            cache = Client.CreateCache<int, int>(TemplateCacheName.Replace("*", "1"));
            var cfg = cache.GetConfiguration();
            Assert.AreEqual(CacheAtomicityMode.Transactional, cfg.AtomicityMode);
            Assert.AreEqual(3, cfg.Backups);
            Assert.AreEqual(CacheMode.Partitioned, cfg.CacheMode);
        }

        /// <summary>
        /// Tests getOrCreate from template.
        /// </summary>
        [Test]
        public void TestGetOrCreateFromTemplate()
        {
            // No template: default configuration.
            var cache = Client.GetOrCreateCache<int, int>("foobar");
            TestUtils.AssertReflectionEqual(new CacheConfiguration(), cache.GetConfiguration());
            cache[1] = 1;

            // Create when exists.
            cache = Client.GetOrCreateCache<int, int>("foobar");
            Assert.AreEqual(1, cache[1]);

            // Template: custom configuration.
            cache = Client.GetOrCreateCache<int, int>(TemplateCacheName.Replace("*", "1"));
            var cfg = cache.GetConfiguration();
            Assert.AreEqual(CacheAtomicityMode.Transactional, cfg.AtomicityMode);
            Assert.AreEqual(3, cfg.Backups);
            Assert.AreEqual(CacheMode.Partitioned, cfg.CacheMode);

            // Create when exists.
            cache[1] = 1;
            cache = Client.GetOrCreateCache<int, int>(cache.Name);
            Assert.AreEqual(1, cache[1]);
        }

        /// <summary>
        /// Tests cache creation from configuration.
        /// </summary>
        [Test]
        public void TestCreateFromConfiguration()
        {
            // TODO
        }

        /// <summary>
        /// Tests cache creation from configuration.
        /// </summary>
        [Test]
        public void TestGetOrCreateFromConfiguration()
        {
            // TODO
        }

        /** <inheritdoc /> */
        protected override IgniteConfiguration GetIgniteConfiguration()
        {
            return new IgniteConfiguration(base.GetIgniteConfiguration())
            {
                CacheConfiguration = new[]
                {
                    new CacheConfiguration(TemplateCacheName)
                    {
                        AtomicityMode = CacheAtomicityMode.Transactional,
                        Backups = 3
                    }
                }
            };
        }
    }
}
