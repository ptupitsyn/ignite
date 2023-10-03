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

package org.apache.ignite.internal.util.nio;

import java.io.OutputStream;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.nio.ByteOrder;
import java.util.Arrays;
import java.util.concurrent.CountDownLatch;
import javax.net.ssl.SSLContext;
import org.apache.ignite.IgniteCheckedException;
import org.apache.ignite.internal.util.nio.ssl.GridNioSslFilter;
import org.apache.ignite.internal.util.typedef.internal.U;
import org.apache.ignite.testframework.GridTestUtils;
import org.apache.logging.log4j.Level;
import org.apache.logging.log4j.core.config.Configurator;
import org.junit.Test;

import static java.util.concurrent.TimeUnit.SECONDS;

/**
 * Tests for new NIO server with SSL enabled.
 */
public class GridNioSslSelfTest extends GridNioSelfTest {
    /** Test SSL context. */
    private static SSLContext sslCtx;

    /** {@inheritDoc} */
    @Override protected void beforeTestsStarted() throws Exception {
        super.beforeTestsStarted();

        sslCtx = GridTestUtils.sslContext();
    }

    /** {@inheritDoc} */
    @Override protected Socket createSocket() throws IgniteCheckedException {
        try {
            return sslCtx.getSocketFactory().createSocket();
        }
        catch (Exception e) {
            throw new IgniteCheckedException(e);
        }
    }

    /** {@inheritDoc} */
    @SuppressWarnings("unchecked")
    @Override protected GridNioServer.Builder<?> serverBuilder(int port,
        GridNioParser parser,
        GridNioServerListener lsnr
    ) throws Exception {
        return GridNioServer.builder()
            .address(U.getLocalHost())
            .port(port)
            .listener(lsnr)
            .logger(log)
            .selectorCount(2)
            .igniteInstanceName("nio-test-grid")
            .tcpNoDelay(false)
            .directBuffer(true)
            .byteOrder(ByteOrder.nativeOrder())
            .socketSendBufferSize(0)
            .socketReceiveBufferSize(0)
            .sendQueueLimit(0)
            .filters(
                new GridNioCodecFilter(parser, log, false),
                new GridNioSslFilter(sslCtx, true, ByteOrder.nativeOrder(), log, null));
    }

    /** {@inheritDoc} */
    @Test
    @Override public void testWriteTimeout() throws Exception {
        // Skip base test because it enables "skipWrite" mode in the GridNioServer
        // which makes SSL handshake impossible.
    }

    /** {@inheritDoc} */
    @Test
    @Override public void testAsyncSendReceive() throws Exception {
        // No-op, do not want to mess with SSL channel.
    }

    private static byte[] buildMessage() {
        // https://github.com/eclipse/jetty.project/issues/6072
        byte[] bytes = new byte[20005];
        bytes[0] = 22;  // record type
        bytes[1] = 3;   // major version
        bytes[2] = 3;   // minor version
        bytes[3] = 78; // record length 2 bytes
        bytes[4] = 32;  // record length

        bytes[5] = 1; // message type
        bytes[6] = 0; // message length 3 bytes
        bytes[7] = 78;
        bytes[8] = 23;

        for( int i = 9; i < bytes.length; i++) {
            bytes[i] = 1;
        }
        return bytes;
    }

    @Test
    public void testInvalidLargeTLSFrame() throws Exception {
        // This test does not cause issues in Ignite, because
        // GridNioSslHandler.messageReceived expands buffer to accommodate big messages.
        Configurator.setRootLevel(Level.TRACE);

        CountDownLatch latch = new CountDownLatch(1);
        NioListener lsnr = new NioListener(latch);
        GridNioServer<?> srvr = startServer(new GridBufferedParser(true, ByteOrder.nativeOrder()), lsnr);

        // Create raw TLS record.
        byte[] bytes = new byte[40005];
        Arrays.fill(bytes, (byte) 1);

        bytes[0] = 22; // record type
        bytes[1] = 3;  // major version
        bytes[2] = 3;  // minor version
        bytes[3] = (byte) 0x9c; // record length 2 bytes / 0x9C40 / decimal 40,000
        bytes[4] = 0x40; // record length
        bytes[5] = 1;  // message type
        bytes[6] = 0;  // message length 3 bytes / 0x009C37 / decimal 39,991
        bytes[7] = (byte) 0x9c;
        bytes[8] = 0x37;

        try (Socket s = createSocket()) {
            s.connect(new InetSocketAddress(U.getLocalHost(), srvr.port()), 1000);
            {
                s.getOutputStream().write(bytes);

                // Sleep to see if the server spins.
                Thread.sleep(1000);

                // Read until -1 or read timeout.
                s.setSoTimeout(100_000);
                while (s.getInputStream().read() != -1) {
                }
            }
        }
    }

    @Test
    public void testSendReceiveRaw() throws Exception {
        CountDownLatch latch = new CountDownLatch(1);
        NioListener lsnr = new NioListener(latch);
        GridNioServer<?> srvr = startServer(new GridBufferedParser(true, ByteOrder.nativeOrder()), lsnr);

        byte[] payload = buildMessage();

        // Send payload to a raw Java socket
        try (Socket s = createSocket()) {
            s.connect(new InetSocketAddress(U.getLocalHost(), srvr.port()), 1000);
            OutputStream outputStream = s.getOutputStream();
            outputStream.write(payload);
            outputStream.flush();

//            // Write payload byte by byte
//            for (byte b : payload) {
//                Thread.sleep(100);
//                outputStream.write(b);
//                outputStream.flush();
//            }
        }

        assert latch.await(50, SECONDS);
        srvr.stop();
    }

    @Test
    public void testSendReceive2() throws Exception {
        // TODO: This bypasses ClientListenerNioListener, we need an integration test?
        // Or maybe not, just investigate why a call to SSLEngineImpl.checkParams can be stuck in a loop
        // (The thread is RUNNABLE)
        CountDownLatch latch = new CountDownLatch(1);

        NioListener lsnr = new NioListener(latch);

        GridNioServer<?> srvr = startServer(new GridBufferedParser(true, ByteOrder.nativeOrder()), lsnr);

        TestClient client = null;

        try {
            client = createClient(U.getLocalHost(), srvr.port(), U.getLocalHost());

            client.sendMessage(createMessage(), MSG_SIZE);

            client.close();

            assert latch.await(30, SECONDS);

            assertEquals("Unexpected message count", 1, lsnr.getMessageCount());
        }
        finally {
            srvr.stop();

            if (client != null)
                client.close();
        }
    }
}
