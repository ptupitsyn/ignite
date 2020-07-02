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

import org.apache.ignite.internal.processors.platform.client.ClientConnectionContext;
import org.apache.ignite.internal.processors.platform.client.ClientMessageParser;
import org.apache.ignite.lang.IgniteAsyncCallback;

import javax.cache.event.CacheEntryEvent;
import javax.cache.event.CacheEntryListenerException;
import javax.cache.event.CacheEntryUpdatedListener;

/**
 * Continuous query handle.
 */
@IgniteAsyncCallback
public class ClientCacheQueryContinuousHandle implements CacheEntryUpdatedListener<Object, Object> {
    /** */
    private final ClientConnectionContext ctx;

    /** */
    private volatile Long continuousQueryId;

    /**
     * Ctor.
     * @param ctx Context.
     */
    public ClientCacheQueryContinuousHandle(ClientConnectionContext ctx) {
        assert ctx != null;

        this.ctx = ctx;
    }

    /** {@inheritDoc} */
    @Override public void onUpdated(Iterable<CacheEntryEvent<?, ?>> iterable) throws CacheEntryListenerException {
        // Client is not yet ready to receive notifications - skip them.
        // TODO: Is this correct in presence of initial query? Should we cache notifications and then send them?
        if (continuousQueryId == null)
            return;

        ClientCacheEntryEventNotification notification = new ClientCacheEntryEventNotification(
                ClientMessageParser.OP_QUERY_CONTINUOUS_EVENT_NOTIFICATION,
                continuousQueryId,
                iterable);

        ctx.notifyClient(notification);
    }

    /**
     * Sets the cursor id.
     * @param continuousQueryId Cursor id.
     */
    public void startNotifications(long continuousQueryId) {
        this.continuousQueryId = continuousQueryId;
    }
}
