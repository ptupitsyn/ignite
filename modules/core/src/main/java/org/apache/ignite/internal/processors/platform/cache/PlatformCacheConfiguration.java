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

package org.apache.ignite.internal.processors.platform.cache;

import org.apache.ignite.cache.*;
import org.apache.ignite.configuration.*;
import org.apache.ignite.internal.portable.*;

import java.util.*;

/**
 * Platform CacheConfiguration utils.
 */
public class PlatformCacheConfiguration {
    public static void writeCacheConfiguration(BinaryRawWriterEx writer, CacheConfiguration ccfg) {
        writer.writeInt(ccfg.getAtomicityMode().ordinal());
        writer.writeInt(ccfg.getAtomicWriteOrderMode().ordinal());
        writer.writeInt(ccfg.getBackups());
        writer.writeInt(ccfg.getCacheMode().ordinal());
        writer.writeBoolean(ccfg.isCopyOnRead());
        writer.writeBoolean(ccfg.isEagerTtl());
        writer.writeBoolean(ccfg.isSwapEnabled());
        writer.writeBoolean(ccfg.isEvictSynchronized());
        writer.writeInt(ccfg.getEvictSynchronizedConcurrencyLevel());
        writer.writeInt(ccfg.getEvictSynchronizedKeyBufferSize());
        writer.writeLong(ccfg.getEvictSynchronizedTimeout());
        writer.writeBoolean(ccfg.isInvalidate());
        writer.writeBoolean(ccfg.isKeepPortableInStore());
        writer.writeBoolean(ccfg.isLoadPreviousValue());
        writer.writeLong(ccfg.getDefaultLockTimeout());
        writer.writeLong(ccfg.getLongQueryWarningTimeout());
        writer.writeInt(ccfg.getMaxConcurrentAsyncOperations());
        writer.writeFloat(ccfg.getEvictMaxOverflowRatio());
        writer.writeInt(ccfg.getMemoryMode().ordinal());
        writer.writeString(ccfg.getName());
        writer.writeLong(ccfg.getOffHeapMaxMemory());
        writer.writeBoolean(ccfg.isReadFromBackup());
        writer.writeInt(ccfg.getRebalanceBatchSize());
        writer.writeLong(ccfg.getRebalanceDelay());
        writer.writeInt(ccfg.getRebalanceMode().ordinal());
        writer.writeInt(ccfg.getRebalanceThreadPoolSize());
        writer.writeLong(ccfg.getRebalanceThrottle());
        writer.writeLong(ccfg.getRebalanceTimeout());
        writer.writeBoolean(ccfg.isSqlEscapeAll());
        writer.writeInt(ccfg.getSqlOnheapRowCacheSize());
        writer.writeInt(ccfg.getStartSize());
        writer.writeInt(ccfg.getWriteBehindBatchSize());
        writer.writeBoolean(ccfg.isWriteBehindEnabled());
        writer.writeLong(ccfg.getWriteBehindFlushFrequency());
        writer.writeInt(ccfg.getWriteBehindFlushSize());
        writer.writeInt(ccfg.getWriteBehindFlushThreadCount());
        writer.writeInt(ccfg.getWriteSynchronizationMode().ordinal());

        writeCacheTypeMeta(writer, ccfg.getTypeMetadata());
    }

    /**
     * Write cache type metadata.
     *
     * @param writer Writer.
     * @param typeMeta Metadata.
     */
    private static void writeCacheTypeMeta(BinaryRawWriterEx writer, Collection<CacheTypeMetadata> typeMeta) {
        if (typeMeta != null) {
            writer.writeInt(typeMeta.size());

            for (CacheTypeMetadata meta : typeMeta) {
                writer.writeString(meta.getDatabaseSchema());
                writer.writeString(meta.getDatabaseTable());

                writer.writeString(meta.getKeyType());
                writer.writeString(meta.getValueType());

                writeMap(writer, meta.getQueryFields());
                writeCollection(writer, meta.getTextFields());
                writeMap(writer, meta.getAscendingFields());
                writeMap(writer, meta.getDescendingFields());
                writeStringMap(writer, meta.getAliases());
            }
        }
        else
            writer.writeInt(0);
    }

    /**
     * Writes a map.
     *
     * @param writer Writer
     * @param map Map.
     */
    private static void writeMap(BinaryRawWriterEx writer, Map<String, Class<?>> map) {
        if (map != null) {
            writer.writeInt(map.size());

            for (Map.Entry<String, Class<?>> e : map.entrySet()) {
                writer.writeString(e.getKey());
                writer.writeString(e.getValue().getName());
            }
        }
        else
            writer.writeInt(0);
    }

    /**
     * Writes a map.
     *
     * @param writer Writer
     * @param map Map.
     */
    private static void writeStringMap(BinaryRawWriterEx writer, Map<String, String> map) {
        if (map != null) {
            writer.writeInt(map.size());

            for (Map.Entry<String, String> e : map.entrySet()) {
                writer.writeString(e.getKey());
                writer.writeString(e.getValue());
            }
        }
        else
            writer.writeInt(0);
    }

    /**
     * Writes a collection.
     *
     * @param writer Writer
     * @param col Collection.
     */
    private static void writeCollection(BinaryRawWriterEx writer, Collection col) {
        if (col != null) {
            writer.writeInt(col.size());

            for (Object e : col)
                writer.writeObject(e);
        }
        else
            writer.writeInt(0);
    }
}
