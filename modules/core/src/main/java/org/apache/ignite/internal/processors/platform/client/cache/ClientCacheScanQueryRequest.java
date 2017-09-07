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

import org.apache.ignite.binary.BinaryRawReader;
import org.apache.ignite.cache.query.ScanQuery;
import org.apache.ignite.internal.GridKernalContext;
import org.apache.ignite.internal.processors.platform.PlatformContext;
import org.apache.ignite.internal.processors.platform.cache.PlatformCacheEntryFilterImpl;
import org.apache.ignite.internal.processors.platform.client.ClientResponse;
import org.apache.ignite.lang.IgniteBiPredicate;

/**
 * Scan query request.
 */
public class ClientCacheScanQueryRequest extends ClientCacheRequest {
    /** No filter. */
    private static final byte FILTER_PLATFORM_NONE = 0;

    /** .NET filter. */
    private static final byte FILTER_PLATFORM_DOTNET = 1;

    /** Query. */
    private final ScanQuery qry;

    /**
     * Ctor.
     *
     * @param reader Reader.
     */
    @SuppressWarnings("unchecked")
    public ClientCacheScanQueryRequest(BinaryRawReader reader) {
        super(reader);

        qry = new ScanQuery();

        qry.setLocal(reader.readBoolean());
        qry.setPageSize(reader.readInt());

        if (reader.readBoolean()) {
            qry.setPartition(reader.readInt());
        }

        byte filterPlatform = reader.readByte();

        if (filterPlatform == FILTER_PLATFORM_DOTNET) {
            Object dotNetFilter = reader.readObject();  // TODO: Detached
            PlatformContext ctx = null; // TODO

            IgniteBiPredicate filter = new PlatformCacheEntryFilterImpl(dotNetFilter, 0, ctx);

            qry.setFilter(filter);
        }
        else if (filterPlatform != FILTER_PLATFORM_NONE) {
            throw new UnsupportedOperationException("Invalid client ScanQuery filter code: " + filterPlatform);
        }
    }

    /** {@inheritDoc} */
    @Override public ClientResponse process(GridKernalContext ctx) {
        // TODO: Execute query and return id.
        return new ClientCacheScanQueryResponse(getRequestId(),0L);
    }
}
