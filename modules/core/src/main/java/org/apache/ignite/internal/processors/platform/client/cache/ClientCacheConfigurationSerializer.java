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

import org.apache.ignite.binary.BinaryRawReader;
import org.apache.ignite.binary.BinaryRawWriter;
import org.apache.ignite.cache.CacheAtomicityMode;
import org.apache.ignite.cache.CacheKeyConfiguration;
import org.apache.ignite.cache.CacheMode;
import org.apache.ignite.cache.CacheRebalanceMode;
import org.apache.ignite.cache.CacheWriteSynchronizationMode;
import org.apache.ignite.cache.PartitionLossPolicy;
import org.apache.ignite.cache.QueryEntity;
import org.apache.ignite.configuration.CacheConfiguration;
import org.apache.ignite.internal.binary.BinaryRawWriterEx;

import java.util.ArrayList;
import java.util.Collection;

import static org.apache.ignite.internal.processors.platform.utils.PlatformConfigurationUtils.readQueryEntity;
import static org.apache.ignite.internal.processors.platform.utils.PlatformConfigurationUtils.writeEnumInt;
import static org.apache.ignite.internal.processors.platform.utils.PlatformConfigurationUtils.writeQueryEntity;

/**
 * Cache configuration serializer.
 */
public class ClientCacheConfigurationSerializer {
    /** */
    private static final short ATOMICITY_MODE = 0;

    /** */
    private static final short BACKUPS = 1;

    /** */
    private static final short CACHE_MODE = 2;

    /** */
    private static final short COPY_ON_READ = 3;

    /** */
    private static final short DATA_REGION_NAME = 4;

    /** */
    private static final short EAGER_TTL = 5;

    /** */
    private static final short STATISTICS_ENABLED = 6;

    /** */
    private static final short GROUP_NAME = 7;

    /** */
    private static final short INVALIDATE = 8;

    /** */
    private static final short DEFAULT_LOCK_TIMEOUT = 9;

    /** */
    private static final short MAX_CONCURRENT_ASYNC_OPERATIONS = 10;

    /** */
    private static final short MAX_QUERY_ITERATORS_COUNT = 11;

    /** */
    private static final short NAME = 12;

    /** */
    private static final short ONHEAP_CACHE_ENABLED = 13;

    /** */
    private static final short PARTITION_LOSS_POLICY = 14;

    /** */
    private static final short QUERY_DETAIL_METRICS_SIZE = 15;

    /** */
    private static final short QUERY_PARALLELISM = 16;

    /** */
    private static final short READ_FROM_BACKUP = 17;

    /** */
    private static final short REBALANCE_BATCH_SIZE = 18;

    /** */
    private static final short REBALANCE_BATCHES_PREFETCH_COUNT = 19;

    /** */
    private static final short REBALANCE_DELAY = 20;

    /** */
    private static final short REBALANCE_MODE = 21;

    /** */
    private static final short REBALANCE_ORDER = 22;

    /** */
    private static final short REBALANCE_THROTTLE = 23;

    /** */
    private static final short REBALANCE_TIMEOUT = 24;

    /** */
    private static final short SQL_ESCAPE_ALL = 25;

    /** */
    private static final short SQL_INDEX_MAX_INLINE_SIZE = 26;

    /** */
    private static final short SQL_SCHEMA = 27;

    /** */
    private static final short WRITE_SYNCHRONIZATION_MODE = 28;

    /** */
    private static final short KEY_CONFIGURATION = 29;

    /** */
    private static final short QUERY_ENTITIES = 30;

    /**
     * Writes the cache configuration.
     * @param writer Writer.
     * @param cfg Configuration.
     */
    static void write(BinaryRawWriterEx writer, CacheConfiguration cfg) {
        assert writer != null;
        assert cfg != null;

        // Reserve for length.
        int pos = writer.reserveInt();

        writeEnumInt(writer, cfg.getAtomicityMode(), CacheConfiguration.DFLT_CACHE_ATOMICITY_MODE);
        writer.writeInt(cfg.getBackups());
        writeEnumInt(writer, cfg.getCacheMode(), CacheConfiguration.DFLT_CACHE_MODE);
        writer.writeBoolean(cfg.isCopyOnRead());
        writer.writeString(cfg.getDataRegionName());
        writer.writeBoolean(cfg.isEagerTtl());
        writer.writeBoolean(cfg.isStatisticsEnabled());
        writer.writeString(cfg.getGroupName());
        writer.writeBoolean(cfg.isInvalidate());
        writer.writeLong(cfg.getDefaultLockTimeout());
        writer.writeInt(cfg.getMaxConcurrentAsyncOperations());
        writer.writeInt(cfg.getMaxQueryIteratorsCount());
        writer.writeString(cfg.getName());
        writer.writeBoolean(cfg.isOnheapCacheEnabled());
        writer.writeInt(cfg.getPartitionLossPolicy().ordinal());
        writer.writeInt(cfg.getQueryDetailMetricsSize());
        writer.writeInt(cfg.getQueryParallelism());
        writer.writeBoolean(cfg.isReadFromBackup());
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
        writeEnumInt(writer, cfg.getWriteSynchronizationMode());

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

        // Write length (so that part of the config can be skipped).
        writer.writeInt(pos, writer.out().position() - pos - 4);
    }

    /**
     * Reads the cache configuration.
     *
     * @param reader Reader.
     * @return Configuration.
     */
    static CacheConfiguration read(BinaryRawReader reader) {
        reader.readInt();  // Skip length.

        CacheConfiguration cfg = new CacheConfiguration()
                .setAtomicityMode(CacheAtomicityMode.fromOrdinal(reader.readInt()))
                .setBackups(reader.readInt())
                .setCacheMode(CacheMode.fromOrdinal(reader.readInt()))
                .setCopyOnRead(reader.readBoolean())
                .setDataRegionName(reader.readString())
                .setEagerTtl(reader.readBoolean())
                .setStatisticsEnabled(reader.readBoolean())
                .setGroupName(reader.readString())
                .setInvalidate(reader.readBoolean())
                .setDefaultLockTimeout(reader.readLong())
                .setMaxConcurrentAsyncOperations(reader.readInt())
                .setMaxQueryIteratorsCount(reader.readInt())
                .setName(reader.readString())
                .setOnheapCacheEnabled(reader.readBoolean())
                .setPartitionLossPolicy(PartitionLossPolicy.fromOrdinal((byte)reader.readInt()))
                .setQueryDetailMetricsSize(reader.readInt())
                .setQueryParallelism(reader.readInt())
                .setReadFromBackup(reader.readBoolean())
                .setRebalanceBatchSize(reader.readInt())
                .setRebalanceBatchesPrefetchCount(reader.readLong())
                .setRebalanceDelay(reader.readLong())
                .setRebalanceMode(CacheRebalanceMode.fromOrdinal(reader.readInt()))
                .setRebalanceOrder(reader.readInt())
                .setRebalanceThrottle(reader.readLong())
                .setRebalanceTimeout(reader.readLong())
                .setSqlEscapeAll(reader.readBoolean())
                .setSqlIndexMaxInlineSize(reader.readInt())
                .setSqlSchema(reader.readString())
                .setWriteSynchronizationMode(CacheWriteSynchronizationMode.fromOrdinal(reader.readInt()));

        // Key configuration.
        int keyCnt = reader.readInt();

        if (keyCnt > 0) {
            CacheKeyConfiguration[] keys = new CacheKeyConfiguration[keyCnt];

            for (int i = 0; i < keyCnt; i++) {
                keys[i] = new CacheKeyConfiguration(reader.readString(), reader.readString());
            }

            cfg.setKeyConfiguration(keys);
        }

        // Query entities.
        int qryEntCnt = reader.readInt();

        if (qryEntCnt > 0) {
            Collection<QueryEntity> entities = new ArrayList<>(qryEntCnt);

            for (int i = 0; i < qryEntCnt; i++)
                entities.add(readQueryEntity(reader));

            cfg.setQueryEntities(entities);
        }

        return cfg;
    }
}
