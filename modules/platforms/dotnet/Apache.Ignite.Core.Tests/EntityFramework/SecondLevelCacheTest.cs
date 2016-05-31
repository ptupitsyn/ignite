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

namespace Apache.Ignite.Core.Tests.EntityFramework
{
    using System.Diagnostics.CodeAnalysis;
    using Apache.Ignite.EntityFramework;
    using NUnit.Framework;

    /// <summary>
    /// Tests the EF cache provider.
    /// </summary>
    public class SecondLevelCacheTest
    {
        [Test]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public void TestConfiguration()
        {
            Assert.IsNull(Ignition.TryGetIgnite());

            // Test default config (picks up app.config section)
            new IgniteDbConfiguration();
            var ignite = Ignition.TryGetIgnite("grid1");
            Assert.IsNotNull(ignite);
            Assert.IsNotNull(ignite.GetCache<object, object>(IgniteDbConfiguration.DefaultCacheName));
        }

        [Test]
        public void TestBasic()
        {

            
        }

        [Test]
        public void TestExpiration()
        {
            
        }
    }
}
