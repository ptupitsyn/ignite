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

package org.apache.ignite.platform;

import org.apache.ignite.Ignite;
import org.apache.ignite.IgniteCache;
import org.apache.ignite.IgniteException;
import org.apache.ignite.Ignition;
import org.apache.ignite.cache.CacheEntryEventSerializableFilter;
import org.apache.ignite.cache.query.ContinuousQuery;
import org.apache.ignite.cluster.ClusterNode;
import org.apache.ignite.compute.ComputeJob;
import org.apache.ignite.compute.ComputeJobAdapter;
import org.apache.ignite.compute.ComputeJobResult;
import org.apache.ignite.compute.ComputeTaskAdapter;
import org.apache.ignite.internal.util.typedef.F;
import org.apache.ignite.resources.IgniteInstanceResource;
import org.jetbrains.annotations.Nullable;

import javax.cache.event.CacheEntryEvent;
import javax.cache.event.CacheEntryListenerException;
import javax.cache.event.CacheEntryUpdatedListener;
import java.util.Collections;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

/**
 * Task run a continuous query with a Java filter.
 */
public class PlatformRunContinuousQueryTask extends ComputeTaskAdapter<String, Boolean> {
    /** {@inheritDoc} */
    @Nullable @Override public Map<? extends ComputeJob, ClusterNode> map(List<ClusterNode> subgrid,
        @Nullable String arg) throws IgniteException {
        return Collections.singletonMap(new PlatformRunContinuousQueryJob(arg), F.first(subgrid));
    }

    /** {@inheritDoc} */
    @Nullable @Override public Boolean reduce(List<ComputeJobResult> results) throws IgniteException {
        ComputeJobResult res = results.get(0);

        if (res.getException() != null)
            throw res.getException();
        else
            return results.get(0).getData();
    }

    /**
     * Job.
     */
    private static class PlatformRunContinuousQueryJob extends ComputeJobAdapter {
        /** */
        private final String cacheName;

        /** Ignite. */
        @SuppressWarnings("UnusedDeclaration")
        @IgniteInstanceResource
        private Ignite ignite;

        /**
         * Ctor.
         * @param cacheName Name.
         */
        private PlatformRunContinuousQueryJob(String cacheName) {
            this.cacheName = cacheName;
        }

        /** {@inheritDoc} */
        @Override public Object execute() throws IgniteException {
            final IgniteCache localCache = ignite.cache(cacheName + "_local");

            ContinuousQuery qry = new ContinuousQuery().setRemoteFilter(new CacheEntryEventSerializableFilter() {
                @Override public boolean evaluate(CacheEntryEvent event) throws CacheEntryListenerException {
                    // TODO: Insert logic
                    return true;
                }
            }).setLocalListener(new CacheEntryUpdatedListener() {
                @Override public void onUpdated(Iterable events) throws CacheEntryListenerException {
                    for (Iterator iterator = events.iterator(); iterator.hasNext();) {
                        CacheEntryEvent element = (CacheEntryEvent) iterator.next();
                        switch (element.getEventType())
                        {
                            case CREATED:
                            case UPDATED:
                                localCache.put(element.getKey(), element.getValue());
                                break;
                            case REMOVED:
                                localCache.remove(element.getKey());
                                break;
                        }
                    }
                }
            });

            ignite.cache(cacheName).query(qry);

            return null;
        }
    }
}
