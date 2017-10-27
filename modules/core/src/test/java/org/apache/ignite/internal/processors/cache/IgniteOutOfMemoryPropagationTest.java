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

package org.apache.ignite.internal.processors.cache;

import java.util.HashMap;
import java.util.Map;
import org.apache.ignite.IgniteCache;
import org.apache.ignite.IgniteDataStreamer;
import org.apache.ignite.Ignition;
import org.apache.ignite.cache.CacheAtomicityMode;
import org.apache.ignite.cache.CacheMode;
import org.apache.ignite.cache.CacheWriteSynchronizationMode;
import org.apache.ignite.cluster.ClusterTopologyException;
import org.apache.ignite.configuration.CacheConfiguration;
import org.apache.ignite.configuration.DataRegionConfiguration;
import org.apache.ignite.configuration.DataStorageConfiguration;
import org.apache.ignite.configuration.IgniteConfiguration;
import org.apache.ignite.internal.IgniteEx;
import org.apache.ignite.internal.mem.IgniteOutOfMemoryException;
import org.apache.ignite.internal.util.typedef.G;
import org.apache.ignite.internal.util.typedef.X;
import org.apache.ignite.testframework.junits.common.GridCommonAbstractTest;
import org.apache.ignite.transactions.Transaction;
import org.apache.ignite.transactions.TransactionConcurrency;
import org.apache.ignite.transactions.TransactionIsolation;

/**
 *
 */
public class IgniteOutOfMemoryPropagationTest extends GridCommonAbstractTest {

    /** */
    public static final int NODES = 3;

    /** */
    private CacheAtomicityMode atomicityMode;

    /** */
    private CacheMode mode;

    /** */
    private int backupsCount;

    /** */
    private CacheWriteSynchronizationMode writeSyncMode;

    /** */
    private IgniteEx client;

    /** {@inheritDoc} */
    @Override protected void afterTestsStopped() throws Exception {
        stopAllGrids();

        assert G.allGrids().isEmpty();
    }

    /** {@inheritDoc} */
    @Override protected long getTestTimeout() {
        return 20 * 60 * 1000;
    }

    /** */
    public void testPutOOMPropagation() throws Exception {
        testOOMPropagation(false);
    }

    /** */
    public void testStreamerOOMPropagation() throws Exception {
        testOOMPropagation(true);
    }

    /** */
    private void testOOMPropagation(boolean useStreamer) throws Exception {
        for (CacheAtomicityMode atomicityMode : CacheAtomicityMode.values()) {
            for (CacheMode cacheMode : CacheMode.values()) {
                for (CacheWriteSynchronizationMode writeSyncMode : CacheWriteSynchronizationMode.values()) {
                    for (int backupsCount = 0; backupsCount < 1; backupsCount++) {
                        if (writeSyncMode == CacheWriteSynchronizationMode.FULL_ASYNC
                            || cacheMode == CacheMode.REPLICATED)
                            continue;

                        if (atomicityMode == CacheAtomicityMode.TRANSACTIONAL && !useStreamer) {
                            for (TransactionConcurrency concurrency : TransactionConcurrency.values()) {
                                for (TransactionIsolation isolation : TransactionIsolation.values()) {
                                    checkOOMPropagation(
                                        false,
                                        CacheAtomicityMode.TRANSACTIONAL,
                                        cacheMode,
                                        writeSyncMode,
                                        backupsCount,
                                        concurrency,
                                        isolation);
                                }
                            }
                        }
                        else
                            checkOOMPropagation(useStreamer, atomicityMode, cacheMode, writeSyncMode, backupsCount);
                    }
                }
            }
        }
    }

    /** */
    private void checkOOMPropagation(boolean useStreamer, CacheAtomicityMode atomicityMode, CacheMode cacheMode,
        CacheWriteSynchronizationMode writeSyncMode, int backupsCount) throws Exception {
        checkOOMPropagation(useStreamer, atomicityMode, cacheMode, writeSyncMode, backupsCount, null, null);
    }

    /** */
    private void checkOOMPropagation(boolean useStreamer, CacheAtomicityMode atomicityMode, CacheMode cacheMode,
        CacheWriteSynchronizationMode writeSyncMode, int backupsCount,
        TransactionConcurrency concurrency, TransactionIsolation isolation) throws Exception {
        Throwable t = null;

        System.out.println("Checking conf: CacheAtomicityMode." + atomicityMode +
            " CacheMode." + mode + " CacheWriteSynchronizationMode." + writeSyncMode + " backupsCount = " + backupsCount
            + " TransactionConcurrency." + concurrency + " TransactionIsolation." + isolation);

        initGrid(atomicityMode, cacheMode, writeSyncMode, backupsCount);
        try {
            forceOOM(useStreamer, concurrency, isolation);
        }
        catch (Throwable t0) {
            t = t0;

            t.printStackTrace(System.out);

            assertTrue(X.hasCause(t, IgniteOutOfMemoryException.class, ClusterTopologyException.class));
        }
        finally {
            assertNotNull(t);

            stopAllGrids();
        }
    }

    /**
     * Ignite grid of 3 server nodes with passed parameters.
     *
     * @param atomicityMode atomicity mode
     * @param mode cache mode
     * @param writeSyncMode cache write synchronization mode
     * @param backupsCount backups count
     * @throws Exception
     */
    private void initGrid(CacheAtomicityMode atomicityMode, CacheMode mode,
        CacheWriteSynchronizationMode writeSyncMode, int backupsCount) throws Exception {

        this.atomicityMode = atomicityMode;
        this.mode = mode;
        this.backupsCount = backupsCount;
        this.writeSyncMode = writeSyncMode;

        Ignition.setClientMode(false);

        for (int i = 0; i < NODES; i++)
            startGrid(i);

        Ignition.setClientMode(true);

        client = startGrid(NODES + 1);

        // it is required to start first node in test jvm, but we can not start client node,
        // because client will fail to connect and test will fail too.
        // as workaround start first server node in test jvm and then stop it.
        stopGrid(0);
    }


    /** */
    public void forceOOM(boolean useStreamer, TransactionConcurrency concurrency,
        TransactionIsolation isolation) throws Exception {
        final IgniteCache<Object, Object> cache = client.cache(DEFAULT_CACHE_NAME);

        IgniteDataStreamer<String, String> streamer = client.dataStreamer(DEFAULT_CACHE_NAME);

        Map<String, String> map = new HashMap<>();

        Transaction tx = null;

        for (int i = 0; i < Integer.MAX_VALUE; i++) {
            map.put("k" + i, "v" + i);

            if (map.size() > 1_000) {
                if (concurrency != null && isolation != null)
                    tx = client.transactions().txStart(concurrency, isolation);

                if (useStreamer)
                    streamer.addData(map);
                else
                    cache.putAll(map);

                map.clear();

                if (tx != null) {
                    tx.commit();
                    tx.close();
                }
            }
        }
    }

    /** {@inheritDoc} */
    @Override protected boolean isMultiJvm() {
        return true;
    }

    /** {@inheritDoc} */
    @Override protected boolean isRemoteJvm(String igniteInstanceName) {
        return !(Ignition.isClientMode() || igniteInstanceName.endsWith("0"));
    }

    /** {@inheritDoc} */
    @Override protected IgniteConfiguration getConfiguration(String igniteInstanceName) throws Exception {
        IgniteConfiguration cfg = super.getConfiguration(igniteInstanceName);

        DataStorageConfiguration memCfg = new DataStorageConfiguration();

        memCfg.setDefaultDataRegionConfiguration(new DataRegionConfiguration()
            .setMaxSize(10 * 1024 * 1024 + 1));

        cfg.setDataStorageConfiguration(memCfg);

        CacheConfiguration<Object, Object> baseCfg = new CacheConfiguration<>(DEFAULT_CACHE_NAME);

        baseCfg.setAtomicityMode(this.atomicityMode);
        baseCfg.setCacheMode(this.mode);
        baseCfg.setBackups(this.backupsCount);
        baseCfg.setWriteSynchronizationMode(this.writeSyncMode);

        cfg.setCacheConfiguration(baseCfg);

        return cfg;
    }
}
