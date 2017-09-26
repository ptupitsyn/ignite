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

package org.apache.ignite.internal.processors.platform.events;

import org.apache.ignite.events.Event;
import org.apache.ignite.internal.GridKernalContext;
import org.apache.ignite.internal.binary.BinaryRawWriterEx;
import org.apache.ignite.internal.processors.platform.PlatformContext;
import org.apache.ignite.internal.processors.platform.PlatformEventFilterListener;
import org.apache.ignite.internal.processors.platform.memory.PlatformMemory;
import org.apache.ignite.internal.processors.platform.memory.PlatformOutputStream;
import org.apache.ignite.internal.processors.platform.utils.PlatformUtils;

import java.util.UUID;

/**
 * Platform local event filter. Delegates apply to native platform.
 */
public class PlatformLocalEventListener implements PlatformEventFilterListener {
    /** Listener id. */
    private final int id;

    /** Platform context. */
    private PlatformContext ctx;

    /**
     * Constructor.
     *
     * @param id Listener id.
     */
    public PlatformLocalEventListener(int id) {
        this.id = id;
    }

    /** {@inheritDoc} */
    @Override public void initialize(GridKernalContext kernalCtx) {
        ctx = PlatformUtils.platformContext(kernalCtx.grid());
    }

    /** {@inheritDoc} */
    @Override public void onClose() {
        // No-op: predefined listeners do not need to be released.
    }

    /** {@inheritDoc} */
    @Override public boolean apply(UUID uuid, Event event) {
        // Remote filter - no-op.
        return false;
    }

    /** {@inheritDoc} */
    @Override public boolean apply(Event evt) {
        assert ctx != null;

        try (PlatformMemory mem = ctx.memory().allocate()) {
            PlatformOutputStream out = mem.output();

            BinaryRawWriterEx writer = ctx.writer(out);

            writer.writeInt(id);

            ctx.writeEvent(writer, evt);

            out.synchronize();

            long res = ctx.gateway().eventLocalListenerApply(mem.pointer());

            return res != 0;
        }
    }
}
