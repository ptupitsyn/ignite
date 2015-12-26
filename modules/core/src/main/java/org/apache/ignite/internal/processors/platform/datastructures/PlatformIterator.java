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
import org.apache.ignite.internal.binary.BinaryRawWriterEx;
import org.apache.ignite.internal.processors.platform.PlatformAbstractTarget;
import org.apache.ignite.internal.processors.platform.PlatformContext;

import java.util.Iterator;

public class PlatformIterator extends PlatformAbstractTarget {
    /** Operation: next entry. */
    private static final int OP_NEXT = 1;

    /** Iterator. */
    private final Iterator iter;

    /**
     * Constructor.
     *
     * @param platformCtx Context.
     * @param iter Iterator.
     */
    public PlatformIterator(PlatformContext platformCtx, Iterator iter) {
        super(platformCtx);

        this.iter = iter;
    }

    /** {@inheritDoc} */
    @Override protected void processOutStream(int type, BinaryRawWriterEx writer) throws IgniteCheckedException {
        switch (type) {
            case OP_NEXT:
                if (iter.hasNext()) {
                    Object e = iter.next();

                    writer.writeBoolean(true);

                    writer.writeObjectDetached(e);
                }
                else
                    writer.writeBoolean(false);

                break;

            default:
                super.processOutStream(type, writer);
        }
    }
}
