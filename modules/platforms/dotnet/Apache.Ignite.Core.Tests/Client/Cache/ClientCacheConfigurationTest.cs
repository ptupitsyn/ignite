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
    using System.IO;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Impl.Binary.IO;
    using Apache.Ignite.Core.Impl.Client.Cache;
    using NUnit.Framework;

    /// <summary>
    /// Tests client cache configuration handling.
    /// </summary>
    public class ClientCacheConfigurationTest
    {
        /// <summary>
        /// Tests the serialization/deserialization of <see cref="CacheConfiguration"/>.
        /// </summary>
        [Test]
        public void TestSerializeDeserialize()
        {
            var empty = new CacheConfiguration("foo");

            TestUtils.AssertReflectionEqual(empty, SerializeDeserialize(empty));
        }

        /// <summary>
        /// Serializes and deserializes the config.
        /// </summary>
        private static CacheConfiguration SerializeDeserialize(CacheConfiguration cfg)
        {
            using (var stream = new BinaryHeapStream(128))
            {
                ClientCacheConfigurationSerializer.Write(stream, cfg);
                stream.Seek(0, SeekOrigin.Begin);
                return ClientCacheConfigurationSerializer.Read(stream);
            }
        }
    }
}
