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

namespace Apache.Ignite.Core.Impl.Client.Cache
{
    using System.Diagnostics;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Binary.IO;

    /// <summary>
    /// Writes and reads <see cref="CacheConfiguration"/> for thin client mode.
    /// <para />
    /// Thin client supports a subset of <see cref="CacheConfiguration"/> properties, so
    /// <see cref="CacheConfiguration.Read"/> is not suitable.
    /// </summary>
    internal static class ClientCacheConfigurationSerializer
    {
        /// <summary>
        /// Writes the specified config.
        /// </summary>
        public static void Write(CacheConfiguration cfg, IBinaryStream stream)
        {
            Debug.Assert(cfg != null);
            Debug.Assert(stream != null);

            // Configuration should be written with a system marshaller.
            var w = BinaryUtils.Marshaller.StartMarshal(stream);

            w.WriteString(cfg.Name);
            w.WriteInt((int)cfg.AtomicityMode);
            w.WriteInt(cfg.Backups);
            w.WriteInt((int)cfg.CacheMode);
            w.WriteBoolean(cfg.CopyOnRead);
            w.WriteBoolean(cfg.EagerTtl);
            w.WriteBoolean(cfg.Invalidate);
            w.WriteBoolean(cfg.KeepBinaryInStore);
            w.WriteBoolean(cfg.LoadPreviousValue);
            w.WriteTimeSpanAsLong(cfg.LockTimeout);
            w.WriteInt(cfg.MaxConcurrentAsyncOperations);
            w.WriteBoolean(cfg.ReadFromBackup);
            w.WriteInt(cfg.RebalanceBatchSize);
            w.WriteTimeSpanAsLong(cfg.RebalanceDelay);
            w.WriteInt((int)cfg.RebalanceMode);
            w.WriteTimeSpanAsLong(cfg.RebalanceThrottle);
            w.WriteTimeSpanAsLong(cfg.RebalanceTimeout);
            w.WriteBoolean(cfg.SqlEscapeAll);
            w.WriteInt(cfg.WriteBehindBatchSize);
            w.WriteBoolean(cfg.WriteBehindEnabled);
            w.WriteTimeSpanAsLong(cfg.WriteBehindFlushFrequency);
            w.WriteInt(cfg.WriteBehindFlushSize);
            w.WriteInt(cfg.WriteBehindFlushThreadCount);
            w.WriteBoolean(cfg.WriteBehindCoalescing);
            w.WriteInt((int)cfg.WriteSynchronizationMode);
            w.WriteBoolean(cfg.ReadThrough);
            w.WriteBoolean(cfg.WriteThrough);
            w.WriteBoolean(cfg.EnableStatistics);
            w.WriteString(cfg.DataRegionName);
            w.WriteInt((int)cfg.PartitionLossPolicy);
            w.WriteString(cfg.GroupName);
            w.WriteInt(cfg.SqlIndexMaxInlineSize);
            w.WriteCollectionRaw(cfg.QueryEntities);

            // TODO: Throw on unsupported properties.
        }

        /// <summary>
        /// Reads the config.
        /// </summary>
        public static CacheConfiguration Read(IBinaryStream stream)
        {
            Debug.Assert(stream != null);

            var reader = BinaryUtils.Marshaller.StartUnmarshal(stream);

            return new CacheConfiguration
            {
                AtomicityMode = (CacheAtomicityMode) reader.ReadInt(),
                Backups = reader.ReadInt(),
                CacheMode = (CacheMode) reader.ReadInt(),
                CopyOnRead = reader.ReadBoolean(),
                EagerTtl = reader.ReadBoolean(),
                Invalidate = reader.ReadBoolean(),
                KeepBinaryInStore = reader.ReadBoolean(),
                LoadPreviousValue = reader.ReadBoolean(),
                LockTimeout = reader.ReadLongAsTimespan(),
                MaxConcurrentAsyncOperations = reader.ReadInt(),
                Name = reader.ReadString(),
                ReadFromBackup = reader.ReadBoolean(),
                RebalanceBatchSize = reader.ReadInt(),
                RebalanceDelay = reader.ReadLongAsTimespan(),
                RebalanceMode = (CacheRebalanceMode) reader.ReadInt(),
                RebalanceThrottle = reader.ReadLongAsTimespan(),
                RebalanceTimeout = reader.ReadLongAsTimespan(),
                SqlEscapeAll = reader.ReadBoolean(),
                WriteBehindBatchSize = reader.ReadInt(),
                WriteBehindEnabled = reader.ReadBoolean(),
                WriteBehindFlushFrequency = reader.ReadLongAsTimespan(),
                WriteBehindFlushSize = reader.ReadInt(),
                WriteBehindFlushThreadCount = reader.ReadInt(),
                WriteBehindCoalescing = reader.ReadBoolean(),
                WriteSynchronizationMode = (CacheWriteSynchronizationMode) reader.ReadInt(),
                ReadThrough = reader.ReadBoolean(),
                WriteThrough = reader.ReadBoolean(),
                EnableStatistics = reader.ReadBoolean(),
                DataRegionName = reader.ReadString(),
                PartitionLossPolicy = (PartitionLossPolicy) reader.ReadInt(),
                GroupName = reader.ReadString(),
                SqlIndexMaxInlineSize = reader.ReadInt(),
                OnheapCacheEnabled = reader.ReadBoolean(),
                StoreConcurrentLoadAllThreshold = reader.ReadInt(),
                RebalanceOrder = reader.ReadInt(),
                RebalanceBatchesPrefetchCount = reader.ReadLong(),
                MaxQueryIteratorsCount = reader.ReadInt(),
                QueryDetailMetricsSize = reader.ReadInt(),
                QueryParallelism = reader.ReadInt(),
                SqlSchema = reader.ReadString(),
                QueryEntities = reader.ReadCollectionRaw(r => new QueryEntity(r)),
                KeyConfiguration = reader.ReadCollectionRaw(r => new CacheKeyConfiguration(r))
            };
        }
    }
}
