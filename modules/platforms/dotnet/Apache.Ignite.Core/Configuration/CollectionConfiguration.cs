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

namespace Apache.Ignite.Core.Configuration
{
    using System.Diagnostics;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache.Configuration;

    /// <summary>
    /// Ignite collection configuration.
    /// </summary>
    public class CollectionConfiguration
    {
        /// <summary> The default atomicity mode. </summary>
        public const CacheAtomicityMode DefaultAtomicityMode = CacheAtomicityMode.Atomic;

        /// <summary> The default cache mode. </summary>
        public const CacheMode DefaultCacheMode = CacheMode.Partitioned;

        /// <summary> The default memory mode. </summary>
        public const CacheMemoryMode DefaultMemoryMode = CacheMemoryMode.OnheapTiered;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionConfiguration"/> class.
        /// </summary>
        public CollectionConfiguration()
        {
            AtomicityMode = DefaultAtomicityMode;
            CacheMode = DefaultCacheMode;
            MemoryMode = DefaultMemoryMode;
        }

        /// <summary>
        /// Gets or sets the atomicity mode.
        /// </summary>
        public CacheAtomicityMode AtomicityMode { get; set; }

        /// <summary>
        /// Gets or sets the cache mode.
        /// </summary>
        public CacheMode CacheMode { get; set; }

        /// <summary>
        /// Gets or sets the memory mode.
        /// </summary>
        public CacheMemoryMode MemoryMode { get; set; }

        /// <summary>
        /// Writes this instance to a writer.
        /// </summary>
        internal void Write(IBinaryRawWriter writer)
        {
            Debug.Assert(writer != null);

            writer.WriteInt((int) AtomicityMode);
        }
    }
}
