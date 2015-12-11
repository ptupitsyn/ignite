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

package org.apache.ignite.internal.processors.platform.utils;

import org.apache.ignite.binary.BinaryRawReader;
import org.apache.ignite.binary.BinaryRawWriter;
import org.apache.ignite.cache.CacheAtomicWriteOrderMode;
import org.apache.ignite.cache.CacheAtomicityMode;
import org.apache.ignite.cache.CacheMemoryMode;
import org.apache.ignite.cache.CacheMode;
import org.apache.ignite.cache.CacheRebalanceMode;
import org.apache.ignite.cache.CacheWriteSynchronizationMode;
import org.apache.ignite.cache.QueryEntity;
import org.apache.ignite.configuration.CacheConfiguration;
import org.apache.ignite.configuration.IgniteConfiguration;
import org.apache.ignite.internal.portable.BinaryRawReaderEx;
import org.apache.ignite.internal.portable.BinaryRawWriterEx;
import org.apache.ignite.platform.dotnet.PlatformDotNetBinaryConfiguration;
import org.apache.ignite.platform.dotnet.PlatformDotNetBinaryTypeConfiguration;
import org.apache.ignite.platform.dotnet.PlatformDotNetCacheStoreFactoryNative;
import org.apache.ignite.platform.dotnet.PlatformDotNetConfiguration;
import org.apache.ignite.spi.discovery.DiscoverySpi;
import org.apache.ignite.spi.discovery.tcp.TcpDiscoverySpi;
import org.apache.ignite.spi.discovery.tcp.ipfinder.TcpDiscoveryIpFinder;
import org.apache.ignite.spi.discovery.tcp.ipfinder.multicast.TcpDiscoveryMulticastIpFinder;
import org.apache.ignite.spi.discovery.tcp.ipfinder.vm.TcpDiscoveryVmIpFinder;

import java.lang.management.ManagementFactory;
import java.net.InetSocketAddress;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

/**
 * Configuration utils.
 */
public class PlatformConfigurationUtils {
    /**
     * Write .Net configuration to the stream.
     *
     * @param writer Writer.
     * @param cfg Configuration.
     */
    public static void writeDotNetConfiguration(BinaryRawWriterEx writer, PlatformDotNetConfiguration cfg) {
        // 1. Write assemblies.
        PlatformUtils.writeNullableCollection(writer, cfg.getAssemblies());

        PlatformDotNetBinaryConfiguration binaryCfg = cfg.getBinaryConfiguration();

        if (binaryCfg != null) {
            writer.writeBoolean(true);

            PlatformUtils.writeNullableCollection(writer, binaryCfg.getTypesConfiguration(),
                new PlatformWriterClosure<PlatformDotNetBinaryTypeConfiguration>() {
                    @Override public void write(BinaryRawWriterEx writer, PlatformDotNetBinaryTypeConfiguration typ) {
                        writer.writeString(typ.getTypeName());
                        writer.writeString(typ.getNameMapper());
                        writer.writeString(typ.getIdMapper());
                        writer.writeString(typ.getSerializer());
                        writer.writeString(typ.getAffinityKeyFieldName());
                        writer.writeObject(typ.getKeepDeserialized());
                        writer.writeBoolean(typ.isEnum());
                    }
                });

            PlatformUtils.writeNullableCollection(writer, binaryCfg.getTypes());
            writer.writeString(binaryCfg.getDefaultNameMapper());
            writer.writeString(binaryCfg.getDefaultIdMapper());
            writer.writeString(binaryCfg.getDefaultSerializer());
            writer.writeBoolean(binaryCfg.isDefaultKeepDeserialized());
        }
        else
            writer.writeBoolean(false);
    }

    /**
     * Reads cache configuration from a stream.
     *
     * @param in Stream.
     * @return Cache configuration.
     */
    public static CacheConfiguration readCacheConfiguration(BinaryRawReaderEx in) {
        assert in != null;

        CacheConfiguration ccfg = new CacheConfiguration();

        ccfg.setAtomicityMode(CacheAtomicityMode.fromOrdinal(in.readInt()));
        ccfg.setAtomicWriteOrderMode(CacheAtomicWriteOrderMode.fromOrdinal((byte)in.readInt()));
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
        ccfg.setKeepBinaryInStore(in.readBoolean());
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

        Object storeFactory = in.readObjectDetached();

        if (storeFactory != null)
            ccfg.setCacheStoreFactory(new PlatformDotNetCacheStoreFactoryNative(storeFactory));

        int qryEntCnt = in.readInt();

        if (qryEntCnt > 0) {
            ArrayList entities = new ArrayList(qryEntCnt);

            for (int i = 0; i < qryEntCnt; i++)
                entities.add(readQueryEntity(in));

            ccfg.setQueryEntities(entities);
        }

        return ccfg;
    }

    /**
     * Reads the query entity.
     *
     * @param in Stream.
     * @return QueryEntity.
     */
    public static QueryEntity readQueryEntity(BinaryRawReaderEx in) {

    }

    /**
     * Reads Ignite configuration.
     * @param in Reader.
     * @param cfg Configuration.
     */
    public static void readIgniteConfiguration(BinaryRawReaderEx in, IgniteConfiguration cfg) {
        if (in.readBoolean()) cfg.setClientMode(in.readBoolean());
        if (in.readBoolean()) cfg.setMetricsExpireTime(in.readLong());
        if (in.readBoolean()) cfg.setMetricsLogFrequency(in.readLong());
        if (in.readBoolean()) cfg.setMetricsUpdateFrequency(in.readLong());
        if (in.readBoolean()) cfg.setMetricsHistorySize(in.readInt());
        if (in.readBoolean()) cfg.setNetworkSendRetryCount(in.readInt());
        if (in.readBoolean()) cfg.setNetworkSendRetryDelay(in.readLong());
        if (in.readBoolean()) cfg.setNetworkTimeout(in.readLong());

        int[] eventTypes = in.readIntArray();
        if (eventTypes != null) cfg.setIncludeEventTypes(eventTypes);

        String workDir = in.readString();
        if (workDir != null) cfg.setWorkDirectory(workDir);

        readCacheConfigurations(in, cfg);
        readDiscoveryConfiguration(in, cfg);
    }

    /**
     * Reads cache configurations from a stream and updates provided IgniteConfiguration.
     *
     * @param cfg IgniteConfiguration to update.
     * @param in Reader.
     */
    public static void readCacheConfigurations(BinaryRawReaderEx in, IgniteConfiguration cfg) {
        int len = in.readInt();

        if (len == 0)
            return;

        List<CacheConfiguration> caches = new ArrayList<>();

        for (int i = 0; i < len; i++)
            caches.add(readCacheConfiguration(in));

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
     * Reads discovery configuration from a stream and updates provided IgniteConfiguration.
     *
     * @param cfg IgniteConfiguration to update.
     * @param in Reader.
     */
    public static void readDiscoveryConfiguration(BinaryRawReader in, IgniteConfiguration cfg) {
        boolean hasConfig = in.readBoolean();

        if (!hasConfig)
            return;

        TcpDiscoverySpi disco = new TcpDiscoverySpi();

        boolean hasIpFinder = in.readBoolean();

        if (hasIpFinder) {
            byte ipFinderType = in.readByte();

            int addrCount = in.readInt();

            ArrayList<String> addrs = null;

            if (addrCount > 0) {
                addrs = new ArrayList<>(addrCount);

                for (int i = 0; i < addrCount; i++)
                    addrs.add(in.readString());
            }

            TcpDiscoveryVmIpFinder finder = null;
            if (ipFinderType == 1) {
                finder = new TcpDiscoveryVmIpFinder();
            }
            else if (ipFinderType == 2) {
                TcpDiscoveryMulticastIpFinder finder0 = new TcpDiscoveryMulticastIpFinder();

                finder0.setLocalAddress(in.readString());
                finder0.setMulticastGroup(in.readString());
                finder0.setMulticastPort(in.readInt());
                finder0.setAddressRequestAttempts(in.readInt());
                finder0.setResponseWaitTime(in.readInt());

                boolean hasTtl = in.readBoolean();

                if (hasTtl)
                    finder0.setTimeToLive(in.readInt());

                finder = finder0;
            }
            else {
                assert false;
            }

            finder.setAddresses(addrs);

            disco.setIpFinder(finder);
        }

        disco.setSocketTimeout(in.readLong());
        disco.setAckTimeout(in.readLong());
        disco.setMaxAckTimeout(in.readLong());
        disco.setNetworkTimeout(in.readLong());
        disco.setJoinTimeout(in.readLong());

        cfg.setDiscoverySpi(disco);
    }

    /**
     * Writes cache configuration.
     *
     * @param writer Writer.
     * @param ccfg Configuration.
     */
    public static void writeCacheConfiguration(BinaryRawWriter writer, CacheConfiguration ccfg) {
        assert writer != null;
        assert ccfg != null;

        writer.writeInt(ccfg.getAtomicityMode() == null ?
            CacheConfiguration.DFLT_CACHE_ATOMICITY_MODE.ordinal() : ccfg.getAtomicityMode().ordinal());
        writer.writeInt(ccfg.getAtomicWriteOrderMode() == null ? 0 : ccfg.getAtomicWriteOrderMode().ordinal());
        writer.writeInt(ccfg.getBackups());
        writer.writeInt(ccfg.getCacheMode() == null ?
            CacheConfiguration.DFLT_CACHE_MODE.ordinal() : ccfg.getCacheMode().ordinal());
        writer.writeBoolean(ccfg.isCopyOnRead());
        writer.writeBoolean(ccfg.isEagerTtl());
        writer.writeBoolean(ccfg.isSwapEnabled());
        writer.writeBoolean(ccfg.isEvictSynchronized());
        writer.writeInt(ccfg.getEvictSynchronizedConcurrencyLevel());
        writer.writeInt(ccfg.getEvictSynchronizedKeyBufferSize());
        writer.writeLong(ccfg.getEvictSynchronizedTimeout());
        writer.writeBoolean(ccfg.isInvalidate());
        writer.writeBoolean(ccfg.isKeepBinaryInStore());
        writer.writeBoolean(ccfg.isLoadPreviousValue());
        writer.writeLong(ccfg.getDefaultLockTimeout());
        writer.writeLong(ccfg.getLongQueryWarningTimeout());
        writer.writeInt(ccfg.getMaxConcurrentAsyncOperations());
        writer.writeFloat(ccfg.getEvictMaxOverflowRatio());
        writer.writeInt(ccfg.getMemoryMode() == null ?
            CacheConfiguration.DFLT_MEMORY_MODE.ordinal() : ccfg.getMemoryMode().ordinal());
        writer.writeString(ccfg.getName());
        writer.writeLong(ccfg.getOffHeapMaxMemory());
        writer.writeBoolean(ccfg.isReadFromBackup());
        writer.writeInt(ccfg.getRebalanceBatchSize());
        writer.writeLong(ccfg.getRebalanceDelay());
        writer.writeInt(ccfg.getRebalanceMode() == null ?
            CacheConfiguration.DFLT_REBALANCE_MODE.ordinal() : ccfg.getRebalanceMode().ordinal());
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
        writer.writeInt(ccfg.getWriteSynchronizationMode() == null ? 0 : ccfg.getWriteSynchronizationMode().ordinal());

        if (ccfg.getCacheStoreFactory() instanceof PlatformDotNetCacheStoreFactoryNative)
            writer.writeObject(((PlatformDotNetCacheStoreFactoryNative)ccfg.getCacheStoreFactory()).getNativeFactory());
        else
            writer.writeObject(null);
    }

    /**
     * Writes Ignite configuration.
     *
     * @param w Writer.
     * @param cfg Configuration.
     */
    public static void writeIgniteConfiguration(BinaryRawWriter w, IgniteConfiguration cfg) {
        assert w != null;
        assert cfg != null;

        w.writeString(cfg.getGridName());

        CacheConfiguration[] cacheCfg = cfg.getCacheConfiguration();

        if (cacheCfg != null) {
            w.writeInt(cacheCfg.length);

            for (CacheConfiguration ccfg : cacheCfg)
                writeCacheConfiguration(w, ccfg);
        }
        else
            w.writeInt(0);

        w.writeString(cfg.getIgniteHome());

        w.writeLong(ManagementFactory.getMemoryMXBean().getHeapMemoryUsage().getInit());
        w.writeLong(ManagementFactory.getMemoryMXBean().getHeapMemoryUsage().getMax());

        writeDiscoveryConfiguration(w, cfg.getDiscoverySpi());

        w.writeBoolean(cfg.isClientMode());

        w.writeIntArray(cfg.getIncludeEventTypes());
        w.writeLong(cfg.getMetricsExpireTime());
        w.writeInt(cfg.getMetricsHistorySize());
        w.writeLong(cfg.getMetricsLogFrequency());
        w.writeLong(cfg.getMetricsUpdateFrequency());
        w.writeInt(cfg.getNetworkSendRetryCount());
        w.writeLong(cfg.getNetworkSendRetryDelay());
        w.writeLong(cfg.getNetworkTimeout());
        w.writeString(cfg.getWorkDirectory());
    }

    /**
     * Writes discovery configuration.
     *
     * @param w Writer.
     * @param spi Disco.
     */
    private static void writeDiscoveryConfiguration(BinaryRawWriter w, DiscoverySpi spi) {
        assert w != null;
        assert spi != null;

        if (!(spi instanceof TcpDiscoverySpi)) {
            w.writeBoolean(false);
            return;
        }

        w.writeBoolean(true);

        TcpDiscoverySpi tcp = (TcpDiscoverySpi)spi;

        TcpDiscoveryIpFinder finder = tcp.getIpFinder();

        if (finder instanceof TcpDiscoveryVmIpFinder) {
            w.writeBoolean(true);

            boolean isMulticast = finder instanceof TcpDiscoveryMulticastIpFinder;

            w.writeByte((byte)(isMulticast ? 2 : 1));

            TcpDiscoveryVmIpFinder vmFinder = (TcpDiscoveryVmIpFinder) finder;

            Collection<InetSocketAddress> addrs = vmFinder.getRegisteredAddresses();

            w.writeInt(addrs.size());

            for (InetSocketAddress a : addrs)
                w.writeString(a.toString());

            if (isMulticast) {
                TcpDiscoveryMulticastIpFinder multiFinder = (TcpDiscoveryMulticastIpFinder) vmFinder;

                w.writeString(multiFinder.getLocalAddress());
                w.writeString(multiFinder.getMulticastGroup());
                w.writeInt(multiFinder.getMulticastPort());
                w.writeInt(multiFinder.getAddressRequestAttempts());
                w.writeInt(multiFinder.getResponseWaitTime());

                Integer ttl = multiFinder.getTimeToLive();
                w.writeBoolean(ttl != null);

                if (ttl != null)
                    w.writeInt(ttl);
            }
        }
        else {
            w.writeBoolean(false);
        }

        w.writeLong(tcp.getSocketTimeout());
        w.writeLong(tcp.getAckTimeout());
        w.writeLong(tcp.getMaxAckTimeout());
        w.writeLong(tcp.getNetworkTimeout());
        w.writeLong(tcp.getJoinTimeout());
    }

    /**
     * Private constructor.
     */
    private PlatformConfigurationUtils() {
        // No-op.
    }
}
