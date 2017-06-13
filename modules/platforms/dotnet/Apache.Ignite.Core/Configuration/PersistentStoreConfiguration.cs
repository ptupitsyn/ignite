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
    using System;

    /// <summary>
    /// Configures Apache Ignite persistent store.
    /// </summary>
    public class PersistentStoreConfiguration
    {
        /// <summary>
        /// Default value for <see cref="CheckpointingPageBufferSize"/>.
        /// </summary>
        public const long DefaultCheckpointingPageBufferSize = 256L * 1024 * 1024;

        /** */
        private long? _checkpointingPageBufferSize;

        /// <summary>
        /// Gets or sets the path where data and indexes will be persisted.
        /// </summary>
        public string PersistentStorePath { get; set; }

        /// <summary>
        /// Gets or sets the checkpointing frequency which is a minimal interval when the dirty pages will be written
        /// to the Persistent Store.
        /// </summary>
        public TimeSpan CheckpointingFrequency { get; set; }

        /// <summary>
        /// Gets or sets the size of the checkpointing page buffer.
        /// </summary>
        public long CheckpointingPageBufferSize
        {
            get { return _checkpointingPageBufferSize ?? DefaultCheckpointingPageBufferSize; }
            set { _checkpointingPageBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the number of threads for checkpointing.
        /// </summary>
        public int CheckpointingThreads { get; set; }
    }
}
