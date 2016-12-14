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

import org.apache.ignite.IgniteCheckedException;
import org.apache.ignite.IgniteException;
import org.apache.ignite.cluster.ClusterNode;
import org.apache.ignite.internal.binary.BinaryRawReaderEx;
import org.apache.ignite.internal.binary.BinaryRawWriterEx;
import org.apache.ignite.internal.processors.platform.PlatformAbstractTarget;
import org.apache.ignite.internal.processors.platform.PlatformContext;
import org.apache.ignite.internal.processors.platform.PlatformTarget;
import org.apache.ignite.plugin.*;
import org.jetbrains.annotations.Nullable;

import java.io.Serializable;
import java.util.UUID;

/**
 * Test plugin provider.
 */
public class PlatformTestPluginProvider implements PluginProvider<PluginConfiguration> {
    /** */
    private final PlatformTestPlugin plugin = new PlatformTestPlugin();

    /** {@inheritDoc} */
    @Override public String name() {
        return "PlatformTestPlugin";
    }

    /** {@inheritDoc} */
    @Override public String version() {
        return "1.2.3";
    }

    /** {@inheritDoc} */
    @Override public String copyright() {
        return "copyleft";
    }

    /** {@inheritDoc} */
    @Override public <T extends IgnitePlugin> T plugin() {
        return (T)plugin;
    }

    /** {@inheritDoc} */
    @Override public void initExtensions(PluginContext ctx, ExtensionRegistry registry) {
        // No-op.
    }

    /** {@inheritDoc} */
    @Nullable @Override public <T> T createComponent(PluginContext ctx, Class<T> cls) {
        return null;
    }

    /** {@inheritDoc} */
    @Override public void start(PluginContext ctx) throws IgniteCheckedException {
        // No-op.
    }

    /** {@inheritDoc} */
    @Override public void stop(boolean cancel) throws IgniteCheckedException {
        // No-op.
    }

    /** {@inheritDoc} */
    @Override public void onIgniteStart() throws IgniteCheckedException {
        // No-op.
    }

    /** {@inheritDoc} */
    @Override public void onIgniteStop(boolean cancel) {
        // No-op.
    }

    /** {@inheritDoc} */
    @Nullable @Override public Serializable provideDiscoveryData(UUID nodeId) {
        return null;
    }

    /** {@inheritDoc} */
    @Override public void receiveDiscoveryData(UUID nodeId, Serializable data) {
        // No-op.
    }

    /** {@inheritDoc} */
    @Override public void validateNewNode(ClusterNode node) throws PluginValidationException {
        // No-op.
    }

    /**
     * Test plugin.
     */
    public static class PlatformTestPlugin implements IgnitePlatformPlugin {
        /** Plugin target. */
        // TODO
        private final PlatformTarget target = new PlatformTestPluginTarget(null);

        /** {@inheritDoc} */
        @Override public PlatformTarget platformTarget() {
            return target;
        }
    }

    /**
     * Test target.
     */
    public static class PlatformTestPluginTarget extends PlatformAbstractTarget {
        /** */
        private static final int OP_READ_WRITE = 1;

        /** */
        private static final int OP_ERROR = 2;

        /** */
        private static final int OP_INVOKE_CALLBACK = 3;

        /** */
        private static final int OP_GET_NAME = 4;

        /** */
        private static final int OP_GET_CHILD = 5;

        /** */
        private static final int OP_GET_OBJECT_NAME = 6;

        /** */
        private static final int OP_GET_NODE_ID = 7;

        /** */
        private static final int OP_GET_CALLBACK_RESPONSE = 8;

        /** */
        private String name = "root";

        /** */
        private String callbackResponse = "";

        /**
         * Constructor.
         *
         * @param platformCtx Context.
         */
        protected PlatformTestPluginTarget(PlatformContext platformCtx) {
            super(platformCtx);
        }

        @Override
        public PlatformTarget processInObjectStreamOutObjectStream(int type, @Nullable PlatformTarget arg,
                                                                   BinaryRawReaderEx reader, BinaryRawWriterEx writer)
                throws IgniteCheckedException {
            switch (type) {
                case OP_READ_WRITE: {
                    Object data = reader.readObject();

                    writer.writeObject(data);

                    return null;
                }

                case OP_ERROR: {
                    String text = reader.readString();

                    throw new IgniteException(text);
                }

                case OP_INVOKE_CALLBACK: {
                    final String val = reader.readString();

                    // TODO
                    /*
                    callbackResponse = context.callback(new CI1<BinaryRawWriter>() {
                        @Override
                        public void apply(BinaryRawWriter writer) {
                            writer.writeString(val);
                        }
                    }, new C1<BinaryRawReader, String>() {
                        @Override
                        public String apply(BinaryRawReader reader) {
                            return reader.readString();
                        }
                    });*/

                    return null;
                }

                case OP_GET_NAME: {
                    writer.writeString(name);

                    return null;
                }

                case OP_GET_CHILD: {
                    // TODO
                    final PlatformTestPluginTarget child = new PlatformTestPluginTarget(null);

                    child.name = reader.readString();

                    return child;
                }

                case OP_GET_OBJECT_NAME: {
                    PlatformTestPluginTarget target = (PlatformTestPluginTarget) arg;

                    writer.writeString(target.name);
                }

                case OP_GET_NODE_ID: {
                    // TODO
                    /**
                    writer.writeUuid(context.ignite().cluster().localNode().id());
                     */

                    return null;
                }

                case OP_GET_CALLBACK_RESPONSE: {
                    writer.writeString(callbackResponse);

                    return null;
                }
            }
            return super.processInObjectStreamOutObjectStream(type, arg, reader, writer);
        }

    }
}
