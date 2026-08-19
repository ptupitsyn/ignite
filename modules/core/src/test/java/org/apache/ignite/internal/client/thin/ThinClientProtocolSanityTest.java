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

package org.apache.ignite.internal.client.thin;

import java.io.IOException;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.util.Arrays;
import java.util.Collection;
import java.util.Collections;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.Future;
import java.util.concurrent.TimeUnit;
import javax.cache.Cache;
import org.apache.ignite.IgniteBinary;
import org.apache.ignite.Ignition;
import org.apache.ignite.binary.BinaryObject;
import org.apache.ignite.binary.BinaryType;
import org.apache.ignite.cache.CacheAtomicityMode;
import org.apache.ignite.cache.CachePeekMode;
import org.apache.ignite.cache.QueryEntity;
import org.apache.ignite.cache.QueryIndex;
import org.apache.ignite.cache.query.ContinuousQuery;
import org.apache.ignite.cache.query.FieldsQueryCursor;
import org.apache.ignite.cache.query.IndexQuery;
import org.apache.ignite.cache.query.QueryCursor;
import org.apache.ignite.cache.query.ScanQuery;
import org.apache.ignite.cache.query.SqlFieldsQuery;
import org.apache.ignite.cache.query.SqlQuery;
import org.apache.ignite.client.ClientAtomicLong;
import org.apache.ignite.client.ClientCache;
import org.apache.ignite.client.ClientCacheConfiguration;
import org.apache.ignite.client.ClientCompute;
import org.apache.ignite.client.ClientCollectionConfiguration;
import org.apache.ignite.client.ClientException;
import org.apache.ignite.client.ClientIgniteSet;
import org.apache.ignite.client.ClientFeatureNotSupportedByServerException;
import org.apache.ignite.client.ClientServiceDescriptor;
import org.apache.ignite.client.ClientTransaction;
import org.apache.ignite.client.IgniteClient;
import org.apache.ignite.client.Person;
import org.apache.ignite.cluster.ClusterNode;
import org.apache.ignite.cluster.ClusterState;
import org.apache.ignite.configuration.ClientConfiguration;
import org.apache.ignite.internal.processors.cache.version.GridCacheVersion;
import org.apache.ignite.internal.util.typedef.T3;
import org.apache.ignite.internal.util.typedef.X;
import org.apache.ignite.services.ServiceCallContext;
import org.apache.ignite.internal.util.typedef.internal.CU;
import org.junit.*;
import org.junit.rules.TestName;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertNotNull;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertTrue;
import static org.junit.Assert.fail;

/**
 * Protocol sanity test for the Java thin client: exercises every {@link ClientOperation} that is reachable against a
 * plain running cluster, one test per operation where the operation maps to a distinct API call.
 * <p>
 * Unlike the other tests in this package, this one does <b>not</b> start a cluster. It connects to a cluster that is
 * already running on {@link #ADDR}. When nothing answers on that address the whole class is skipped, so a module-wide
 * build does not fail on it. For the same reason the class is not a member of {@code ClientTestSuite}.
 * <p>
 * Cluster prerequisites:
 * <ul>
 * <li>Reachable on {@link #ADDR} and active.</li>
 * <li>Indexing module present - the SQL and index query tests need it.</li>
 * <li>Server new enough for the {@code DATA_REPLICATION_OPERATIONS}, {@code INDEX_QUERY} and {@code HEARTBEAT}
 * protocol features. An older server gives a {@code ClientFeatureNotSupportedByServerException}.</li>
 * <li>Thin client compute on ({@code ThinClientConfiguration.setMaxActiveComputeTasksPerConnection}), with the tasks
 * {@link #ECHO_TASK_CLS}, {@link #SLEEP_TASK_CLS} and {@link #FAIL_TASK_CLS} on the server classpath, and deployed
 * under their task names. The GridGain {@code IgniteClientCompatNodeRunnerTest} does all of this.</li>
 * <li>A service with the methods of {@link CompatService}, deployed under the name {@link #SVC_NAME}. The same runner
 * deploys it.</li>
 * </ul>
 * Every cache, atomic long and set that the test makes has a name that starts with {@link #PREFIX}, and is removed
 * again when the class completes.
 * <p>
 * These operations are <b>not</b> covered:
 * <ul>
 * <li>{@link ClientOperation#SERVICE_GET_TOPOLOGY} - {@code ClientServicesImpl.serviceTopology} sends it only if the
 * server gives the {@code SERVICE_TOPOLOGY} feature, and a GridGain server does not have this feature.</li>
 * <li>{@link ClientOperation#CACHE_INVOKE}, {@link ClientOperation#CACHE_INVOKE_ALL} - the entry processor class must
 * be on the server classpath.</li>
 * <li>{@link ClientOperation#CLUSTER_GET_WAL_STATE}, {@link ClientOperation#CLUSTER_CHANGE_WAL_STATE} - WAL.</li>
 * <li>{@link ClientOperation#ATOMIC_LONG_VALUE_COMPARE_AND_SET_AND_GET} - {@link ClientAtomicLongImpl} never sends it,
 * it only uses {@link ClientOperation#ATOMIC_LONG_VALUE_COMPARE_AND_SET}.</li>
 * <li>{@link ClientOperation#CLUSTER_GET_DC_NODES} - sent only when the server nodes carry a data center id.</li>
 * <li>{@link ClientOperation#CACHE_PARTITIONS}, {@link ClientOperation#CLUSTER_GROUP_GET_NODE_ENDPOINTS},
 * {@link ClientOperation#GET_BINARY_CONFIGURATION} - the client sends these on its own account, not in answer to an
 * API call. They run as a side effect of the other tests, but no single test can assert them.</li>
 * </ul>
 */
public class ThinClientProtocolSanityTest {
    /** Address of the cluster under test. */
    private static final String ADDR = "127.0.0.1:10800";

    /** Host part of {@link #ADDR}. */
    private static final String HOST = "127.0.0.1";

    /** Port part of {@link #ADDR}. */
    private static final int PORT = 10800;

    /** Timeout of the connect probe that decides whether the cluster is there at all, in milliseconds. */
    private static final int PROBE_TIMEOUT = 3_000;

    /** Prefix of every cache, atomic long and set that this test makes. */
    private static final String PREFIX = "thinProtoSanity_";

    /** Cache that the plain cache operation tests share. It is cleared before each test. */
    private static final String DFLT_CACHE = PREFIX + "cache";

    /** Table of the cache that the SQL and index query tests share. */
    private static final String QRY_TBL = "THIN_PROTO_SANITY";

    /** Value type of {@link #QRY_TBL}. There is no Java class behind it, so its cache is used with keep binary. */
    private static final String QRY_VAL_TYPE = "ThinProtoSanityValue";

    /** Name of the index on {@link #QRY_TBL}, needed by the index query test. */
    private static final String QRY_IDX = "THIN_PROTO_SANITY_IDX";

    /** Number of rows that the query tests insert. Bigger than any page size they use, so paging is forced. */
    private static final int QRY_ROWS = 10;

    /** Class name of the task that gives its argument back. The compute tests execute it. */
    private static final String ECHO_TASK_CLS = "org.apache.ignite.client.CompatEchoTask";

    /** Task name of {@link #ECHO_TASK_CLS}, from its {@code ComputeTaskName} annotation. */
    private static final String ECHO_TASK_NAME = "CompatEchoTask";

    /** Class name of the task that stays busy. The cancel and the timeout test execute it. */
    private static final String SLEEP_TASK_CLS = "org.apache.ignite.client.CompatSleepTask";

    /** Class name of the task that always fails. */
    private static final String FAIL_TASK_CLS = "org.apache.ignite.client.CompatFailTask";

    /** Message that {@link #FAIL_TASK_CLS} puts into its error. */
    private static final String FAIL_TASK_ERR_MSG = "Compat compute task failure.";

    /** Argument of {@link #SLEEP_TASK_CLS}, in milliseconds. It is longer than any wait of the compute tests. */
    private static final long SLEEP_TASK_DURATION = 30_000L;

    /** Time that a compute test waits for a task result, in seconds. */
    private static final long RESULT_WAIT = 10L;

    /** Timeout that the timeout test puts on the task, in milliseconds. */
    private static final long TASK_TIMEOUT = 500L;

    /** Name of the service that the service tests call. */
    private static final String SVC_NAME = "CompatService";

    /** Message that {@link CompatService#fail()} puts into its error. */
    private static final String SVC_ERR_MSG = "Compat service failure.";

    /** Client that the tests share. Tests that need their own configuration open a short-lived one instead. */
    private static IgniteClient client;

    /** Gives each test a name it can derive unique cache, atomic long and set names from. */
    @Rule public TestName testName = new TestName();

    /**
     * Skips the class when no cluster answers on {@link #ADDR}, otherwise opens the shared client.
     */
    @BeforeClass
    public static void beforeClass() {
        Assume.assumeTrue("No Ignite cluster on " + ADDR + ", skipping protocol sanity test.", clusterAvailable());

        client = Ignition.startClient(clientConfiguration());
    }

    /**
     * Removes everything the test made, then closes the shared client.
     */
    @AfterClass
    public static void afterClass() {
        if (client == null)
            return;

        try {
            for (String name : client.cacheNames()) {
                if (name.startsWith(PREFIX))
                    client.destroyCache(name);
            }
        }
        finally {
            client.close();

            client = null;
        }
    }

    /**
     * Puts the shared cache back into a known empty state.
     */
    @Before
    public void before() {
        client.getOrCreateCache(DFLT_CACHE).removeAll();
    }

    /**
     * Tested operation: {@link ClientOperation#HANDSHAKE}. A client that finishes its constructor has completed the
     * handshake, and the following call proves the channel is usable.
     */
    @Test
    public void testHandshake() {
        try (IgniteClient cli = Ignition.startClient(clientConfiguration())) {
            assertNotNull(cli.cacheNames());
        }
    }

    /**
     * Tested operations: {@link ClientOperation#HEARTBEAT} and {@link ClientOperation#GET_IDLE_TIMEOUT}. The latter is
     * sent while the channel starts up, see {@code TcpClientChannel.getHeartbeatInterval}. The sleep spans several
     * heartbeat intervals, and the call afterwards shows the channel survived them.
     */
    @Test
    public void testHeartbeat() throws Exception {
        ClientConfiguration cfg = clientConfiguration()
            .setHeartbeatEnabled(true)
            .setHeartbeatInterval(500L);

        try (IgniteClient cli = Ignition.startClient(cfg)) {
            Thread.sleep(1_500L);

            assertNotNull(cli.cacheNames());
        }
    }

    /**
     * Tested operation: {@link ClientOperation#RESOURCE_CLOSE}. {@code GenericQueryPager.close} sends it only while the
     * server still holds pages, so the cursor is abandoned after a single entry.
     */
    @Test
    public void testResourceClose() throws Exception {
        ClientCache<Integer, Integer> cache = fillCache(100);

        QueryCursor<Cache.Entry<Integer, Integer>> cur = cache.query(new ScanQuery<Integer, Integer>().setPageSize(1));

        Iterator<Cache.Entry<Integer, Integer>> iter = cur.iterator();

        assertTrue(iter.hasNext());

        iter.next();

        cur.close();
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_CREATE_WITH_NAME}.
     */
    @Test
    public void testCacheCreateWithName() {
        ClientCache<Integer, Integer> cache = client.createCache(cacheName());

        assertEquals(cacheName(), cache.getName());
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET_OR_CREATE_WITH_NAME}.
     */
    @Test
    public void testCacheGetOrCreateWithName() {
        assertEquals(cacheName(), client.getOrCreateCache(cacheName()).getName());

        // Second call takes the existing cache.
        assertEquals(cacheName(), client.getOrCreateCache(cacheName()).getName());
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_CREATE_WITH_CONFIGURATION}.
     */
    @Test
    public void testCacheCreateWithConfiguration() {
        ClientCacheConfiguration ccfg = new ClientCacheConfiguration()
            .setName(cacheName())
            .setBackups(1);

        assertEquals(cacheName(), client.createCache(ccfg).getName());
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET_OR_CREATE_WITH_CONFIGURATION}.
     */
    @Test
    public void testCacheGetOrCreateWithConfiguration() {
        ClientCacheConfiguration ccfg = new ClientCacheConfiguration()
            .setName(cacheName())
            .setBackups(1);

        assertEquals(cacheName(), client.getOrCreateCache(ccfg).getName());

        // Second call takes the existing cache.
        assertEquals(cacheName(), client.getOrCreateCache(ccfg).getName());
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET_NAMES}.
     */
    @Test
    public void testCacheGetNames() {
        client.getOrCreateCache(cacheName());

        assertTrue(client.cacheNames().contains(cacheName()));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET_CONFIGURATION}.
     */
    @Test
    public void testCacheGetConfiguration() {
        ClientCacheConfiguration ccfg = new ClientCacheConfiguration()
            .setName(cacheName())
            .setBackups(1);

        ClientCacheConfiguration readCfg = client.getOrCreateCache(ccfg).getConfiguration();

        assertEquals(cacheName(), readCfg.getName());
        assertEquals(1, readCfg.getBackups());
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_DESTROY}.
     */
    @Test
    public void testCacheDestroy() {
        client.getOrCreateCache(cacheName());

        assertTrue(client.cacheNames().contains(cacheName()));

        client.destroyCache(cacheName());

        assertFalse(client.cacheNames().contains(cacheName()));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_PUT}.
     */
    @Test
    public void testCachePut() {
        ClientCache<Integer, String> cache = dfltCache();

        cache.put(1, "1");

        assertEquals("1", cache.get(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET}.
     */
    @Test
    public void testCacheGet() {
        ClientCache<Integer, String> cache = dfltCache();

        assertNull(cache.get(1));

        cache.put(1, "1");

        assertEquals("1", cache.get(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_CONTAINS_KEY}.
     */
    @Test
    public void testCacheContainsKey() {
        ClientCache<Integer, String> cache = dfltCache();

        assertFalse(cache.containsKey(1));

        cache.put(1, "1");

        assertTrue(cache.containsKey(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_PUT_IF_ABSENT}.
     */
    @Test
    public void testCachePutIfAbsent() {
        ClientCache<Integer, String> cache = dfltCache();

        assertTrue(cache.putIfAbsent(1, "1"));
        assertFalse(cache.putIfAbsent(1, "2"));

        assertEquals("1", cache.get(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET_AND_PUT}.
     */
    @Test
    public void testCacheGetAndPut() {
        ClientCache<Integer, String> cache = dfltCache();

        assertNull(cache.getAndPut(1, "1"));
        assertEquals("1", cache.getAndPut(1, "2"));

        assertEquals("2", cache.get(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET_AND_PUT_IF_ABSENT}.
     */
    @Test
    public void testCacheGetAndPutIfAbsent() {
        ClientCache<Integer, String> cache = dfltCache();

        assertNull(cache.getAndPutIfAbsent(1, "1"));
        assertEquals("1", cache.getAndPutIfAbsent(1, "2"));

        assertEquals("1", cache.get(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET_AND_REPLACE}.
     */
    @Test
    public void testCacheGetAndReplace() {
        ClientCache<Integer, String> cache = dfltCache();

        assertNull(cache.getAndReplace(1, "1"));

        cache.put(1, "1");

        assertEquals("1", cache.getAndReplace(1, "2"));
        assertEquals("2", cache.get(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET_AND_REMOVE}.
     */
    @Test
    public void testCacheGetAndRemove() {
        ClientCache<Integer, String> cache = dfltCache();

        assertNull(cache.getAndRemove(1));

        cache.put(1, "1");

        assertEquals("1", cache.getAndRemove(1));
        assertFalse(cache.containsKey(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_REPLACE}.
     */
    @Test
    public void testCacheReplace() {
        ClientCache<Integer, String> cache = dfltCache();

        assertFalse(cache.replace(1, "1"));

        cache.put(1, "1");

        assertTrue(cache.replace(1, "2"));
        assertEquals("2", cache.get(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_REPLACE_IF_EQUALS}.
     */
    @Test
    public void testCacheReplaceIfEquals() {
        ClientCache<Integer, String> cache = dfltCache();

        cache.put(1, "1");

        assertFalse(cache.replace(1, "wrong", "2"));
        assertEquals("1", cache.get(1));

        assertTrue(cache.replace(1, "1", "2"));
        assertEquals("2", cache.get(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_REMOVE_KEY}.
     */
    @Test
    public void testCacheRemoveKey() {
        ClientCache<Integer, String> cache = dfltCache();

        assertFalse(cache.remove(1));

        cache.put(1, "1");

        assertTrue(cache.remove(1));
        assertFalse(cache.containsKey(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_REMOVE_IF_EQUALS}.
     */
    @Test
    public void testCacheRemoveIfEquals() {
        ClientCache<Integer, String> cache = dfltCache();

        cache.put(1, "1");

        assertFalse(cache.remove(1, "wrong"));
        assertTrue(cache.containsKey(1));

        assertTrue(cache.remove(1, "1"));
        assertFalse(cache.containsKey(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_CLEAR_KEY}.
     */
    @Test
    public void testCacheClearKey() {
        ClientCache<Integer, String> cache = dfltCache();

        cache.put(1, "1");
        cache.put(2, "2");

        cache.clear(1);

        assertFalse(cache.containsKey(1));
        assertTrue(cache.containsKey(2));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_PUT_ALL}.
     */
    @Test
    public void testCachePutAll() {
        ClientCache<Integer, String> cache = dfltCache();

        Map<Integer, String> data = new HashMap<>();

        for (int i = 0; i < 10; i++)
            data.put(i, Integer.toString(i));

        cache.putAll(data);

        assertEquals(data, cache.getAll(data.keySet()));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET_ALL}.
     */
    @Test
    public void testCacheGetAll() {
        ClientCache<Integer, String> cache = dfltCache();

        cache.put(1, "1");
        cache.put(2, "2");

        Map<Integer, String> res = cache.getAll(new HashSet<>(Arrays.asList(1, 2, 3)));

        assertEquals(2, res.size());
        assertEquals("1", res.get(1));
        assertEquals("2", res.get(2));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_CONTAINS_KEYS}.
     */
    @Test
    public void testCacheContainsKeys() {
        ClientCache<Integer, String> cache = dfltCache();

        Set<Integer> keys = new HashSet<>(Arrays.asList(1, 2));

        assertFalse(cache.containsKeys(keys));

        cache.put(1, "1");
        cache.put(2, "2");

        assertTrue(cache.containsKeys(keys));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_CLEAR_KEYS}.
     */
    @Test
    public void testCacheClearKeys() {
        ClientCache<Integer, String> cache = dfltCache();

        cache.put(1, "1");
        cache.put(2, "2");
        cache.put(3, "3");

        cache.clearAll(new HashSet<>(Arrays.asList(1, 2)));

        assertFalse(cache.containsKey(1));
        assertFalse(cache.containsKey(2));
        assertTrue(cache.containsKey(3));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_REMOVE_KEYS}.
     */
    @Test
    public void testCacheRemoveKeys() {
        ClientCache<Integer, String> cache = dfltCache();

        cache.put(1, "1");
        cache.put(2, "2");
        cache.put(3, "3");

        cache.removeAll(new HashSet<>(Arrays.asList(1, 2)));

        assertFalse(cache.containsKey(1));
        assertFalse(cache.containsKey(2));
        assertTrue(cache.containsKey(3));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_GET_SIZE}, through both the int and the long form. The int form
     * is deprecated but still has its own client side path, so it is covered too.
     */
    @SuppressWarnings("deprecation")
    @Test
    public void testCacheGetSize() {
        ClientCache<Integer, String> cache = dfltCache();

        assertEquals(0, cache.size());

        for (int i = 0; i < 10; i++)
            cache.put(i, Integer.toString(i));

        assertEquals(10, cache.size());
        assertEquals(10, cache.size(CachePeekMode.PRIMARY));
        assertEquals(10L, cache.sizeLong());
        assertEquals(10L, cache.sizeLong(CachePeekMode.PRIMARY));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_CLEAR}.
     */
    @Test
    public void testCacheClear() {
        ClientCache<Integer, String> cache = dfltCache();

        cache.put(1, "1");
        cache.put(2, "2");

        cache.clear();

        assertEquals(0L, cache.sizeLong());
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_REMOVE_ALL}.
     */
    @Test
    public void testCacheRemoveAll() {
        ClientCache<Integer, String> cache = dfltCache();

        cache.put(1, "1");
        cache.put(2, "2");

        cache.removeAll();

        assertEquals(0L, cache.sizeLong());
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_PUT_ALL_CONFLICT}. Needs no server side classes: the conflict
     * version type is part of the server itself.
     */
    @Test
    @Ignore("ClientFeatureNotSupportedByServerException: Feature DATA_REPLICATION_OPERATIONS is not supported by the server")
    public void testCachePutAllConflict() {
        TcpClientCache<Integer, Integer> cache = (TcpClientCache<Integer, Integer>)client.<Integer, Integer>getOrCreateCache(cacheName());

        Map<Integer, T3<Integer, GridCacheVersion, Long>> data = new HashMap<>();

        data.put(1, new T3<>(1, new GridCacheVersion(1, 1, 1, 2), CU.EXPIRE_TIME_ETERNAL));

        cache.putAllConflict(data);

        assertEquals((Integer)1, cache.get(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CACHE_REMOVE_ALL_CONFLICT}.
     */
    @Test
    @Ignore("ClientFeatureNotSupportedByServerException: Feature DATA_REPLICATION_OPERATIONS is not supported by the server")
    public void testCacheRemoveAllConflict() {
        TcpClientCache<Integer, Integer> cache = (TcpClientCache<Integer, Integer>)client.<Integer, Integer>getOrCreateCache(cacheName());

        GridCacheVersion ver = new GridCacheVersion(1, 1, 1, 2);

        Map<Integer, T3<Integer, GridCacheVersion, Long>> data = new HashMap<>();

        data.put(1, new T3<>(1, ver, CU.EXPIRE_TIME_ETERNAL));

        cache.putAllConflict(data);

        assertEquals((Integer)1, cache.get(1));

        Map<Integer, GridCacheVersion> rmv = new HashMap<>();

        rmv.put(1, ver);

        cache.removeAllConflict(rmv);

        assertFalse(cache.containsKey(1));
    }

    /**
     * Tested operations: {@link ClientOperation#QUERY_SCAN} and {@link ClientOperation#QUERY_SCAN_CURSOR_GET_PAGE}.
     * The query carries no filter, so nothing has to be deployed on the server. The page size is smaller than the
     * entry count, which forces the paging operation.
     */
    @Test
    public void testQueryScan() {
        ClientCache<Integer, Integer> cache = fillCache(QRY_ROWS);

        try (QueryCursor<Cache.Entry<Integer, Integer>> cur = cache.query(new ScanQuery<Integer, Integer>().setPageSize(2))) {
            assertEquals(QRY_ROWS, cur.getAll().size());
        }
    }

    /**
     * Tested operations: {@link ClientOperation#QUERY_SQL} and {@link ClientOperation#QUERY_SQL_CURSOR_GET_PAGE}.
     */
    @SuppressWarnings("deprecation")
    @Test
    public void testQuerySql() {
        ClientCache<Integer, BinaryObject> cache = queryCache().withKeepBinary();

        SqlQuery<Integer, BinaryObject> qry = new SqlQuery<Integer, BinaryObject>(QRY_VAL_TYPE, "B >= ?")
            .setArgs(0)
            .setPageSize(2);

        try (QueryCursor<Cache.Entry<Integer, BinaryObject>> cur = cache.query(qry)) {
            assertEquals(QRY_ROWS, cur.getAll().size());
        }
    }

    /**
     * Tested operations: {@link ClientOperation#QUERY_SQL_FIELDS} and
     * {@link ClientOperation#QUERY_SQL_FIELDS_CURSOR_GET_PAGE}.
     */
    @Test
    public void testQuerySqlFields() {
        ClientCache<Integer, Object> cache = queryCache();

        SqlFieldsQuery qry = new SqlFieldsQuery("select A, B from " + QRY_TBL + " order by A").setPageSize(2);

        try (FieldsQueryCursor<List<?>> cur = cache.query(qry)) {
            assertEquals(QRY_ROWS, cur.getAll().size());
        }
    }

    /**
     * Tested operations: {@link ClientOperation#QUERY_INDEX} and {@link ClientOperation#QUERY_INDEX_CURSOR_GET_PAGE}.
     * The query names its value type as a string and carries no filter, so no server side class is needed.
     */
    @Test
    public void testQueryIndex() {
        ClientCache<Integer, BinaryObject> cache = queryCache().withKeepBinary();

        IndexQuery<Integer, BinaryObject> qry = new IndexQuery<>(QRY_VAL_TYPE, QRY_IDX);

        qry.setPageSize(2);

        try (QueryCursor<Cache.Entry<Integer, BinaryObject>> cur = cache.query(qry)) {
            assertEquals(QRY_ROWS, cur.getAll().size());
        }
    }

    /**
     * Tested operations: {@link ClientOperation#QUERY_CONTINUOUS} and {@link ClientOperation#QUERY_CONTINUOUS_EVENT}.
     * Only a local listener is set, so {@code ClientCacheEntryListenerHandler.startListen} writes a null remote filter
     * factory and nothing needs deploying. Reaching the latch proves the notification was decoded.
     */
    @Test
    public void testQueryContinuous() throws Exception {
        ClientCache<Integer, Integer> cache = client.getOrCreateCache(cacheName());

        CountDownLatch latch = new CountDownLatch(1);

        ContinuousQuery<Integer, Integer> qry = new ContinuousQuery<>();

        qry.setLocalListener(evts -> evts.forEach(evt -> latch.countDown()));

        try (QueryCursor<Cache.Entry<Integer, Integer>> ignored = cache.query(qry, null)) {
            cache.put(1, 1);

            assertTrue("Continuous query event was not delivered.", latch.await(10, TimeUnit.SECONDS));
        }
    }

    /**
     * Tested operations: {@link ClientOperation#PUT_BINARY_TYPE} and
     * {@link ClientOperation#REGISTER_BINARY_TYPE_NAME}. A fresh client has an empty metadata cache and an empty
     * marshaller context, so writing a {@link Person} makes it send both.
     */
    @Test
    public void testPutBinaryTypeAndRegisterBinaryTypeName() {
        try (IgniteClient cli = Ignition.startClient(clientConfiguration())) {
            ClientCache<Integer, Person> cache = cli.getOrCreateCache(cacheName());

            Person person = new Person(1, "Joe");

            cache.put(1, person);

            assertEquals(person, cache.get(1));
        }
    }

    /**
     * Tested operation: {@link ClientOperation#GET_BINARY_TYPE}. The type is written by one client and asked for by
     * another, whose metadata cache does not hold it yet.
     */
    @Test
    public void testGetBinaryType() {
        client.<Integer, Person>getOrCreateCache(cacheName()).put(1, new Person(1, "Joe"));

        try (IgniteClient cli = Ignition.startClient(clientConfiguration())) {
            IgniteBinary binary = cli.binary();

            BinaryType type = binary.type(Person.class.getName());

            assertNotNull(type);
            assertTrue(type.fieldNames().contains("name"));
        }
    }

    /**
     * Tested operation: {@link ClientOperation#GET_BINARY_TYPE_NAME}. A fresh client reads a value of a type it never
     * wrote, so its marshaller context has to ask the server for the class name of the type id.
     */
    @Test
    public void testGetBinaryTypeName() {
        Person person = new Person(1, "Joe");

        client.<Integer, Person>getOrCreateCache(cacheName()).put(1, person);

        try (IgniteClient cli = Ignition.startClient(clientConfiguration())) {
            assertEquals(person, cli.<Integer, Person>cache(cacheName()).get(1));
        }
    }

    /**
     * Tested operations: {@link ClientOperation#TX_START} and {@link ClientOperation#TX_END}, committing.
     */
    @Test
    public void testTxStartCommit() {
        ClientCache<Integer, String> cache = txCache();

        try (ClientTransaction tx = client.transactions().txStart()) {
            cache.put(1, "1");

            tx.commit();
        }

        assertEquals("1", cache.get(1));
    }

    /**
     * Tested operations: {@link ClientOperation#TX_START} and {@link ClientOperation#TX_END}, rolling back.
     */
    @Test
    public void testTxStartRollback() {
        ClientCache<Integer, String> cache = txCache();

        cache.put(1, "1");

        try (ClientTransaction tx = client.transactions().txStart()) {
            cache.put(1, "2");

            tx.rollback();
        }

        assertEquals("1", cache.get(1));
    }

    /**
     * Tested operation: {@link ClientOperation#CLUSTER_GET_STATE}.
     */
    @Test
    public void testClusterGetState() {
        assertNotNull(client.cluster().state());
    }

    /**
     * Tested operation: {@link ClientOperation#CLUSTER_CHANGE_STATE}. The current state is written back unchanged, so
     * the cluster is left exactly as it was found.
     */
    @Test
    public void testClusterChangeState() {
        ClusterState state = client.cluster().state();

        client.cluster().state(state);

        assertEquals(state, client.cluster().state());
    }

    /**
     * Tested operations: {@link ClientOperation#CLUSTER_GROUP_GET_NODE_IDS} and
     * {@link ClientOperation#CLUSTER_GROUP_GET_NODE_INFO}. {@code ClientClusterGroupImpl.nodes} sends both in turn.
     */
    @Test
    public void testClusterGroupNodes() {
        Collection<ClusterNode> nodes = client.cluster().nodes();

        assertFalse("Cluster reported no nodes.", nodes.isEmpty());

        for (ClusterNode node : nodes)
            assertNotNull(node.id());
    }

    /**
     * Tested operation: {@link ClientOperation#SERVICE_GET_DESCRIPTORS}.
     */
    @Test
    public void testServiceGetDescriptors() {
        Collection<ClientServiceDescriptor> descs = client.services().serviceDescriptors();

        assertNotNull(descs);

        for (ClientServiceDescriptor desc : descs) {
            if (SVC_NAME.equals(desc.name()))
                return;
        }

        fail("Service " + SVC_NAME + " is not in the descriptors.");
    }

    /**
     * Tested operation: {@link ClientOperation#SERVICE_GET_DESCRIPTOR}.
     */
    @Test
    public void testServiceGetDescriptor() {
        ClientServiceDescriptor desc = client.services().serviceDescriptor(SVC_NAME);

        assertEquals(SVC_NAME, desc.name());
        assertNotNull(desc.serviceClass());
        assertEquals(1, desc.totalCount());
        assertNotNull(desc.originNodeId());
    }

    /**
     * Tested operation: {@link ClientOperation#SERVICE_INVOKE}. The proxy sends the name of the method and the types
     * of the parameters, thus the server needs no class of this test.
     */
    @Test
    public void testServiceInvoke() {
        CompatService svc = client.services().serviceProxy(SVC_NAME, CompatService.class);

        assertEquals("ping", svc.echo("ping"));
        assertEquals(5, svc.add(2, 3));
    }

    /**
     * Tested operation: {@link ClientOperation#SERVICE_INVOKE}, for a method that gives {@code null} back.
     */
    @Test
    public void testServiceInvokeNullResult() {
        CompatService svc = client.services().serviceProxy(SVC_NAME, CompatService.class);

        assertNull(svc.echo(null));
    }

    /**
     * Tested operation: {@link ClientOperation#SERVICE_INVOKE}, with a timeout. The timeout travels in the request,
     * and the method is much faster than it.
     */
    @Test
    public void testServiceInvokeWithTimeout() {
        CompatService svc = client.services().serviceProxy(SVC_NAME, CompatService.class, 10_000L);

        assertEquals("ping", svc.echo("ping"));
    }

    /**
     * Tested operation: {@link ClientOperation#SERVICE_INVOKE}, for a cluster group. The request then holds the node
     * IDs of the group in the place of an empty list.
     */
    @Test
    public void testServiceInvokeOnClusterGroup() {
        CompatService svc = client.services(client.cluster().forServers()).serviceProxy(SVC_NAME, CompatService.class);

        assertEquals("ping", svc.echo("ping"));
    }

    /**
     * Tested operation: {@link ClientOperation#SERVICE_INVOKE}, for a method that fails. The error text of the server
     * comes back in the answer.
     */
    @Test
    public void testServiceInvokeFailure() {
        CompatService svc = client.services().serviceProxy(SVC_NAME, CompatService.class);

        try {
            svc.fail();

            fail("Service method was expected to fail.");
        }
        catch (ClientException e) {
            assertTrue("Unexpected error: " + e, X.getFullStackTrace(e).contains(SVC_ERR_MSG));
        }
    }

    /**
     * A caller context needs the {@code SERVICE_INVOKE_CALLCTX} feature. Apache Ignite gives ID 10 to this feature and
     * GridGain gives ID 34, thus a GridGain server does not give the feature to an Apache Ignite client and the client
     * stops the call before it sends a request.
     */
    @Test
    public void testServiceInvokeWithCallerContext() {
        ServiceCallContext callCtx = ServiceCallContext.builder().put("key", "value").build();

        CompatService svc = client.services().serviceProxy(SVC_NAME, CompatService.class, callCtx, 0L);

        try {
            svc.echo("ping");

            fail("Caller context was expected to be rejected.");
        }
        catch (ClientFeatureNotSupportedByServerException e) {
            assertTrue("Unexpected error: " + e, e.getMessage().contains("SERVICE_INVOKE_CALLCTX"));
        }
    }

    /**
     * Methods of the service that the cluster runner deploys. The request holds only the name of the method, thus this
     * interface needs no counterpart of the same name on the server.
     */
    public interface CompatService {
        /**
         * @param val Value.
         * @return The given value.
         */
        public String echo(String val);

        /**
         * @param a First value.
         * @param b Second value.
         * @return Sum of the two values.
         */
        public int add(int a, int b);

        /**
         * Always throws an error.
         */
        public void fail();
    }

    /**
     * Tested operations: {@link ClientOperation#COMPUTE_TASK_EXECUTE} and
     * {@link ClientOperation#COMPUTE_TASK_FINISHED}. The task is named by its class, thus the server finds the class on
     * its classpath and no deployment is necessary.
     */
    @Test
    public void testComputeTaskExecute() throws Exception {
        String res = client.compute().execute(ECHO_TASK_CLS, "ping");

        assertEquals("ping", res);
    }

    /**
     * Tested operation: {@link ClientOperation#COMPUTE_TASK_EXECUTE}, with the task name in the place of the class
     * name. The server resolves the name only because the cluster runner deployed the task.
     */
    @Test
    public void testComputeTaskExecuteByName() throws Exception {
        String res = client.compute().execute(ECHO_TASK_NAME, "ping");

        assertEquals("ping", res);
    }

    /**
     * Tested operations: {@link ClientOperation#COMPUTE_TASK_EXECUTE} and
     * {@link ClientOperation#COMPUTE_TASK_FINISHED}, through the asynchronous API. The result comes in the
     * notification, thus the future gives it only after the server sends the notification.
     */
    @Test
    public void testComputeTaskExecuteAsync() throws Exception {
        Future<String> fut = client.compute().executeAsync(ECHO_TASK_CLS, "ping");

        assertEquals("ping", fut.get(RESULT_WAIT, TimeUnit.SECONDS));
    }

    /**
     * Tested operation: {@link ClientOperation#COMPUTE_TASK_EXECUTE}, for a cluster group. The request then holds the
     * node IDs of the group in the place of an empty list.
     */
    @Test
    public void testComputeTaskExecuteOnClusterGroup() throws Exception {
        ClientCompute compute = client.compute(client.cluster().forServers());

        assertEquals("ping", compute.execute(ECHO_TASK_CLS, "ping"));
    }

    /**
     * Tested operation: {@link ClientOperation#COMPUTE_TASK_EXECUTE}, with the no-failover flag. The flag travels in
     * the flag byte of the request.
     */
    @Test
    public void testComputeTaskExecuteWithNoFailover() throws Exception {
        String res = client.compute().withNoFailover().execute(ECHO_TASK_CLS, "ping");

        assertEquals("ping", res);
    }

    /**
     * Tested operation: {@link ClientOperation#COMPUTE_TASK_EXECUTE}, with the no-result-cache flag. The server then
     * keeps no job results, thus {@code reduce} of the task gets an empty list and the task gives {@code null} back.
     * A result other than {@code null} shows that the flag did not reach the server.
     */
    @Test
    public void testComputeTaskExecuteWithNoResultCache() throws Exception {
        String res = client.compute().withNoResultCache().execute(ECHO_TASK_CLS, "ping");

        assertNull(res);
    }

    /**
     * Tested operation: {@link ClientOperation#RESOURCE_CLOSE}, for a compute task. {@code ClientComputeImpl} sends it
     * only from {@code cancel(true)}. {@code cancel(false)} completes the future on the client side alone. The call
     * after the cancel shows that the channel is still good.
     */
    @Test
    public void testComputeTaskCancel() throws Exception {
        Future<String> fut = client.compute().executeAsync(SLEEP_TASK_CLS, SLEEP_TASK_DURATION);

        assertTrue("Task was not cancelled.", fut.cancel(true));
        assertTrue(fut.isCancelled());

        assertNotNull(client.cacheNames());
    }

    /**
     * Tested operation: {@link ClientOperation#COMPUTE_TASK_EXECUTE}, with the timeout field. The task is longer than
     * the timeout, thus the server stops the task and sends an error.
     */
    @Test
    public void testComputeTaskWithTimeout() throws Exception {
        try {
            client.compute().withTimeout(TASK_TIMEOUT).execute(SLEEP_TASK_CLS, SLEEP_TASK_DURATION);

            fail("Task was expected to time out.");
        }
        catch (ClientException ignored) {
            // Expected: the server stopped the task.
        }
    }

    /**
     * Tested operations: {@link ClientOperation#COMPUTE_TASK_EXECUTE} and
     * {@link ClientOperation#COMPUTE_TASK_FINISHED}, with a task that fails. The error text of the server travels in
     * the notification, thus the failure path of the compute protocol is also tested.
     */
    @Test
    public void testComputeTaskFailure() throws Exception {
        try {
            client.compute().execute(FAIL_TASK_CLS, null);

            fail("Task was expected to fail.");
        }
        catch (ClientException e) {
            assertTrue("Unexpected error: " + e, X.getFullStackTrace(e).contains(FAIL_TASK_ERR_MSG));
        }
    }

    /**
     * Tested operation: {@link ClientOperation#ATOMIC_LONG_CREATE}.
     */
    @Test
    public void testAtomicLongCreate() {
        ClientAtomicLong atomic = client.atomicLong(atomicName(), 42L, true);

        try {
            assertEquals(42L, atomic.get());
        }
        finally {
            atomic.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#ATOMIC_LONG_EXISTS}.
     */
    @Test
    public void testAtomicLongExists() {
        ClientAtomicLong atomic = client.atomicLong(atomicName(), 0L, true);

        try {
            assertFalse(atomic.removed());
        }
        finally {
            atomic.close();
        }

        assertTrue(atomic.removed());
    }

    /**
     * Tested operation: {@link ClientOperation#ATOMIC_LONG_VALUE_GET}.
     */
    @Test
    public void testAtomicLongValueGet() {
        ClientAtomicLong atomic = client.atomicLong(atomicName(), 7L, true);

        try {
            assertEquals(7L, atomic.get());
        }
        finally {
            atomic.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#ATOMIC_LONG_VALUE_ADD_AND_GET}, which also carries increment, decrement
     * and their get-and forms.
     */
    @Test
    public void testAtomicLongValueAddAndGet() {
        ClientAtomicLong atomic = client.atomicLong(atomicName(), 0L, true);

        try {
            assertEquals(5L, atomic.addAndGet(5L));
            assertEquals(5L, atomic.getAndAdd(5L));
            assertEquals(11L, atomic.incrementAndGet());
            assertEquals(11L, atomic.getAndIncrement());
            assertEquals(11L, atomic.decrementAndGet());
            assertEquals(11L, atomic.getAndDecrement());
            assertEquals(10L, atomic.get());
        }
        finally {
            atomic.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#ATOMIC_LONG_VALUE_GET_AND_SET}.
     */
    @Test
    public void testAtomicLongValueGetAndSet() {
        ClientAtomicLong atomic = client.atomicLong(atomicName(), 1L, true);

        try {
            assertEquals(1L, atomic.getAndSet(2L));
            assertEquals(2L, atomic.get());
        }
        finally {
            atomic.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#ATOMIC_LONG_VALUE_COMPARE_AND_SET}.
     */
    @Test
    public void testAtomicLongValueCompareAndSet() {
        ClientAtomicLong atomic = client.atomicLong(atomicName(), 1L, true);

        try {
            assertFalse(atomic.compareAndSet(99L, 2L));
            assertEquals(1L, atomic.get());

            assertTrue(atomic.compareAndSet(1L, 2L));
            assertEquals(2L, atomic.get());
        }
        finally {
            atomic.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#ATOMIC_LONG_REMOVE}.
     */
    @Test
    public void testAtomicLongRemove() {
        ClientAtomicLong atomic = client.atomicLong(atomicName(), 1L, true);

        atomic.close();

        assertTrue(atomic.removed());
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_GET_OR_CREATE}.
     */
    @Test
    public void testSetGetOrCreate() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            assertNotNull(set);
            assertEquals(setName(), set.name());

            // Without a configuration the existing set is taken.
            assertNotNull(client.set(setName(), null));
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_EXISTS}.
     */
    @Test
    public void testSetExists() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            assertFalse(set.removed());
        }
        finally {
            set.close();
        }

        assertTrue(set.removed());
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_VALUE_ADD}.
     */
    @Test
    public void testSetValueAdd() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            assertTrue(set.add("a"));
            assertFalse(set.add("a"));
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_VALUE_ADD_ALL}.
     */
    @Test
    public void testSetValueAddAll() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            assertTrue(set.addAll(Arrays.asList("a", "b")));
            assertEquals(2, set.size());
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_VALUE_CONTAINS}.
     */
    @Test
    public void testSetValueContains() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            set.add("a");

            assertTrue(set.contains("a"));
            assertFalse(set.contains("b"));
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_VALUE_CONTAINS_ALL}.
     */
    @Test
    public void testSetValueContainsAll() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            set.addAll(Arrays.asList("a", "b"));

            assertTrue(set.containsAll(Arrays.asList("a", "b")));
            assertFalse(set.containsAll(Arrays.asList("a", "c")));
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_VALUE_REMOVE}.
     */
    @Test
    public void testSetValueRemove() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            set.add("a");

            assertTrue(set.remove("a"));
            assertFalse(set.remove("a"));
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_VALUE_REMOVE_ALL}.
     */
    @Test
    public void testSetValueRemoveAll() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            set.addAll(Arrays.asList("a", "b", "c"));

            assertTrue(set.removeAll(Arrays.asList("a", "b")));
            assertEquals(1, set.size());
            assertTrue(set.contains("c"));
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_VALUE_RETAIN_ALL}.
     */
    @Test
    public void testSetValueRetainAll() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            set.addAll(Arrays.asList("a", "b", "c"));

            assertTrue(set.retainAll(Arrays.asList("a", "b")));
            assertEquals(2, set.size());
            assertFalse(set.contains("c"));
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_SIZE}.
     */
    @Test
    public void testSetSize() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            assertEquals(0, set.size());

            set.addAll(Arrays.asList("a", "b"));

            assertEquals(2, set.size());
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_CLEAR}.
     */
    @Test
    public void testSetClear() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            set.addAll(Arrays.asList("a", "b"));

            set.clear();

            assertEquals(0, set.size());
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operations: {@link ClientOperation#OP_SET_ITERATOR_START} and
     * {@link ClientOperation#OP_SET_ITERATOR_GET_PAGE}. The page size is smaller than the element count, which forces
     * the paging operation.
     */
    @Test
    public void testSetIterator() {
        ClientIgniteSet<Integer> set = client.set(setName(), new ClientCollectionConfiguration());

        try {
            for (int i = 0; i < 10; i++)
                set.add(i);

            set.pageSize(2);

            int cnt = 0;

            for (Integer ignored : set)
                cnt++;

            assertEquals(10, cnt);
        }
        finally {
            set.close();
        }
    }

    /**
     * Tested operation: {@link ClientOperation#OP_SET_CLOSE}.
     */
    @Test
    public void testSetClose() {
        ClientIgniteSet<String> set = client.set(setName(), new ClientCollectionConfiguration());

        set.add("a");

        set.close();

        assertTrue(set.removed());
    }

    /**
     * Tested operation: {@link ClientOperation#OP_STOP_WARMUP}. Warm-up only runs while a node starts, so
     * {@code GridCacheProcessor.stopWarmUp} always rejects the request on a running cluster. The error is the point:
     * it proves the request was encoded and the error response decoded.
     */
    @Test
    @Ignore("Unexpected message: Ignite failed to process request [12]: Invalid request op code: 10000 (server status code [2])")
    public void testStopWarmUp() {
        try {
            ((TcpIgniteClient)client).stopWarmUp();

            fail("Warm-up stop was expected to be rejected by a running node.");
        }
        catch (ClientException e) {
            assertTrue("Unexpected message: " + e.getMessage(), e.getMessage().contains("Node has already started"));
        }
    }

    /**
     * @return Configuration pointed at the cluster under test.
     */
    private static ClientConfiguration clientConfiguration() {
        return new ClientConfiguration().setAddresses(ADDR);
    }

    /**
     * @return {@code True} when something answers on {@link #ADDR}.
     */
    private static boolean clusterAvailable() {
        try (Socket sock = new Socket()) {
            sock.connect(new InetSocketAddress(HOST, PORT), PROBE_TIMEOUT);

            return true;
        }
        catch (IOException ignored) {
            return false;
        }
    }

    /**
     * @return Cache name unique to the running test.
     */
    private String cacheName() {
        return PREFIX + testName.getMethodName();
    }

    /**
     * @return Atomic long name unique to the running test.
     */
    private String atomicName() {
        return PREFIX + testName.getMethodName();
    }

    /**
     * @return Set name unique to the running test.
     */
    private String setName() {
        return PREFIX + testName.getMethodName();
    }

    /**
     * @return Shared cache the plain cache operation tests use. It is empty at the start of every test.
     */
    private <K, V> ClientCache<K, V> dfltCache() {
        return client.cache(DFLT_CACHE);
    }

    /**
     * Makes a cache of the running test and fills it with the given number of entries.
     *
     * @param cnt Number of entries.
     * @return Filled cache.
     */
    private ClientCache<Integer, Integer> fillCache(int cnt) {
        ClientCache<Integer, Integer> cache = client.getOrCreateCache(cacheName());

        cache.removeAll();

        Map<Integer, Integer> data = new HashMap<>();

        for (int i = 0; i < cnt; i++)
            data.put(i, i);

        cache.putAll(data);

        return cache;
    }

    /**
     * @return Transactional cache of the running test.
     */
    private <K, V> ClientCache<K, V> txCache() {
        return client.getOrCreateCache(new ClientCacheConfiguration()
            .setName(cacheName())
            .setAtomicityMode(CacheAtomicityMode.TRANSACTIONAL));
    }

    /**
     * Makes the cache the SQL and index query tests use and fills it through SQL. The value type is named by string
     * and has an index, so index queries reach it without any class on the server.
     *
     * @return Filled cache.
     */
    private <K, V> ClientCache<K, V> queryCache() {
        LinkedHashMap<String, String> flds = new LinkedHashMap<>();

        flds.put("A", Integer.class.getName());
        flds.put("B", Integer.class.getName());

        LinkedHashMap<String, Boolean> idxFlds = new LinkedHashMap<>();

        idxFlds.put("B", true);

        QueryEntity qryEntity = new QueryEntity()
            .setTableName(QRY_TBL)
            .setKeyType(Integer.class.getName())
            .setValueType(QRY_VAL_TYPE)
            .setFields(flds)
            .setKeyFieldName("A")
            .setIndexes(Collections.singleton(new QueryIndex().setName(QRY_IDX).setFields(idxFlds)));

        ClientCache<K, V> cache = client.getOrCreateCache(new ClientCacheConfiguration()
            .setName(cacheName())
            .setQueryEntities(qryEntity));

        cache.removeAll();

        for (int i = 0; i < QRY_ROWS; i++) {
            cache.query(new SqlFieldsQuery("insert into " + QRY_TBL + "(A, B) values (?, ?)").setArgs(i, i))
                .getAll();
        }

        return cache;
    }
}
