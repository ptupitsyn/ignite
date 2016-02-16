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
        /// <summary>
        /// Gets or sets the atomicity mode.
        /// </summary>
        public CacheAtomicityMode AtomicityMode { get; set; }

        // TODO: implement this after IGNITE-1906 merge
        internal void Write(IBinaryRawWriter writer)
        {
            Debug.Assert(writer != null);

            writer.WriteInt((int) AtomicityMode);
        }
    }
}
