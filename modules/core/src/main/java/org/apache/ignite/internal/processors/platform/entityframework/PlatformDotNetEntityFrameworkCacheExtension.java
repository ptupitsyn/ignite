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

package org.apache.ignite.internal.processors.platform.entityframework;

import org.apache.ignite.Ignite;
import org.apache.ignite.IgniteCache;
import org.apache.ignite.IgniteCheckedException;
import org.apache.ignite.cache.CachePeekMode;
import org.apache.ignite.internal.binary.BinaryRawReaderEx;
import org.apache.ignite.internal.processors.platform.cache.PlatformCache;
import org.apache.ignite.internal.processors.platform.cache.PlatformCacheExtension;
import org.apache.ignite.internal.processors.platform.memory.PlatformMemory;
import org.apache.ignite.internal.util.typedef.internal.S;
import org.apache.ignite.lang.IgniteRunnable;
import org.apache.ignite.resources.IgniteInstanceResource;

import javax.cache.Cache;
import javax.cache.processor.EntryProcessorResult;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

/**
 * EntityFramework cache extension.
 */
public class PlatformDotNetEntityFrameworkCacheExtension implements PlatformCacheExtension {
    /** Extension ID. */
    private static final int EXT_ID = 1;

    /** Operation: increment entity set versions. */
    private static final int OP_INVALIDATE_SETS = 1;

    /** Cache key for cleanup node ID. Contains characters not allowed in SQL table name. */
    private static final String CLEANUP_NODE_ID = " ^CLEANUP_NODE_ID^ ";

    /** {@inheritDoc} */
    @Override public int id() {
        return EXT_ID;
    }

    /** {@inheritDoc} */
    @SuppressWarnings("unchecked")
    @Override public long processInOutStreamLong(PlatformCache target, int type, BinaryRawReaderEx reader,
        PlatformMemory mem) throws IgniteCheckedException {
        switch (type) {
            case OP_INVALIDATE_SETS: {
                final IgniteCache<String, Long> metaCache = (IgniteCache<String, Long>)target.rawCache();
                final String dataCacheName = reader.readString();

                int cnt = reader.readInt();
                final Set<String> entitySetNames = new HashSet(cnt);

                for (int i = 0; i < cnt; i++)
                    entitySetNames.add(reader.readString());

                final Map<String, EntryProcessorResult<Long>> currentVersions =
                    metaCache.invokeAll(entitySetNames,
                    new PlatformDotNetEntityFrameworkIncreaseVersionProcessor());

                // Initiate old entries cleanup.
                Ignite grid = target.platformContext().kernalContext().grid();

                grid.compute().broadcast(new IgniteRunnable() {
                    @IgniteInstanceResource
                    private Ignite ignite;

                    @Override public void run() {
                        removeOldEntries(ignite, dataCacheName, currentVersions);
                    }
                });

                // TODO:
                // 0) Use a separate meta cache for versions and cleanup state
                // 1) limit cleanup tasks: store node id in a special key.
                //    If node is present, then cleanup is in process;
                //    If node has left, then start new cleanup.
                // 2) do not use public thread pool - HOW? ComputeJobContinuation, new thread, holdcc, callcc
                // 3) cache can have a node filter?
                // 4) we should account for lost data: meta cache should have backups.
                //grid.compute().broadcast()

                return target.writeResult(mem, null);
            }
        }

        throw new IgniteCheckedException("Unsupported operation type: " + type);
    }

    /** {@inheritDoc} */
    @Override public String toString() {
        return S.toString(PlatformDotNetEntityFrameworkCacheExtension.class, this);
    }

    /**
     * Removes old cache entries locally.
     *
     * @param ignite Ignite.
     * @param dataCacheName Cache name.
     * @param currentVersions Current versions.
     */
    private static void removeOldEntries(Ignite ignite, String dataCacheName,
        Map<String, EntryProcessorResult<Long>> currentVersions) {
        IgniteCache<String, PlatformDotNetEntityFrameworkCacheEntry> cache = ignite.cache(dataCacheName);

        for (Cache.Entry<String, PlatformDotNetEntityFrameworkCacheEntry> cacheEntry :
            cache.localEntries(CachePeekMode.ALL)) {
            PlatformDotNetEntityFrameworkCacheEntry entry = cacheEntry.getValue();

            for (Map.Entry<String, Long> entitySet : entry.entitySets().entrySet()) {
                EntryProcessorResult<Long> curVer = currentVersions.get(entitySet.getKey());

                if (curVer != null && entitySet.getValue() < curVer.get())
                    cache.remove(cacheEntry.getKey());
            }
        }
    }
}
