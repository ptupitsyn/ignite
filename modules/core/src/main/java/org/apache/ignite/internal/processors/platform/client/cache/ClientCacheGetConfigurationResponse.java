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

import org.apache.ignite.configuration.CacheConfiguration;
import org.apache.ignite.internal.binary.BinaryRawWriterEx;
import org.apache.ignite.internal.processors.platform.client.ClientResponse;

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
        writer.writeLong(cfg.getDefaultLockTimeout());
        writer.writeBoolean(cfg.isEagerTtl());
        writer.writeString(cfg.getGroupName());
        writer.writeBoolean(cfg.isInvalidate());
        writer.writeBoolean(cfg.isLoadPreviousValue());
        writer.writeInt(cfg.getMaxConcurrentAsyncOperations());
        writer.writeInt(cfg.getMaxQueryIteratorsCount());
        writer.writeString(cfg.getName());
        writer.writeBoolean(cfg.isOnheapCacheEnabled());
        writer.writeInt(cfg.getPartitionLossPolicy().ordinal());
        writer.writeInt(cfg.getQueryDetailMetricsSize());
        writer.writeInt(cfg.getQueryParallelism());
        writer.writeBoolean(cfg.isReadFromBackup());
        writer.writeBoolean(cfg.isReadThrough());
        writer.writeLong(cfg.getRebalanceBatchesPrefetchCount());
        writer.writeInt(cfg.getRebalanceBatchSize());
        writer.writeLong(cfg.getRebalanceDelay());
        writeEnumInt(writer, cfg.getRebalanceMode(), CacheConfiguration.DFLT_REBALANCE_MODE);
        writer.writeInt(cfg.getRebalanceOrder());
        writer.writeLong(cfg.getRebalanceThrottle());
        writer.writeLong(cfg.getRebalanceTimeout());
        writer.writeBoolean(cfg.isSqlEscapeAll());
        writer.writeInt(cfg.getSqlIndexMaxInlineSize());
        writer.writeString(cfg.getSqlSchema());
        writer.writeBoolean(cfg.isStatisticsEnabled());
        writer.writeInt(cfg.getStoreConcurrentLoadAllThreshold());
        writer.writeBoolean(cfg.isStoreKeepBinary());
        writer.writeInt(cfg.getWriteBehindBatchSize());
        writer.writeBoolean(cfg.getWriteBehindCoalescing());
        writer.writeBoolean(cfg.isWriteBehindEnabled());
        writer.writeLong(cfg.getWriteBehindFlushFrequency());
        writer.writeInt(cfg.getWriteBehindFlushSize());
        writer.writeInt(cfg.getWriteBehindFlushThreadCount());
        writeEnumInt(writer, cfg.getWriteSynchronizationMode());
        writer.writeBoolean(cfg.isWriteThrough());
    }
}
