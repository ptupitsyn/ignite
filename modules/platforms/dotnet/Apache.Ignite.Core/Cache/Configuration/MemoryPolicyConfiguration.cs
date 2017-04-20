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
    /// 
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
        public MemoryPolicyConfiguration(IBinaryRawReader reader)
        {
            // TODO
        }

        /// <summary>
        /// Writes this instance to a writer.
        /// </summary>
        internal void Write(IBinaryRawWriter writer)
        {
            // TODO
        }

        public string Name { get; set; }

        public long Size { get; set; }

        public string SwapFilePath { get; set; }

        public DataPageEvictionMode PageEvictionMode { get; set; }
    }
}