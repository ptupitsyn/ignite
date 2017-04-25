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

package org.apache.ignite.spi.discovery.tcp;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.net.SocketTimeoutException;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import org.apache.ignite.Ignite;
import org.apache.ignite.IgniteCheckedException;
import org.apache.ignite.events.DiscoveryEvent;
import org.apache.ignite.events.Event;
import org.apache.ignite.internal.util.typedef.G;
import org.apache.ignite.internal.util.typedef.X;
import org.apache.ignite.internal.util.typedef.internal.U;
import org.apache.ignite.lang.IgnitePredicate;
import org.apache.ignite.spi.discovery.tcp.internal.TcpDiscoveryNode;
import org.apache.ignite.spi.discovery.tcp.ipfinder.vm.TcpDiscoveryVmIpFinder;
import org.apache.ignite.spi.discovery.tcp.messages.TcpDiscoveryAbstractMessage;
import org.apache.ignite.spi.discovery.tcp.messages.TcpDiscoveryPingRequest;
import org.apache.ignite.spi.discovery.tcp.messages.TcpDiscoveryPingResponse;
import org.jetbrains.annotations.Nullable;

import static org.apache.ignite.events.EventType.EVT_NODE_FAILED;

/**
 * Client-based discovery SPI test with failure detection timeout enabled.
 */
public class TcpClientDiscoverySpiFailureTimeoutSelfTest extends TcpClientDiscoverySpiSelfTest {
    /** */
    private final static int FAILURE_AWAIT_TIME = 7_000;

    /** */
    private final static long FAILURE_THRESHOLD = 10_000;

    /** */
    private final static long CLIENT_FAILURE_THRESHOLD = 30_000;

    /** Failure detection timeout for nodes configuration. */
    private static long failureThreshold = FAILURE_THRESHOLD;

    /** Client failure detection timeout for nodes configuration. */
    private static long clientFailureThreshold = CLIENT_FAILURE_THRESHOLD;

    /** */
    private static boolean useTestSpi;

    /** {@inheritDoc} */
    @Override protected boolean useFailureDetectionTimeout() {
        return true;
    }

    /** {@inheritDoc} */
    @Override protected long clientFailureDetectionTimeout() {
        return clientFailureThreshold;
    }

    /** {@inheritDoc} */
    @Override protected long failureDetectionTimeout() {
        return failureThreshold;
    }

    /** {@inheritDoc} */
    @Override protected long awaitTime() {
        return failureDetectionTimeout() + FAILURE_AWAIT_TIME;
    }

    /** {@inheritDoc} */
    @Override protected TcpDiscoverySpi getDiscoverySpi() {
        return useTestSpi ? new TestTcpDiscoverySpi2() : super.getDiscoverySpi();
    }

    /**
     * @throws Exception in case of error.
     */
    public void testFailureDetectionTimeoutEnabled() throws Exception {
        startServerNodes(1);
        startClientNodes(1);

        checkNodes(1, 1);

        assertTrue(((TcpDiscoverySpi)(G.ignite("server-0").configuration().getDiscoverySpi())).
            failureDetectionTimeoutEnabled());
        assertEquals(failureDetectionTimeout(),
            ((TcpDiscoverySpi)(G.ignite("server-0").configuration().getDiscoverySpi())).failureDetectionTimeout());

        assertTrue(((TcpDiscoverySpi)(G.ignite("client-0").configuration().getDiscoverySpi())).
            failureDetectionTimeoutEnabled());
        assertEquals(failureDetectionTimeout(),
            ((TcpDiscoverySpi)(G.ignite("client-0").configuration().getDiscoverySpi())).failureDetectionTimeout());
    }

    /**
     * @throws Exception in case of error.
     */
    public void testFailureTimeoutWorkabilityAvgTimeout() throws Exception {
        failureThreshold = 3000;

        try {
            checkFailureThresholdWorkability();
        }
        finally {
            failureThreshold = FAILURE_THRESHOLD;
        }
    }

    /**
     * @throws Exception in case of error.
     */
    public void testFailureTimeoutWorkabilitySmallTimeout() throws Exception {
        failureThreshold = 500;

        try {
            checkFailureThresholdWorkability();
        }
        finally {
            failureThreshold = FAILURE_THRESHOLD;
        }
    }

    /**
     * Test failure detection time between two server with failure detection.
     *
     * @throws Exception in case of error.
     */
    public void testFailureTimeoutServerServer() throws Exception {
        failureThreshold = 3000;
        clientFailureThreshold = 10000;
        useTestSpi = true;

        try {

            startServerNodes(2);

            checkNodes(2, 0);

            Ignite srv0 = G.ignite("server-0");
            final TestTcpDiscoverySpi2 spi0 = (TestTcpDiscoverySpi2)srv0.configuration().getDiscoverySpi();

            Ignite srv1 = G.ignite("server-1");
            final TestTcpDiscoverySpi2 spi1 = (TestTcpDiscoverySpi2)srv1.configuration().getDiscoverySpi();

            long failureTime = U.currentTimeMillis();

            final long[] failureDetectTime = new long[2];
            final CountDownLatch latch = new CountDownLatch(1);

            spi0.writeToSocketDelay = 7000;
            spi1.writeToSocketDelay = 7000;

            Thread pinger = new Thread(new Runnable() {
                @Override
                public void run() {
                    spi0.pingNode(spi1.getLocalNodeId());
                }
            });

            pinger.start();

            System.out.println("0: " + spi0.getLocalNodeId() + " " + spi0.getCoordinator());
            System.out.println("1: " + spi1.getLocalNodeId() + " " + spi1.getCoordinator());

            srv0.events().localListen(new IgnitePredicate<Event>() {
                @Override public boolean apply(Event evt) {
                    failureDetectTime[0] = U.currentTimeMillis();
                    latch.countDown();
                    return true;
                }
            }, EVT_NODE_FAILED);

            srv1.events().localListen(new IgnitePredicate<Event>() {
                @Override public boolean apply(Event evt) {
                    failureDetectTime[1] = U.currentTimeMillis();
                    latch.countDown();
                    return true;
                }
            }, EVT_NODE_FAILED);

            pinger.join();

            assertTrue("Can't get node failure event", latch.await(15000, TimeUnit.MILLISECONDS));

            System.out.println("0: " + spi0.getLocalNodeId() + " " + spi0.getCoordinator());
            System.out.println("1: " + spi1.getLocalNodeId() + " " + spi1.getCoordinator());

            U.sleep(7000);

            System.out.println("+0: " + spi0.getLocalNodeId() + " " + spi0.getCoordinator());
            System.out.println("+1: " + spi1.getLocalNodeId() + " " + spi1.getCoordinator());
            long detectTime0 = failureDetectTime[0] - failureTime;
            long detectTime1 = failureDetectTime[1] - failureTime;

            System.out.println("detectTime0=" + detectTime0 + ", detectTime1=" + detectTime1);

            assertTrue("Server node failure detected too fast: " + detectTime1 + "ms",
                detectTime1 > failureThreshold - 100);
            assertTrue("Server node failure detected too slow:  " + detectTime1 + "ms",
                detectTime1 < clientFailureThreshold);
        }
        finally {
            failureThreshold = FAILURE_THRESHOLD;
            clientFailureThreshold = CLIENT_FAILURE_THRESHOLD;
            useTestSpi = false;
        }
    }

    /**
     * Test failure detection time between server and client if client fail with failure detection.
     *
     * @throws Exception in case of error.
     */
    public void testFailureTimeoutServerClient() throws Exception {
        failureThreshold = 3000;
        clientFailureThreshold = 2000;

        try {
            startServerNodes(1);

            startClientNodes(1);

            checkNodes(1, 1);

            Ignite serverNode = G.ignite("server-0");
            final TcpDiscoverySpi serverSpi = (TcpDiscoverySpi)serverNode.configuration().getDiscoverySpi();

            Ignite clientNode = G.ignite("client-0");
            final TcpDiscoverySpi clientSpi = (TcpDiscoverySpi)clientNode.configuration().getDiscoverySpi();

            long failureTime = U.currentTimeMillis();

            final long[] failureDetectTime = new long[1];
            final CountDownLatch latch = new CountDownLatch(1);

            clientSpi.simulateNodeFailure();


            Thread pinger = new Thread(new Runnable() {
                @Override
                public void run() {
                    serverSpi.pingNode(clientSpi.getLocalNodeId());
                }
            });

            pinger.start();

            serverNode.events().localListen(new IgnitePredicate<Event>() {
                @Override public boolean apply(Event evt) {
                    DiscoveryEvent disoEvt = (DiscoveryEvent)evt;
                    failureDetectTime[0] = U.currentTimeMillis();
                    latch.countDown();
                    return true;
                }
            }, EVT_NODE_FAILED);

            pinger.join();

            assertTrue("Can't get node failure event", latch.await(15000, TimeUnit.MILLISECONDS));

            long detectTime = failureDetectTime[0]-failureTime;

            assertTrue("Client node failure detected too fast: " + detectTime + "ms",
                detectTime > clientFailureThreshold - 100);
            assertTrue("Client node failure detected too slow:  " + detectTime + "ms",
                detectTime < clientFailureThreshold + 5000);
        }
        finally {
            failureThreshold = FAILURE_THRESHOLD;
            clientFailureThreshold = CLIENT_FAILURE_THRESHOLD;
        }
    }

    /**
     * Test failure detection time between two server with failure detection.
     *
     * @throws Exception in case of error.
     */
    public void testFailureTimeout3Server() throws Exception {
        failureThreshold = 1000;
        clientFailureThreshold = 10000;
        useTestSpi = true;

        try {

            startServerNodes(3);

            checkNodes(3, 0);

            Ignite srv0 = G.ignite("server-0");
            final TestTcpDiscoverySpi2 spi0 = (TestTcpDiscoverySpi2)srv0.configuration().getDiscoverySpi();

            Ignite srv1 = G.ignite("server-1");
            final TestTcpDiscoverySpi2 spi1 = (TestTcpDiscoverySpi2)srv1.configuration().getDiscoverySpi();

            Ignite srv2 = G.ignite("server-2");
            final TestTcpDiscoverySpi2 spi2 = (TestTcpDiscoverySpi2)srv2.configuration().getDiscoverySpi();


            long failureTime = U.currentTimeMillis();

            final long[] failureDetectTime = new long[1];
            final CountDownLatch latch = new CountDownLatch(1);

            spi1.writeToSocketDelay = 2000;
            //spi0.pingResponseReadFail = true;

            Thread pinger = new Thread(new Runnable() {
                @Override
                public void run() {
                    spi1.pingNode(spi2.getLocalNodeId());
                }
            });

            pinger.start();

            System.out.println("0: " + spi0.getLocalNodeId() + " " + spi0.getCoordinator());
            System.out.println("1: " + spi1.getLocalNodeId() + " " + spi1.getCoordinator());
            System.out.println("2: " + spi2.getLocalNodeId() + " " + spi2.getCoordinator());

            srv1.events().localListen(new IgnitePredicate<Event>() {
                @Override public boolean apply(Event evt) {
                    failureDetectTime[0] = U.currentTimeMillis();
                    latch.countDown();
                    return true;
                }
            }, EVT_NODE_FAILED);

            pinger.join();

            assertTrue("Can't get node failure event", latch.await(15000, TimeUnit.MILLISECONDS));

            System.out.println("0: " + spi0.getLocalNodeId() + " " + spi0.getCoordinator());
            System.out.println("1: " + spi1.getLocalNodeId() + " " + spi1.getCoordinator());
            System.out.println("2: " + spi2.getLocalNodeId() + " " + spi2.getCoordinator());

            U.sleep(7000);

            System.out.println("+0: " + spi0.getLocalNodeId() + " " + spi0.getCoordinator());
            System.out.println("+1: " + spi1.getLocalNodeId() + " " + spi1.getCoordinator());
            System.out.println("2: " + spi2.getLocalNodeId() + " " + spi2.getCoordinator());

            long detectTime = failureDetectTime[0] - failureTime;

            System.out.println("detectTime=" + detectTime);

            assertTrue("Server node failure detected too fast: " + detectTime + "ms",
                detectTime > failureThreshold - 100);
            assertTrue("Server node failure detected too slow:  " + detectTime + "ms",
                detectTime < clientFailureThreshold);
        }
        finally {
            failureThreshold = FAILURE_THRESHOLD;
            clientFailureThreshold = CLIENT_FAILURE_THRESHOLD;
            useTestSpi = false;
        }
    }




    /**
     * @throws Exception in case of error.
     */
    private void checkFailureThresholdWorkability() throws Exception {
        useTestSpi = true;

        TestTcpDiscoverySpi2 firstSpi = null;
        TestTcpDiscoverySpi2 secondSpi = null;

        try {
            startServerNodes(2);

            checkNodes(2, 0);

            firstSpi = (TestTcpDiscoverySpi2)(G.ignite("server-0").configuration().getDiscoverySpi());
            secondSpi = (TestTcpDiscoverySpi2)(G.ignite("server-1").configuration().getDiscoverySpi());

            assert firstSpi.err == null;

            secondSpi.readDelay = failureDetectionTimeout() + 5000;

            assertFalse(firstSpi.pingNode(secondSpi.getLocalNodeId()));

            Thread.sleep(failureDetectionTimeout());

            assertTrue(firstSpi.err != null && X.hasCause(firstSpi.err, SocketTimeoutException.class));

            firstSpi.reset();
            secondSpi.reset();

            assertTrue(firstSpi.pingNode(secondSpi.getLocalNodeId()));

            assertTrue(firstSpi.err == null);
        }
        finally {
            useTestSpi = false;

            if (firstSpi != null)
                firstSpi.reset();

            if (secondSpi != null)
                secondSpi.reset();
        }
    }

    /**
     * @throws Exception If failed.
     */
    public void testClientReconnectOnCoordinatorRouterFail1() throws Exception {
        clientReconnectOnCoordinatorRouterFail(1);
    }

    /**
     * @throws Exception If failed.
     */
    public void testClientReconnectOnCoordinatorRouterFail2() throws Exception {
        clientReconnectOnCoordinatorRouterFail(2);
    }

    /**
     * Test tries to provoke scenario when client sends reconnect message before router failure detected.
     *
     * @param srvNodes Number of additional server nodes.
     * @throws Exception If failed.
     */
    public void clientReconnectOnCoordinatorRouterFail(int srvNodes) throws Exception {
        startServerNodes(1);

        Ignite srv = G.ignite("server-0");

        final TcpDiscoveryNode srvNode = (TcpDiscoveryNode)srv.cluster().localNode();

        final UUID srvNodeId = srvNode.id();

        clientIpFinder = new TcpDiscoveryVmIpFinder();

        clientIpFinder.setAddresses(
            Collections.singleton("localhost:" + srvNode.discoveryPort() + ".." + (srvNode.discoveryPort() + 1)));

        failureThreshold = 1000L;
        netTimeout = 1000L;

        startClientNodes(1); // Client should connect to coordinator.

        failureThreshold = 10_000L;
        netTimeout = 5000L;

        List<String> nodes = new ArrayList<>();

        for (int i = 0; i < srvNodes; i++) {
            Ignite g = startGrid("server-" + srvIdx.getAndIncrement());

            nodes.add(g.name());

            srvNodeIds.add(g.cluster().localNode().id());
        }

        checkNodes(1 + srvNodes, 1);

        nodes.add("client-0");

        final CountDownLatch latch = new CountDownLatch(nodes.size());

        final AtomicBoolean err = new AtomicBoolean();

        for (String node : nodes) {
            G.ignite(node).events().localListen(new IgnitePredicate<Event>() {
                @Override public boolean apply(Event evt) {
                    DiscoveryEvent disoEvt = (DiscoveryEvent)evt;

                    if (disoEvt.eventNode().id().equals(srvNodeId)) {
                        info("Expected node failed event: " + ((DiscoveryEvent) evt).eventNode());

                        latch.countDown();
                    }
                    else {
                        log.info("Unexpected node failed event: " + evt);

                        err.set(true);
                    }

                    return true;
                }
            }, EVT_NODE_FAILED);
        }

        Thread.sleep(5000);

        Ignite client = G.ignite("client-0");

        UUID nodeId = client.cluster().localNode().id();

        log.info("Fail coordinator: " + srvNodeId);

        TestTcpDiscoverySpi srvSpi = (TestTcpDiscoverySpi)srv.configuration().getDiscoverySpi();

        srvSpi.pauseAll(false);

        try {
            Thread.sleep(2000);
        }
        finally {
            srvSpi.simulateNodeFailure();
            srvSpi.resumeAll();
        }

        try {
            assertTrue(latch.await(failureThreshold + 3000, TimeUnit.MILLISECONDS));

            assertFalse("Unexpected event, see log for details.", err.get());
            assertEquals(nodeId, client.cluster().localNode().id());
        }
        finally {
            srvSpi.resumeAll();
        }
    }

    /**
     *
     */
    private static class TestTcpDiscoverySpi2 extends TcpDiscoverySpi {
        /** */
        private volatile long readDelay;

        private volatile long writeToSocketDelay;

        /** Throw exception after soket timeout while reading ping response */
        private volatile boolean pingResponseReadFail;

        /** */
        private Exception err;

        protected void writeToSocket(Socket sock, TcpDiscoveryAbstractMessage msg, byte[] data, long timeout) throws IOException {

            if (writeToSocketDelay > 0) {
                try {
                    Thread.sleep(writeToSocketDelay);
                } catch (InterruptedException e) {
                    // Nothing to do.
                }
            }
            if (sock.getSoTimeout() >= writeToSocketDelay) {
                super.writeToSocket(sock, msg, data, timeout);
            } else {
                System.out.println("Drop " + data.length + " bytes with sock timeout=" + sock.getSoTimeout());
                throw new SocketTimeoutException("Write to socket delay timeout exception.");
            }
        }

        protected void writeToSocket(Socket sock,
                                     OutputStream out,
                                     TcpDiscoveryAbstractMessage msg,
                                     long timeout) throws IOException, IgniteCheckedException {
            if (writeToSocketDelay > 0) {
                try {
                    Thread.sleep(writeToSocketDelay);
                } catch (InterruptedException e) {
                    // Nothing to do.
                }
            }

            if (sock.getSoTimeout() >= writeToSocketDelay) {
                super.writeToSocket(sock, out, msg, timeout);
            } else {
                System.out.println("Drop msg" + msg + " with sock timeout=" + sock.getSoTimeout());
                throw new SocketTimeoutException("Write to socket delay timeout exception.");
            }
        }


        /** {@inheritDoc} */
        @Override protected <T> T readMessage(Socket sock, @Nullable InputStream in, long timeout)
            throws IOException, IgniteCheckedException {

            long currentTimeout = getLocalNode().isClient() ?
                clientFailureDetectionTimeout() : failureDetectionTimeout();

            if (readDelay < currentTimeout) {
                T result = null;
                try {
                    result = super.readMessage(sock, in, timeout);
                }
                catch (Exception e) {
                    err = e;

                    throw e;
                }
                if (pingResponseReadFail && result instanceof TcpDiscoveryPingResponse) {
                    U.sleep(sock.getSoTimeout());
                    throw new IOException("Ping response read exception");
                }
                return result;
            }
            else {
                T msg = super.readMessage(sock, in, timeout);

                if (msg instanceof TcpDiscoveryPingRequest) {
                    try {
                        Thread.sleep(2000);
                    }
                    catch (InterruptedException ignored) {
                        // No-op.
                    }

                    throw new SocketTimeoutException("Forced timeout");
                }

                return msg;
            }
        }

        /**
         * Resets testing state.
         */
        private void reset() {
            readDelay = 0;
            writeToSocketDelay = 0;
            pingResponseReadFail = false;
            err = null;
        }
    }
}
