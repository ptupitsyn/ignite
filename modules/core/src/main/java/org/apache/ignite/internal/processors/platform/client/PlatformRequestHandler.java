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

package org.apache.ignite.internal.processors.platform.client;

import org.apache.ignite.IgniteCheckedException;
import org.apache.ignite.IgniteException;
import org.apache.ignite.binary.BinaryRawWriter;
import org.apache.ignite.internal.binary.BinaryRawReaderEx;
import org.apache.ignite.internal.binary.BinaryWriterExImpl;
import org.apache.ignite.internal.binary.streams.BinaryHeapInputStream;
import org.apache.ignite.internal.binary.streams.BinaryHeapOutputStream;
import org.apache.ignite.internal.binary.streams.BinaryInputStream;
import org.apache.ignite.internal.processors.odbc.SqlListenerRequest;
import org.apache.ignite.internal.processors.odbc.SqlListenerRequestHandler;
import org.apache.ignite.internal.processors.odbc.SqlListenerResponse;
import org.apache.ignite.internal.processors.platform.PlatformProcessor;
import org.apache.ignite.internal.processors.platform.PlatformTarget;
import org.apache.ignite.internal.util.typedef.X;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Platform thin client request handler.
 */
public class PlatformRequestHandler implements SqlListenerRequestHandler {
    /** Platform processor. */
    private final PlatformProcessor proc;

    /**
     * Target registry.
     */
    private final Map<Long, PlatformTarget> targets = new ConcurrentHashMap<>();

    /** */
    // TODO: How do we release targets?
    private final AtomicLong targetIdGen = new AtomicLong();

    /** */
    private static final byte OP_IN_LONG_OUT_LONG = 1;

    /** */
    private static final byte OP_IN_STREAM_OUT_LONG = 2;

    /** */
    private static final byte OP_IN_STREAM_OUT_STREAM = 3;

    /** */
    private static final byte OP_IN_STREAM_OUT_OBJECT = 4;

    /** */
    private static final byte OP_IN_OBJECT_STREAM_OUT_OBJECT_STREAM = 5;

    /** */
    private static final byte OP_OUT_STREAM = 6;

    /** */
    private static final byte OP_OUT_OBJECT = 7;

    /** */
    private static final byte OP_IN_STREAM_ASYNC = 8;

    /**
     * Ctor.
     *
     * @param proc Platform processor.
     */
    public PlatformRequestHandler(PlatformProcessor proc) {
        assert proc != null;

        this.proc = proc;

        registerTarget((PlatformTarget) proc);
    }

    /** {@inheritDoc} */
    @Override public SqlListenerResponse handle(SqlListenerRequest req) {
        PlatformRequest req0 = (PlatformRequest)req;

        BinaryInputStream inStream = new BinaryHeapInputStream(req0.getData());
        BinaryRawReaderEx reader = proc.context().reader(inStream);

        // TODO: Use PlatformPooledMemory instead!
        BinaryHeapOutputStream outStream = new BinaryHeapOutputStream(32);
        BinaryRawWriter writer = new BinaryWriterExImpl(null, outStream,
                null, null);

        try {
            processCommand(reader, writer);
        } catch (IgniteCheckedException e) {
            return new PlatformResponse(SqlListenerResponse.STATUS_FAILED, X.getFullStackTrace(e), null);
        }

        return new PlatformResponse(SqlListenerResponse.STATUS_SUCCESS, null, outStream.array());
    }

    /**
     * Processes the command.
     *
     * @param reader Reader.
     * @param writer Writer.
     * @throws IgniteCheckedException On error.
     */
    private void processCommand(BinaryRawReaderEx reader, BinaryRawWriter writer) throws IgniteCheckedException {
        byte cmd = reader.readByte();
        PlatformTarget target = getTarget(reader.readLong());
        int opCode = reader.readInt();

        switch (cmd) {
            case OP_IN_LONG_OUT_LONG: {
                long res = target.processInLongOutLong(opCode, reader.readLong());
                writer.writeLong(res);
                return;
            }

            case OP_IN_STREAM_OUT_LONG: {
                // TODO: Pass memory!
                long res = target.processInStreamOutLong(opCode, reader, null);
            }

            case OP_IN_STREAM_OUT_OBJECT: {
                PlatformTarget res = target.processInStreamOutObject(opCode, reader);
                long resId = registerTarget(res);
                writer.writeLong(resId);
                return;
            }
        }

        throw new IgniteException("Invalid command: " + cmd);
    }

    /**
     * Registers the target for future access from platform side.
     *
     * @param target Target.
     * @return Unique id.
     */
    private long registerTarget(PlatformTarget target) {
        assert target != null;

        long id = targetIdGen.incrementAndGet();

        targets.put(id, target);

        return id;
    }

    /**
     * Gets the target by id.
     *
     * @param id Target id.
     * @return Target.
     */
    private PlatformTarget getTarget(long id) {
        PlatformTarget target = targets.get(id);

        if (target == null) {
            throw new IgniteException("Resource handle does not exist: " + id);
        }

        return target;
    }

    /** {@inheritDoc} */
    @Override public SqlListenerResponse handleException(Exception e) {
        // TODO: ??
        return null;
    }
}