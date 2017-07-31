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
import org.apache.ignite.internal.binary.BinaryRawReaderEx;
import org.apache.ignite.internal.binary.streams.BinaryHeapInputStream;
import org.apache.ignite.internal.binary.streams.BinaryInputStream;
import org.apache.ignite.internal.processors.odbc.SqlListenerRequest;
import org.apache.ignite.internal.processors.odbc.SqlListenerRequestHandler;
import org.apache.ignite.internal.processors.odbc.SqlListenerResponse;
import org.apache.ignite.internal.processors.platform.PlatformProcessor;
import org.apache.ignite.internal.processors.platform.PlatformTarget;
import org.apache.ignite.internal.util.typedef.X;

/**
 * Platform thin client request handler.
 */
public class PlatformRequestHandler implements SqlListenerRequestHandler {
    /** Platform processor. */
    private final PlatformProcessor proc;

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
    }

    /** {@inheritDoc} */
    @Override public SqlListenerResponse handle(SqlListenerRequest req) {
        PlatformRequest req0 = (PlatformRequest)req;
        PlatformTarget target = (PlatformTarget)proc;

        BinaryInputStream stream = new BinaryHeapInputStream(req0.getData());
        BinaryRawReaderEx reader = proc.context().reader(stream);

        byte cmd = reader.readByte();

        try {
            switch (cmd) {
                case OP_IN_LONG_OUT_LONG: {
                    long res = target.processInLongOutLong(reader.readInt(), reader.readLong());
                }
            }
        } catch (IgniteCheckedException e) {
            return new PlatformResponse(SqlListenerResponse.STATUS_FAILED, X.getFullStackTrace(e), null);
        }

        return new PlatformResponse(SqlListenerResponse.STATUS_SUCCESS, null, null);
    }

    /** {@inheritDoc} */
    @Override public SqlListenerResponse handleException(Exception e) {
        return null;
    }
}