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

import org.apache.ignite.IgniteException;
import org.apache.ignite.binary.BinaryRawWriter;
import org.apache.ignite.internal.GridKernalContext;
import org.apache.ignite.internal.binary.BinaryRawReaderEx;
import org.apache.ignite.internal.binary.GridBinaryMarshaller;
import org.apache.ignite.internal.binary.streams.BinaryHeapInputStream;
import org.apache.ignite.internal.binary.streams.BinaryHeapOutputStream;
import org.apache.ignite.internal.binary.streams.BinaryInputStream;
import org.apache.ignite.internal.processors.cache.binary.CacheObjectBinaryProcessorImpl;
import org.apache.ignite.internal.processors.odbc.SqlListenerMessageParser;
import org.apache.ignite.internal.processors.odbc.SqlListenerRequest;
import org.apache.ignite.internal.processors.odbc.SqlListenerResponse;
import org.apache.ignite.internal.processors.platform.client.binary.ClientGetBinaryTypeNameRequest;
import org.apache.ignite.internal.processors.platform.client.binary.ClientGetBinaryTypeSchemaRequest;
import org.apache.ignite.internal.processors.platform.client.binary.ClientPutBinaryTypesRequest;
import org.apache.ignite.internal.processors.platform.client.binary.ClientRegisterBinaryTypeNameRequest;
import org.apache.ignite.internal.processors.platform.client.cache.ClientCacheGetRequest;
import org.apache.ignite.internal.processors.platform.client.cache.ClientCachePutRequest;
import org.apache.ignite.internal.processors.platform.client.cache.ClientCacheScanQueryRequest;

/**
 * Thin client message parser.
 */
public class ClientMessageParser implements SqlListenerMessageParser {
    /** */
    private static final short OP_CACHE_GET = 1;

    /** */
    private static final short OP_GET_BINARY_TYPE_NAME = 2;

    /** */
    private static final short OP_GET_BINARY_TYPE_SCHEMA = 3;

    /** */
    private static final short OP_CACHE_PUT = 4;

    /** */
    private static final short OP_REGISTER_BINARY_TYPE_NAME = 5;

    /** */
    private static final short OP_PUT_BINARY_TYPES = 6;

    /** */
    private static final short OP_QUERY_SCAN = 7;

    /** Marshaller. */
    private final GridBinaryMarshaller marsh;

    /**
     * Ctor.
     *
     * @param ctx Kernal context.
     */
    public ClientMessageParser(GridKernalContext ctx) {
        assert ctx != null;

        CacheObjectBinaryProcessorImpl cacheObjProc = (CacheObjectBinaryProcessorImpl)ctx.cacheObjects();
        marsh = cacheObjProc.marshaller();
    }

    /** {@inheritDoc} */
    @Override public SqlListenerRequest decode(byte[] msg) {
        assert msg != null;

        BinaryInputStream inStream = new BinaryHeapInputStream(msg);
        BinaryRawReaderEx reader = marsh.reader(inStream);

        short opCode = reader.readShort();

        switch (opCode) {
            case OP_CACHE_GET:
                return new ClientCacheGetRequest(reader);

            case OP_GET_BINARY_TYPE_NAME:
                return new ClientGetBinaryTypeNameRequest(reader);

            case OP_GET_BINARY_TYPE_SCHEMA:
                return new ClientGetBinaryTypeSchemaRequest(reader);

            case OP_CACHE_PUT:
                return new ClientCachePutRequest(reader);

            case OP_REGISTER_BINARY_TYPE_NAME:
                return new ClientRegisterBinaryTypeNameRequest(reader);

            case OP_PUT_BINARY_TYPES:
                return new ClientPutBinaryTypesRequest(reader);

            case OP_QUERY_SCAN:
                return new ClientCacheScanQueryRequest(reader);
        }

        throw new IgniteException("Invalid operation: " + opCode);
    }

    /** {@inheritDoc} */
    @Override public byte[] encode(SqlListenerResponse resp) {
        BinaryHeapOutputStream outStream = new BinaryHeapOutputStream(32);

        BinaryRawWriter writer = marsh.writer(outStream);

        ((ClientResponse)resp).encode(writer);

        return outStream.arrayCopy();
    }
}
