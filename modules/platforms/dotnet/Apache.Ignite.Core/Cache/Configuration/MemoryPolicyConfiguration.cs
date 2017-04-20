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

namespace Apache.Ignite.Core.Cache.Configuration
{
    using Apache.Ignite.Core.Binary;

    /// <summary>
    /// Defines page memory policy configuration. See <see cref="MemoryConfiguration.MemoryPolicies"/>.
    /// </summary>
    public class MemoryPolicyConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryPolicyConfiguration"/> class.
        /// </summary>
        public MemoryPolicyConfiguration()
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryPolicyConfiguration"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        internal MemoryPolicyConfiguration(IBinaryRawReader reader)
        {
            Name = reader.ReadString();
            Size = reader.ReadLong();
            SwapFilePath = reader.ReadString();
            PageEvictionMode = (DataPageEvictionMode) reader.ReadInt();
        }

        /// <summary>
        /// Writes this instance to a writer.
        /// </summary>
        internal void Write(IBinaryRawWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteLong(Size);
            writer.WriteString(SwapFilePath);
            writer.WriteInt((int) PageEvictionMode);
        }

        /// <summary>
        /// Gets or sets the memory policy name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the maximum memory region size defined by this memory policy.
        /// If the whole data can not fit into the memory region an out of memory exception will be thrown.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the the path to the memory-mapped file the memory region defined by this memory policy
        /// will be mapped to. Having the path set, allows relying on swapping capabilities of an underlying
        /// operating system for the memory region.
        /// <para />
        /// Null for no swap.
        /// </summary>
        public string SwapFilePath { get; set; }

        /// <summary>
        /// Gets or sets the page eviction mode. If <see cref="DataPageEvictionMode.Disabled"/> is used (default)
        /// then an out of memory exception will be thrown if the memory region usage,
        /// defined by this memory policy, goes beyond <see cref="Size"/>.
        /// </summary>
        public DataPageEvictionMode PageEvictionMode { get; set; }
    }
}