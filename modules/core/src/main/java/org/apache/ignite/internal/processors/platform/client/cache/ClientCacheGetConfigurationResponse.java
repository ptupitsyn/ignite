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

package org.apache.ignite.internal.processors.platform.client.cache;

import org.apache.ignite.binary.BinaryRawWriter;
import org.apache.ignite.cache.CacheKeyConfiguration;
import org.apache.ignite.cache.QueryEntity;
import org.apache.ignite.cache.QueryIndex;
import org.apache.ignite.configuration.CacheConfiguration;
import org.apache.ignite.internal.binary.BinaryRawWriterEx;
import org.apache.ignite.internal.processors.platform.client.ClientResponse;

import java.util.Collection;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

import static org.apache.ignite.internal.processors.platform.utils.PlatformConfigurationUtils.writeEnumByte;
import static org.apache.ignite.internal.processors.platform.utils.PlatformConfigurationUtils.writeEnumInt;

/**
 * Cache configuration response.
 */
public class ClientCacheGetConfigurationResponse extends ClientResponse {
    /** Cache configuration. */
    private final CacheConfiguration cfg;

    /**
     * Constructor.
     *
     * @param reqId Request id.
     * @param cfg Cache configuration.
     */
    ClientCacheGetConfigurationResponse(long reqId, CacheConfiguration cfg) {
        super(reqId);

        assert cfg != null;

        this.cfg = cfg;
    }

    /** {@inheritDoc} */
    @Override public void encode(BinaryRawWriterEx writer) {
        super.encode(writer);

        writeEnumInt(writer, cfg.getAtomicityMode(), CacheConfiguration.DFLT_CACHE_ATOMICITY_MODE);
        writer.writeInt(cfg.getBackups());
        writeEnumInt(writer, cfg.getCacheMode(), CacheConfiguration.DFLT_CACHE_MODE);
        writer.writeBoolean(cfg.isCopyOnRead());
        writer.writeString(cfg.getDataRegionName());
        writer.writeBoolean(cfg.isEagerTtl());
        writer.writeBoolean(cfg.isStatisticsEnabled());
        writer.writeString(cfg.getGroupName());
        writer.writeBoolean(cfg.isInvalidate());
        writer.writeBoolean(cfg.isStoreKeepBinary());
        writer.writeBoolean(cfg.isLoadPreviousValue());
        writer.writeLong(cfg.getDefaultLockTimeout());
        writer.writeInt(cfg.getMaxConcurrentAsyncOperations());
        writer.writeInt(cfg.getMaxQueryIteratorsCount());
        writer.writeString(cfg.getName());
        writer.writeBoolean(cfg.isOnheapCacheEnabled());
        writer.writeInt(cfg.getPartitionLossPolicy().ordinal());
        writer.writeInt(cfg.getQueryDetailMetricsSize());
        writer.writeInt(cfg.getQueryParallelism());
        writer.writeBoolean(cfg.isReadFromBackup());
        writer.writeBoolean(cfg.isReadThrough());
        writer.writeInt(cfg.getRebalanceBatchSize());
        writer.writeLong(cfg.getRebalanceBatchesPrefetchCount());
        writer.writeLong(cfg.getRebalanceDelay());
        writeEnumInt(writer, cfg.getRebalanceMode(), CacheConfiguration.DFLT_REBALANCE_MODE);
        writer.writeInt(cfg.getRebalanceOrder());
        writer.writeLong(cfg.getRebalanceThrottle());
        writer.writeLong(cfg.getRebalanceTimeout());
        writer.writeBoolean(cfg.isSqlEscapeAll());
        writer.writeInt(cfg.getSqlIndexMaxInlineSize());
        writer.writeString(cfg.getSqlSchema());
        writer.writeInt(cfg.getStoreConcurrentLoadAllThreshold());
        writer.writeInt(cfg.getWriteBehindBatchSize());
        writer.writeBoolean(cfg.getWriteBehindCoalescing());
        writer.writeBoolean(cfg.isWriteBehindEnabled());
        writer.writeLong(cfg.getWriteBehindFlushFrequency());
        writer.writeInt(cfg.getWriteBehindFlushSize());
        writer.writeInt(cfg.getWriteBehindFlushThreadCount());
        writeEnumInt(writer, cfg.getWriteSynchronizationMode());
        writer.writeBoolean(cfg.isWriteThrough());

        CacheKeyConfiguration[] keys = cfg.getKeyConfiguration();

        if (keys != null) {
            writer.writeInt(keys.length);

            for (CacheKeyConfiguration key : keys) {
                writer.writeString(key.getTypeName());
                writer.writeString(key.getAffinityKeyFieldName());
            }
        } else {
            writer.writeInt(0);
        }

        //noinspection unchecked
        Collection<QueryEntity> qryEntities = cfg.getQueryEntities();

        if (qryEntities != null) {
            writer.writeInt(qryEntities.size());

            for (QueryEntity e : qryEntities)
                writeQueryEntity(writer, e);
        } else
            writer.writeInt(0);
    }

    /**
     * Write query entity.
     *
     * @param writer Writer.
     * @param queryEntity Query entity.
     */
    private static void writeQueryEntity(BinaryRawWriter writer, QueryEntity queryEntity) {
        assert queryEntity != null;

        // TODO: Refactor all of this.
        writer.writeString(queryEntity.getKeyType());
        writer.writeString(queryEntity.getValueType());
        writer.writeString(queryEntity.getTableName());

        // Fields
        LinkedHashMap<String, String> fields = queryEntity.getFields();

        if (fields != null) {
            Set<String> keyFields = queryEntity.getKeyFields();
            Set<String> notNullFields = queryEntity.getNotNullFields();

            writer.writeInt(fields.size());

            for (Map.Entry<String, String> field : fields.entrySet()) {
                writer.writeString(field.getKey());
                writer.writeString(field.getValue());
                writer.writeBoolean(keyFields != null && keyFields.contains(field.getKey()));
                writer.writeBoolean(notNullFields != null && notNullFields.contains(field.getKey()));
            }
        }
        else
            writer.writeInt(0);

        // Aliases
        Map<String, String> aliases = queryEntity.getAliases();

        if (aliases != null) {
            writer.writeInt(aliases.size());

            for (Map.Entry<String, String> alias : aliases.entrySet()) {
                writer.writeString(alias.getKey());
                writer.writeString(alias.getValue());
            }
        }
        else
            writer.writeInt(0);

        // Indexes
        Collection<QueryIndex> indexes = queryEntity.getIndexes();

        if (indexes != null) {
            writer.writeInt(indexes.size());

            for (QueryIndex index : indexes)
                writeQueryIndex(writer, index);
        }
        else
            writer.writeInt(0);

        writer.writeString(queryEntity.getKeyFieldName());
        writer.writeString(queryEntity.getValueFieldName());
    }

    /**
     * Writer query index.
     *
     * @param writer Writer.
     * @param index Index.
     */
    private static void writeQueryIndex(BinaryRawWriter writer, QueryIndex index) {
        assert index != null;

        writer.writeString(index.getName());
        writeEnumByte(writer, index.getIndexType());
        writer.writeInt(index.getInlineSize());

        LinkedHashMap<String, Boolean> fields = index.getFields();

        if (fields != null) {
            writer.writeInt(fields.size());

            for (Map.Entry<String, Boolean> field : fields.entrySet()) {
                writer.writeString(field.getKey());
                writer.writeBoolean(!field.getValue());
            }
        }
        else
            writer.writeInt(0);
    }
}
