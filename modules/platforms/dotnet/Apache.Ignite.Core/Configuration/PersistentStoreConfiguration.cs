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
    using System.ComponentModel;

    /// <summary>
    /// Configures Apache Ignite persistent store.
    /// </summary>
    public class PersistentStoreConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentStoreConfiguration"/> class.
        /// </summary>
        public PersistentStoreConfiguration()
        {
            CheckpointingPageBufferSize = DefaultCheckpointingPageBufferSize;
            CheckpointingFrequency = DefaultCheckpointingFrequency;
            LockWaitTime = DefaultLockWaitTime;
            WalHistorySize = DefaultWalHistorySize;
            WalSegments = DefaultWalSegments;
            WalSegmentSize = DefaultWalSegmentSize;
            TlbSize = DefaultTlbSize;
            WalFlushFrequency = DefaultWalFlushFrequency;
            WalRecordIteratorBufferSize = DefaultWalRecordIteratorBufferSize;
        }

        /// <summary>
        /// Default value for <see cref="CheckpointingPageBufferSize"/>.
        /// </summary>
        public const long DefaultCheckpointingPageBufferSize = 256L * 1024 * 1024;

        /// <summary>
        /// Default value for <see cref="CheckpointingFrequency"/>.
        /// </summary>
        public static readonly TimeSpan DefaultCheckpointingFrequency = TimeSpan.FromSeconds(180);

        /// <summary>
        /// Default value for <see cref="LockWaitTime"/>.
        /// </summary>
        public static readonly TimeSpan DefaultLockWaitTime = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Default value for <see cref="WalHistorySize"/>.
        /// </summary>
        public const int DefaultWalHistorySize = 20;
        
        /// <summary>
        /// Default value for <see cref="WalSegments"/>.
        /// </summary>
        public const int DefaultWalSegments = 10;

        /// <summary>
        /// Default value for <see cref="WalSegmentSize"/>.
        /// </summary>
        public const int DefaultWalSegmentSize = 64 * 1024 * 1024;

        /// <summary>
        /// Default value for <see cref="TlbSize"/>.
        /// </summary>
        public const int DefaultTlbSize = 128 * 1024;

        /// <summary>
        /// Default value for <see cref="WalFlushFrequency"/>.
        /// </summary>
        public static readonly TimeSpan DefaultWalFlushFrequency = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Default value for <see cref="WalRecordIteratorBufferSize"/>.
        /// </summary>
        public const int DefaultWalRecordIteratorBufferSize = 64 * 1024 * 1024;

        /// <summary>
        /// Gets or sets the path where data and indexes will be persisted.
        /// </summary>
        public string PersistentStorePath { get; set; }

        /// <summary>
        /// Gets or sets the checkpointing frequency which is a minimal interval when the dirty pages will be written
        /// to the Persistent Store.
        /// </summary>
        [DefaultValue(typeof(TimeSpan), "00:03:00")]
        public TimeSpan CheckpointingFrequency { get; set; }

        /// <summary>
        /// Gets or sets the size of the checkpointing page buffer.
        /// </summary>
        [DefaultValue(DefaultCheckpointingPageBufferSize)]
        public long CheckpointingPageBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the number of threads for checkpointing.
        /// </summary>
        public int CheckpointingThreads { get; set; }

        /// <summary>
        /// Gets or sets the persistent manager file lock wait time.
        /// </summary>
        [DefaultValue(typeof(TimeSpan), "00:00:10")]
        public TimeSpan LockWaitTime { get; set; }

        /// <summary>
        /// Gets or sets the number of checkpoints to store in WAL (Write Ahead Log) history.
        /// </summary>
        [DefaultValue(DefaultWalHistorySize)]
        public int WalHistorySize { get;set; }

        /// <summary>
        /// Gets or sets a number of WAL (Write Ahead Log) segments to work with.
        /// For performance reasons, the whole WAL is split into files of fixed length called segments.
        /// </summary>
        [DefaultValue(DefaultWalSegments)]
        public int WalSegments { get;set; }

        /// <summary>
        /// Gets or sets the size of the WAL (Write Ahead Log) segment.
        /// For performance reasons, the whole WAL is split into files of fixed length called segments.
        /// </summary>
        [DefaultValue(DefaultWalSegmentSize)]
        public int WalSegmentSize { get;set; }

        /// <summary>
        /// Gets or sets the path to the directory where WAL (Write Ahead Log) is stored.
        /// </summary>
        public string WalStorePath { get; set; }
        
        /// <summary>
        /// Gets or sets the path to the directory where WAL (Write Ahead Log) archive is stored.
        /// Every WAL segment will be fully copied to this directory before it can be reused for WAL purposes.
        /// </summary>
        public string WalArchivePath { get; set; }

        /// <summary>
        /// Gets or sets the WAL (Write Ahead Log) mode.
        /// </summary>
        public WalMode WalMode { get; set; }

        /// <summary>
        /// Gets or sets the size of the TLB (Thread-Local Buffer), in bytes.
        /// </summary>
        [DefaultValue(DefaultTlbSize)]
        public int TlbSize { get; set; }

        /// <summary>
        /// Gets or sets the WAL (Write Ahead Log) flush frequency.
        /// </summary>
        [DefaultValue(typeof(TimeSpan), "00:00:02")]
        public TimeSpan WalFlushFrequency { get; set; }

        /// <summary>
        /// Gets or sets the WAL (Write Ahead Log) fsync (disk sync) delay.
        /// </summary>
        public TimeSpan WalFsyncDelay { get; set; }  // TODO: Default

        /// <summary>
        /// Gets or sets the size of the WAL (Write Ahead Log) record iterator buffer, in bytes.
        /// </summary>
        [DefaultValue(DefaultWalRecordIteratorBufferSize)]
        public int WalRecordIteratorBufferSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether full pages should always be written.
        /// </summary>
        public bool AlwaysWriteFullPages { get; set; }
    }
}
