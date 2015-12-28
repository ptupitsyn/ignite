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

import org.apache.ignite.IgniteCheckedException;
import org.apache.ignite.IgniteQueue;
import org.apache.ignite.internal.binary.BinaryRawReaderEx;
import org.apache.ignite.internal.binary.BinaryRawWriterEx;
import org.apache.ignite.internal.processors.platform.PlatformAbstractTarget;
import org.apache.ignite.internal.processors.platform.PlatformContext;

/**
 * Platform queue.
 */
@SuppressWarnings("unchecked")
public class PlatformQueue extends PlatformAbstractTarget {
    /** */
    private static final int OP_ADD = 1;

    /** */
    private static final int OP_REMOVE = 2;

    /** */
    private static final int OP_TO_ARRAY = 3;

    /** Underlying queue. */
    private final IgniteQueue queue;

    /**
     * Constructor.
     *
     * @param platformCtx Context.
     */
    public PlatformQueue(PlatformContext platformCtx, IgniteQueue queue) {
        super(platformCtx);

        assert queue != null;

        this.queue = queue;
    }

    /**
     * Create cache iterator.
     *
     * @return Cache iterator.
     */
    public PlatformIterator iterator() {
        return new PlatformIterator(platformCtx, queue.iterator());
    }

    /**
     * Closes the queue.
     */
    public void close() {
        queue.close();
    }

    /**
     * Determines whether underlying queue has been removed.
     *
     * @return True if the queue has been removed; otherwise, false.
     */
    public boolean isClosed() {
        return queue.removed();
    }

    /**
     * Determines whether underlying queue is empty.
     *
     * @return True if queue is empty; otherwise, false.
     */
    public boolean isEmpty() {
        return queue.isEmpty();
    }

    /**
     * Gets the queue size.
     *
     * @return Size.
     */
    public int size() {
        return queue.size();
    }

    /**
     * Removes all items from the queue.
     *
     * @param batchSize Batch size.
     */
    public void clear(int batchSize) {
        queue.clear(batchSize);
    }

    /**
     * Gets the capacity of the queue.
     *
     * @return Capacity.
     */
    public int capacity() {
        return queue.capacity();
    }

    /**
     * Gets a value indicating whether underlying queue is colocated.
     *
     * @return True if colocated; otherwise, false.
     */
    public boolean isColocated() {
        return queue.collocated();
    }

    /** {@inheritDoc} */
    @Override protected long processInStreamOutLong(int type, BinaryRawReaderEx reader) throws IgniteCheckedException {
        switch (type) {
            case OP_ADD:
                return queue.add(reader.readObject()) ? TRUE : FALSE;

            case OP_REMOVE:
                return queue.remove(reader.readObject()) ? TRUE : FALSE;

            default:
                return super.processInStreamOutLong(type, reader);
        }
    }

    /** {@inheritDoc} */
    @Override protected void processOutStream(int type, BinaryRawWriterEx writer) throws IgniteCheckedException {
        switch (type) {
            case OP_TO_ARRAY:
                writer.writeObjectArray(queue.toArray());

                break;
            default:
                super.processOutStream(type, writer);
        }
    }
}
