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
    using System;
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
        public static void Write(IBinaryStream stream, CacheConfiguration cfg)
        {
            Debug.Assert(stream != null);
            Debug.Assert(cfg != null);

            // Configuration should be written with a system marshaller.
            var writer = BinaryUtils.Marshaller.StartMarshal(stream);

            writer.WriteInt((int) cfg.AtomicityMode);
            writer.WriteInt(cfg.Backups);
            writer.WriteInt((int) cfg.CacheMode);
            writer.WriteBoolean(cfg.CopyOnRead);
            writer.WriteString(cfg.DataRegionName);
            writer.WriteBoolean(cfg.EagerTtl);
            writer.WriteBoolean(cfg.EnableStatistics);
            writer.WriteString(cfg.GroupName);
            writer.WriteBoolean(cfg.Invalidate);
            writer.WriteBoolean(cfg.KeepBinaryInStore);
            writer.WriteBoolean(cfg.LoadPreviousValue);
            writer.WriteTimeSpanAsLong(cfg.LockTimeout);
            writer.WriteInt(cfg.MaxConcurrentAsyncOperations);
            writer.WriteInt(cfg.MaxQueryIteratorsCount);
            writer.WriteString(cfg.Name);
            writer.WriteBoolean(cfg.OnheapCacheEnabled);
            writer.WriteInt((int) cfg.PartitionLossPolicy);
            writer.WriteInt(cfg.QueryDetailMetricsSize);
            writer.WriteInt(cfg.QueryParallelism);
            writer.WriteBoolean(cfg.ReadFromBackup);
            writer.WriteBoolean(cfg.ReadThrough);
            writer.WriteLong(cfg.RebalanceBatchesPrefetchCount);
            writer.WriteInt(cfg.RebalanceBatchSize);
            writer.WriteTimeSpanAsLong(cfg.RebalanceDelay);
            writer.WriteInt((int) cfg.RebalanceMode);
            writer.WriteInt(cfg.RebalanceOrder);
            writer.WriteTimeSpanAsLong(cfg.RebalanceThrottle);
            writer.WriteTimeSpanAsLong(cfg.RebalanceTimeout);
            writer.WriteBoolean(cfg.SqlEscapeAll);
            writer.WriteInt(cfg.SqlIndexMaxInlineSize);
            writer.WriteString(cfg.SqlSchema);
            writer.WriteInt(cfg.StoreConcurrentLoadAllThreshold);
            writer.WriteInt(cfg.WriteBehindBatchSize);
            writer.WriteBoolean(cfg.WriteBehindCoalescing);
            writer.WriteBoolean(cfg.WriteBehindEnabled);
            writer.WriteTimeSpanAsLong(cfg.WriteBehindFlushFrequency);
            writer.WriteInt(cfg.WriteBehindFlushSize);
            writer.WriteInt(cfg.WriteBehindFlushThreadCount);
            writer.WriteInt((int) cfg.WriteSynchronizationMode);
            writer.WriteBoolean(cfg.WriteThrough);

            writer.WriteCollectionRaw(cfg.KeyConfiguration);
            writer.WriteCollectionRaw(cfg.QueryEntities);

            ThrowUnsupportedIfNotNull(cfg.AffinityFunction, "AffinityFunction");
            ThrowUnsupportedIfNotNull(cfg.EvictionPolicy, "EvictionPolicy");
            ThrowUnsupportedIfNotNull(cfg.ExpiryPolicyFactory, "ExpiryPolicyFactory");
            ThrowUnsupportedIfNotNull(cfg.PluginConfigurations, "PluginConfigurations");
            ThrowUnsupportedIfNotNull(cfg.CacheStoreFactory, "CacheStoreFactory");
        }

        /// <summary>
        /// Reads the config.
        /// </summary>
        public static CacheConfiguration Read(IBinaryStream stream)
        {
            Debug.Assert(stream != null);

            // Configuration should be read with system marshaller.
            var reader = BinaryUtils.Marshaller.StartUnmarshal(stream);

            return new CacheConfiguration
            {
                AtomicityMode = (CacheAtomicityMode)reader.ReadInt(),
                Backups = reader.ReadInt(),
                CacheMode = (CacheMode)reader.ReadInt(),
                CopyOnRead = reader.ReadBoolean(),
                DataRegionName = reader.ReadString(),
                EagerTtl = reader.ReadBoolean(),
                EnableStatistics = reader.ReadBoolean(),
                GroupName = reader.ReadString(),
                Invalidate = reader.ReadBoolean(),
                KeepBinaryInStore = reader.ReadBoolean(),
                LoadPreviousValue = reader.ReadBoolean(),
                LockTimeout = reader.ReadLongAsTimespan(),
                MaxConcurrentAsyncOperations = reader.ReadInt(),
                MaxQueryIteratorsCount = reader.ReadInt(),
                Name = reader.ReadString(),
                OnheapCacheEnabled = reader.ReadBoolean(),
                PartitionLossPolicy = (PartitionLossPolicy)reader.ReadInt(),
                QueryDetailMetricsSize = reader.ReadInt(),
                QueryParallelism = reader.ReadInt(),
                ReadFromBackup = reader.ReadBoolean(),
                ReadThrough = reader.ReadBoolean(),
                RebalanceBatchSize = reader.ReadInt(),
                RebalanceBatchesPrefetchCount = reader.ReadLong(),
                RebalanceDelay = reader.ReadLongAsTimespan(),
                RebalanceMode = (CacheRebalanceMode)reader.ReadInt(),
                RebalanceOrder = reader.ReadInt(),
                RebalanceThrottle = reader.ReadLongAsTimespan(),
                RebalanceTimeout = reader.ReadLongAsTimespan(),
                SqlEscapeAll = reader.ReadBoolean(),
                SqlIndexMaxInlineSize = reader.ReadInt(),
                SqlSchema = reader.ReadString(),
                StoreConcurrentLoadAllThreshold = reader.ReadInt(),
                WriteBehindBatchSize = reader.ReadInt(),
                WriteBehindCoalescing = reader.ReadBoolean(),
                WriteBehindEnabled = reader.ReadBoolean(),
                WriteBehindFlushFrequency = reader.ReadLongAsTimespan(),
                WriteBehindFlushSize = reader.ReadInt(),
                WriteBehindFlushThreadCount = reader.ReadInt(),
                WriteSynchronizationMode = (CacheWriteSynchronizationMode)reader.ReadInt(),
                WriteThrough = reader.ReadBoolean(),

                KeyConfiguration = reader.ReadCollectionRaw(r => new CacheKeyConfiguration(r)),
                QueryEntities = reader.ReadCollectionRaw(r => new QueryEntity(r)),
            };
        }

        /// <summary>
        /// Throws the unsupported exception if property is not null.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyName">Name of the property.</param>
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void ThrowUnsupportedIfNotNull(object obj, string propertyName)
        {
            if (obj != null)
            {
                throw new NotSupportedException(
                    string.Format("{0}.{1} property is not supported in thin client mode.",
                        typeof(CacheConfiguration).Name, propertyName));
            }
        }
    }
}
