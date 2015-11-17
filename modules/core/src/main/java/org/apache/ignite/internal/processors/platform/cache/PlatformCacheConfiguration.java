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
    /**
     * Writes cache configuration.
     *
     * @param writer Writer.
     * @param cfg Configuration.
     */
    public static void writeCacheConfiguration(BinaryRawWriterEx writer, CacheConfiguration cfg) {
        writer.writeInt(cfg.getAtomicityMode().ordinal());
        writer.writeInt(cfg.getAtomicWriteOrderMode().ordinal());
        writer.writeInt(cfg.getBackups());
        writer.writeInt(cfg.getCacheMode().ordinal());
        writer.writeBoolean(cfg.isCopyOnRead());
        writer.writeBoolean(cfg.isEagerTtl());
        writer.writeBoolean(cfg.isSwapEnabled());
        writer.writeBoolean(cfg.isEvictSynchronized());
        writer.writeInt(cfg.getEvictSynchronizedConcurrencyLevel());
        writer.writeInt(cfg.getEvictSynchronizedKeyBufferSize());
        writer.writeLong(cfg.getEvictSynchronizedTimeout());
        writer.writeBoolean(cfg.isInvalidate());
        writer.writeBoolean(cfg.isKeepPortableInStore());
        writer.writeBoolean(cfg.isLoadPreviousValue());
        writer.writeLong(cfg.getDefaultLockTimeout());
        writer.writeLong(cfg.getLongQueryWarningTimeout());
        writer.writeInt(cfg.getMaxConcurrentAsyncOperations());
        writer.writeFloat(cfg.getEvictMaxOverflowRatio());
        writer.writeInt(cfg.getMemoryMode().ordinal());
        writer.writeString(cfg.getName());
        writer.writeLong(cfg.getOffHeapMaxMemory());
        writer.writeBoolean(cfg.isReadFromBackup());
        writer.writeInt(cfg.getRebalanceBatchSize());
        writer.writeLong(cfg.getRebalanceDelay());
        writer.writeInt(cfg.getRebalanceMode().ordinal());
        writer.writeInt(cfg.getRebalanceThreadPoolSize());
        writer.writeLong(cfg.getRebalanceThrottle());
        writer.writeLong(cfg.getRebalanceTimeout());
        writer.writeBoolean(cfg.isSqlEscapeAll());
        writer.writeInt(cfg.getSqlOnheapRowCacheSize());
        writer.writeInt(cfg.getStartSize());
        writer.writeInt(cfg.getWriteBehindBatchSize());
        writer.writeBoolean(cfg.isWriteBehindEnabled());
        writer.writeLong(cfg.getWriteBehindFlushFrequency());
        writer.writeInt(cfg.getWriteBehindFlushSize());
        writer.writeInt(cfg.getWriteBehindFlushThreadCount());
        writer.writeInt(cfg.getWriteSynchronizationMode().ordinal());

        writeCacheTypeMeta(writer, cfg.getTypeMetadata());
    }

    /**
     * Reads cache configurations from a stream and updates provided IgniteConfiguration.
     *
     * @param cfg IgniteConfiguration to update.
     * @param in Reader.
     */
    public static void readCacheConfigurations(BinaryReaderExImpl in, IgniteConfiguration cfg) {
        int len = in.readInt();

        if (len == 0)
            return;

        List<CacheConfiguration> caches = new ArrayList<>();

        for (int i = 0; i < len; i++) {
            CacheConfiguration ccfg = new CacheConfiguration();

            ccfg.setAtomicityMode(CacheAtomicityMode.fromOrdinal(in.readInt()));
            ccfg.setAtomicWriteOrderMode(CacheAtomicWriteOrderMode.fromOrdinal((byte) in.readInt()));
            ccfg.setBackups(in.readInt());
            ccfg.setCacheMode(CacheMode.fromOrdinal(in.readInt()));
            ccfg.setCopyOnRead(in.readBoolean());
            ccfg.setEagerTtl(in.readBoolean());
            ccfg.setSwapEnabled(in.readBoolean());
            ccfg.setEvictSynchronized(in.readBoolean());
            ccfg.setEvictSynchronizedConcurrencyLevel(in.readInt());
            ccfg.setEvictSynchronizedKeyBufferSize(in.readInt());
            ccfg.setEvictSynchronizedTimeout(in.readLong());
            ccfg.setInvalidate(in.readBoolean());
            ccfg.setKeepPortableInStore(in.readBoolean());
            ccfg.setLoadPreviousValue(in.readBoolean());
            ccfg.setDefaultLockTimeout(in.readLong());
            ccfg.setLongQueryWarningTimeout(in.readLong());
            ccfg.setMaxConcurrentAsyncOperations(in.readInt());
            ccfg.setEvictMaxOverflowRatio(in.readFloat());
            ccfg.setMemoryMode(CacheMemoryMode.values()[in.readInt()]);
            ccfg.setName(in.readString());
            ccfg.setOffHeapMaxMemory(in.readLong());
            ccfg.setReadFromBackup(in.readBoolean());
            ccfg.setRebalanceBatchSize(in.readInt());
            ccfg.setRebalanceDelay(in.readLong());
            ccfg.setRebalanceMode(CacheRebalanceMode.fromOrdinal(in.readInt()));
            ccfg.setRebalanceThreadPoolSize(in.readInt());
            ccfg.setRebalanceThrottle(in.readLong());
            ccfg.setRebalanceTimeout(in.readLong());
            ccfg.setSqlEscapeAll(in.readBoolean());
            ccfg.setSqlOnheapRowCacheSize(in.readInt());
            ccfg.setStartSize(in.readInt());
            ccfg.setWriteBehindBatchSize(in.readInt());
            ccfg.setWriteBehindEnabled(in.readBoolean());
            ccfg.setWriteBehindFlushFrequency(in.readLong());
            ccfg.setWriteBehindFlushSize(in.readInt());
            ccfg.setWriteBehindFlushThreadCount(in.readInt());
            ccfg.setWriteSynchronizationMode(CacheWriteSynchronizationMode.fromOrdinal(in.readInt()));

            // TODO: new API is not yet finalized.
            // ccfg.setQueryEntities()

            caches.add(ccfg);
        }

        CacheConfiguration[] oldCaches = cfg.getCacheConfiguration();
        CacheConfiguration[] caches0 = caches.toArray(new CacheConfiguration[caches.size()]);

        if (oldCaches == null)
            cfg.setCacheConfiguration(caches0);
        else {
            CacheConfiguration[] mergedCaches = new CacheConfiguration[oldCaches.length + caches.size()];

            System.arraycopy(oldCaches, 0, mergedCaches, 0, oldCaches.length);
            System.arraycopy(caches0, 0, mergedCaches, oldCaches.length, caches.size());

            cfg.setCacheConfiguration(mergedCaches);
        }
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
     * Read cache type metadata.
     *
     * @param reader Reader
     * @return Metadata.
     */
    private static Collection<CacheTypeMetadata> readCacheTypeMeta(BinaryRawReaderEx reader) {
        int count = reader.readInt();

        if (count == 0)
            return null;

        assert count > 0;

        ArrayList<CacheTypeMetadata> res = new ArrayList<>(count);

        for (int i = 0; i < count; i++) {
            CacheTypeMetadata meta = new CacheTypeMetadata();

            meta.setDatabaseSchema(reader.readString());
            meta.setDatabaseTable(reader.readString());

            res.add(meta);
        }

        return res;
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
