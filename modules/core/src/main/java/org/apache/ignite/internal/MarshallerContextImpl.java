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

package org.apache.ignite.internal;

import org.apache.ignite.IgniteCheckedException;
import org.apache.ignite.IgniteLogger;
import org.apache.ignite.internal.processors.cache.CachePartialUpdateCheckedException;
import org.apache.ignite.internal.processors.cache.GridCacheAdapter;
import org.apache.ignite.internal.processors.cache.GridCacheContext;
import org.apache.ignite.internal.processors.cache.GridCacheTryPutFailedException;
import org.apache.ignite.internal.util.typedef.F;
import org.apache.ignite.internal.util.typedef.internal.CU;
import org.apache.ignite.internal.util.typedef.internal.U;
import org.apache.ignite.plugin.PluginProvider;

import javax.cache.event.CacheEntryEvent;
import javax.cache.event.CacheEntryListenerException;
import javax.cache.event.CacheEntryUpdatedListener;
import java.io.Externalizable;
import java.io.File;
import java.io.IOException;
import java.io.ObjectInput;
import java.io.ObjectOutput;
import java.util.List;
import java.util.concurrent.CountDownLatch;

/**
 * Marshaller context implementation.
 */
public class MarshallerContextImpl extends MarshallerContextAdapter {
    /** */
    private static final String fileExt = ".classname";

    /** */
    private final CountDownLatch latch = new CountDownLatch(1);

    /** */
    private final File workDir;

    /** */
    private final String cacheName;

    /** */
    private final Byte keyPrefix;

    /** */
    private IgniteLogger log;

    /** */
    private volatile GridCacheAdapter<Object, String> cache;

    /** Non-volatile on purpose. */
    private int failedCnt;

    /** */
    private ContinuousQueryListener lsnr;

    /**
     * @param plugins Plugins.
     * @throws IgniteCheckedException In case of error.
     */
    public MarshallerContextImpl(List<PluginProvider> plugins) throws IgniteCheckedException {
        this(plugins, CU.MARSH_CACHE_NAME, null);
    }

    /**
     * @param plugins Plugins.
     * @param cacheName Cache name.
     * @param keyPrefix Composite key prefix.
     * @throws IgniteCheckedException In case of error.
     */
    public MarshallerContextImpl(List<PluginProvider> plugins, String cacheName, Byte keyPrefix)
        throws IgniteCheckedException {
        super(plugins);

        assert fileExt != null;

        workDir = U.resolveWorkDirectory("marshaller", false);

        this.cacheName = cacheName;
        this.keyPrefix = keyPrefix;
    }

    /**
     * @param ctx Context.
     * @throws IgniteCheckedException If failed.
     */
    public void onContinuousProcessorStarted(GridKernalContext ctx) throws IgniteCheckedException {
        if (ctx.clientNode()) {
            lsnr = new ContinuousQueryListener(ctx.log(MarshallerContextImpl.class), workDir, fileExt);

            ctx.continuous().registerStaticRoutine(
                cacheName,
                lsnr,
                null,
                null);
        }
    }

    /**
     * @param ctx Kernal context.
     * @throws IgniteCheckedException In case of error.
     */
    public void onMarshallerCacheStarted(GridKernalContext ctx) throws IgniteCheckedException {
        assert ctx != null;

        log = ctx.log(MarshallerContextImpl.class);

        cache = ctx.cache().internalCache(cacheName);
        final GridCacheContext<Object, String> cacheCtx = cache.context();

        if (cacheCtx.affinityNode()) {
            cacheCtx.continuousQueries().executeInternalQuery(
                    new ContinuousQueryListener(log, workDir, fileExt),
                    null,
                    true,
                    true,
                    false
            );
        }
        else {
            if (lsnr != null) {
                ctx.closure().runLocalSafe(new Runnable() {
                    @SuppressWarnings("unchecked")
                    @Override public void run() {
                        try {
                            Iterable entries = cacheCtx.continuousQueries().existingEntries(false, null);

                            lsnr.onUpdated(entries);
                        }
                        catch (IgniteCheckedException e) {
                            U.error(log, "Failed to load marshaller cache entries: " + e, e);
                        }
                    }
                });
            }
        }

        latch.countDown();
    }

    /**
     * Release marshaller context.
     */
    public void onKernalStop() {
        latch.countDown();
    }

    /** {@inheritDoc} */
    @Override public boolean registerClassName(int id, String clsName) throws IgniteCheckedException {
        GridCacheAdapter<Object, String> cache0 = cache;

        if (cache0 == null)
            return false;

        Object key = getKey(id);

        try {
            String old = cache0.tryGetAndPut(key, clsName);

            if (old != null && !old.equals(clsName))
                throw new IgniteCheckedException("Type ID collision detected [id=" + id + ", clsName1=" + clsName +
                    ", clsName2=" + old + ']');

            failedCnt = 0;

            return true;
        }
        catch (CachePartialUpdateCheckedException | GridCacheTryPutFailedException ignored) {
            if (++failedCnt > 10) {
                if (log.isQuiet())
                    U.quiet(false, "Failed to register marshalled class for more than 10 times in a row " +
                        "(may affect performance).");

                failedCnt = 0;
            }

            return false;
        }
    }

    /** {@inheritDoc} */
    @Override public String className(int id) throws IgniteCheckedException {
        GridCacheAdapter<Object, String> cache0 = cache;

        if (cache0 == null) {
            U.awaitQuiet(latch);

            cache0 = cache;

            if (cache0 == null)
                throw new IllegalStateException("Failed to initialize marshaller context (grid is stopping).");
        }

        Object key = getKey(id);

        String clsName = cache0.getTopologySafe(key);

        if (clsName == null) {
            clsName = MarshallerWorkDirectory.getTypeNameFromFile(key, workDir, fileExt);

            // Must explicitly put entry to cache to invoke other continuous queries.
            registerClassName(id, clsName);
        }

        return clsName;
    }

    /**
     * Gets the cache key for a type id.
     *
     * @param id Id.
     * @return Cache key depending on keyPrefix.
     */
    private Object getKey(int id) {
        if (keyPrefix != null)
            return new TypeIdKey(keyPrefix, id);

        return id;
    }

    /**
     */
    private static class ContinuousQueryListener implements CacheEntryUpdatedListener<Object, String> {
        /** */
        private final IgniteLogger log;

        /** */
        private final File workDir;

        /** */
        private final String fileExt;

        /**
         * @param log Logger.
         * @param workDir Work directory.
         * @param fileExt File extension.
         */
        private ContinuousQueryListener(IgniteLogger log, File workDir, String fileExt) {
            this.log = log;
            this.workDir = workDir;
            this.fileExt = fileExt;
        }

        /** {@inheritDoc} */
        @Override public void onUpdated(Iterable<CacheEntryEvent<?, ? extends String>> evts)
            throws CacheEntryListenerException {
            for (CacheEntryEvent<?, ? extends String> evt : evts) {
                assert evt.getOldValue() == null || F.eq(evt.getOldValue(), evt.getValue()):
                    "Received cache entry update for system marshaller cache: " + evt;

                if (evt.getOldValue() == null)
                    MarshallerWorkDirectory.writeTypeNameToFile(evt.getKey(), evt.getValue(), fileExt, log, workDir);
            }
        }
    }

    /**
     * Composite type key.
     *
     * Each platform can have a different type name for a given type id.
     * Composite key allows sharing a single marshaller cache between multiple platforms.
     */
    private static class TypeIdKey implements Externalizable {
        /** */
        private static final long serialVersionUID = 0L;

        /** */
        private byte prefix;

        /** */
        private int id;

        /**
         * Default ctor for serialization.
         */
        public TypeIdKey() {
            // No-op.
        }

        /**
         * Ctor.
         *
         * @param prefix Prefix.
         * @param id Id.
         */
        private TypeIdKey(byte prefix, int id) {
            this.prefix = prefix;
            this.id = id;
        }

        /** {@inheritDoc} */
        @Override public void writeExternal(ObjectOutput out) throws IOException {
            out.writeByte(prefix);
            out.writeInt(id);
        }

        /** {@inheritDoc} */
        @Override public void readExternal(ObjectInput in) throws IOException, ClassNotFoundException {
            prefix = in.readByte();
            id = in.readInt();
        }

        /** {@inheritDoc} */
        @Override public boolean equals(Object o) {
            if (this == o)
                return true;
            if (o == null || getClass() != o.getClass())
                return false;

            TypeIdKey key = (TypeIdKey)o;

            return prefix == key.prefix && id == key.id;
        }

        /** {@inheritDoc} */
        @Override public int hashCode() {
            return 31 * (int)prefix + id;
        }

        /** {@inheritDoc} */
        @Override public String toString() {
            return prefix + "_" + id;
        }
    }
}
