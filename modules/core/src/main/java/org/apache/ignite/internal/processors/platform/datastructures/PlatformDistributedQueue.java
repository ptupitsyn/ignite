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

package org.apache.ignite.internal.processors.platform.datastructures;

import org.apache.ignite.IgniteQueue;
import org.apache.ignite.internal.processors.platform.PlatformAbstractTarget;
import org.apache.ignite.internal.processors.platform.PlatformContext;

/**
 * Platform queue.
 */
public class PlatformDistributedQueue extends PlatformAbstractTarget {
    /** Underlying queue. */
    private final IgniteQueue queue;

    /**
     * Constructor.
     *
     * @param platformCtx Context.
     */
    public PlatformDistributedQueue(PlatformContext platformCtx, IgniteQueue queue) {
        super(platformCtx);

        assert queue != null;

        this.queue = queue;
    }
}
