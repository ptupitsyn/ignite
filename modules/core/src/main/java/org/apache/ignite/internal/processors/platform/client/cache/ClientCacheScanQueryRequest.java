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

package org.apache.ignite.internal.processors.platform.client.cache;

import org.apache.ignite.cache.query.QueryCursor;
import org.apache.ignite.cache.query.ScanQuery;
import org.apache.ignite.internal.binary.BinaryRawReaderEx;
import org.apache.ignite.internal.processors.cache.query.QueryCursorEx;
import org.apache.ignite.internal.processors.platform.PlatformContext;
import org.apache.ignite.internal.processors.platform.cache.PlatformCacheEntryFilter;
import org.apache.ignite.internal.processors.platform.client.ClientConnectionContext;
import org.apache.ignite.internal.processors.platform.client.ClientLongResponse;
import org.apache.ignite.internal.processors.platform.client.ClientResponse;
import org.apache.ignite.lang.IgniteBiPredicate;

/**
 * Scan query request.
 */
@SuppressWarnings("unchecked")
public class ClientCacheScanQueryRequest extends ClientCacheRequest {
    /** No filter. */
    private static final byte FILTER_PLATFORM_NONE = 0;

    /** .NET filter. */
    private static final byte FILTER_PLATFORM_DOTNET = 1;

    /** Local flag. */
    private final boolean local;

    /** Page size. */
    private final int pageSize;

    /** Partition. */
    private final Integer partition;

    /** Filter platform. */
    private final byte filterPlatform;

    /** Filter object. */
    private final Object filterObject;

    /**
     * Ctor.
     *
     * @param reader Reader.
     */
    public ClientCacheScanQueryRequest(BinaryRawReaderEx reader) {
        super(reader);

        local = reader.readBoolean();
        pageSize = reader.readInt();
        partition = reader.readBoolean() ? reader.readInt() : null;
        filterPlatform = reader.readByte();

        switch (filterPlatform) {
            case FILTER_PLATFORM_NONE:
                filterObject = null;
                break;

            case FILTER_PLATFORM_DOTNET:
                filterObject = reader.readObjectDetached();
                break;

            default:
                throw new UnsupportedOperationException("Invalid client ScanQuery filter code: " + filterPlatform);
        }
    }

    /** {@inheritDoc} */
    @Override public ClientResponse process(ClientConnectionContext ctx) {
        ScanQuery qry = new ScanQuery()
                .setLocal(local)
                .setPageSize(pageSize)
                .setPartition(partition)
                .setFilter(createFilter(ctx));

        QueryCursor cur = getCache(ctx).query(qry);

        ClientCacheQueryCursor clientCur = new ClientCacheQueryCursor((QueryCursorEx) cur, pageSize);

        long cursorId = ctx.handleRegistry().allocate(clientCur);

        return new ClientLongResponse(getRequestId(), cursorId);
    }

    /**
     * Creates the filter.
     *
     * @return Filter.
     * @param ctx Context.
     */
    private IgniteBiPredicate createFilter(ClientConnectionContext ctx) {
        if (filterObject == null) {
            return null;
        }

        if (filterPlatform != FILTER_PLATFORM_DOTNET) {
            throw new UnsupportedOperationException("Invalid client ScanQuery filter code: " + filterPlatform);
        }

        PlatformContext platformCtx = ctx.kernalContext().platform().context();

        // TODO: Necessary to init?
        return platformCtx.createCacheEntryFilter(false, 0);
    }
}
