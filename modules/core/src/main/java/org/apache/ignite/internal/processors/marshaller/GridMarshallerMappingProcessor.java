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

package org.apache.ignite.internal.processors.marshaller;

import java.util.Map;
import java.util.UUID;
import java.util.concurrent.ConcurrentMap;
import org.apache.ignite.IgniteCheckedException;
import org.apache.ignite.cluster.ClusterNode;
import org.apache.ignite.internal.GridKernalContext;
import org.apache.ignite.internal.MarshallerContextImpl;
import org.apache.ignite.internal.managers.discovery.CustomEventListener;
import org.apache.ignite.internal.managers.discovery.GridDiscoveryManager;
import org.apache.ignite.internal.processors.GridProcessorAdapter;
import org.apache.ignite.internal.processors.affinity.AffinityTopologyVersion;
import org.apache.ignite.spi.discovery.tcp.internal.DiscoveryDataContainer;
import org.apache.ignite.spi.discovery.tcp.internal.DiscoveryDataContainer.GridDiscoveryData;
import org.jetbrains.annotations.Nullable;

import static org.apache.ignite.internal.GridComponent.DiscoveryDataExchangeType.MARSHALLER_PROC;

/**
 * Processor responsible for managing custom {@link org.apache.ignite.internal.managers.discovery.DiscoveryCustomMessage} events for exchanging marshalling mappings between nodes in grid.
 *
 * In particular it processes two flows:
 * <ul>
 *     <li>
 *         Some node, server or client, wants to add new mapping for some class.
 *         In that case a pair of {@link MappingProposedMessage} and {@link MappingAcceptedMessage} events is used.
 *     </li>
 *     <li>
 *         As discovery events are delivered to clients asynchronously, client node may not have some mapping when server nodes in the grid are already allowed to use the mapping.
 *         In that situation client sends a {@link MissingMappingRequestMessage} request and processor handles it as well as {@link MissingMappingResponseMessage} message.
 *     </li>
 * </ul>
 */
public class GridMarshallerMappingProcessor extends GridProcessorAdapter {

    private final MarshallerContextImpl marshallerContext;

    /**
     * @param ctx Kernal context.
     */
    public GridMarshallerMappingProcessor(GridKernalContext ctx) {
        super(ctx);

        marshallerContext = ctx.marshallerContext();
    }

    @Override
    public void start() throws IgniteCheckedException {
        GridDiscoveryManager discoMgr = ctx.discovery();

        marshallerContext.onMarshallerProcessorStarted(ctx);

        discoMgr.setCustomEventListener(MappingProposedMessage.class, new MarshallerMappingExchangeListener());

        discoMgr.setCustomEventListener(MappingAcceptedMessage.class, new MappingAcceptedListener());

        discoMgr.setCustomEventListener(MissingMappingRequestMessage.class, new MissingMappingRequestListener());

        discoMgr.setCustomEventListener(MissingMappingResponseMessage.class, new MissingMappingResponseListener());
    }

    private final class MarshallerMappingExchangeListener implements CustomEventListener<MappingProposedMessage> {

        @Override
        public void onCustomEvent(AffinityTopologyVersion topVer, ClusterNode snd, MappingProposedMessage msg) {
            if (!ctx.isStopping())
                if (!msg.isInConflict()) {
                    MarshallerMappingItem item = msg.getMappingItem();
                    if (!marshallerContext.onMappingProposed(item))
                        msg.setInConflict(true);
                }
        }
    }

    private final class MappingAcceptedListener implements CustomEventListener<MappingAcceptedMessage> {
        @Override
        public void onCustomEvent(AffinityTopologyVersion topVer, ClusterNode snd, MappingAcceptedMessage msg) {
            if (!ctx.isStopping())
                marshallerContext.onMappingAccepted(msg.getMappingItem());
        }
    }

    private final class MissingMappingRequestListener implements CustomEventListener<MissingMappingRequestMessage> {
        @Override
        public void onCustomEvent(AffinityTopologyVersion topVer, ClusterNode snd, MissingMappingRequestMessage msg) {
            if (!ctx.isStopping() && !ctx.clientNode())
                if (!msg.isResolved()) {
                    boolean resolved = marshallerContext.resolveMissedMapping(msg.getMappingItem());
                    msg.setResolved(resolved);
                }
        }
    }

    private final class MissingMappingResponseListener implements CustomEventListener<MissingMappingResponseMessage> {
        @Override
        public void onCustomEvent(AffinityTopologyVersion topVer, ClusterNode snd, MissingMappingResponseMessage msg) {
            UUID locNodeId = ctx.localNodeId();
            if (!ctx.isStopping() && ctx.clientNode() && locNodeId.equals(msg.getOrigNodeId()))
                marshallerContext.onMissedMappingResolved(msg.getMarshallerMappingItem());
        }
    }

    /** {@inheritDoc} */
    @Override
    public void collectDiscoveryData(DiscoveryDataContainer dataContainer) {
        if (!ctx.localNodeId().equals(dataContainer.getJoiningNodeId()))
            if (!dataContainer.isCommonDataCollectedFor(MARSHALLER_PROC.ordinal()))
                dataContainer.addGridCommonData(MARSHALLER_PROC.ordinal(), marshallerContext.getCachedMappings());
    }

    /** {@inheritDoc} */
    @Override
    public void onGridDataReceived(GridDiscoveryData data) {
        Map<Byte, ConcurrentMap<Integer, MappedName>> marshallerMappings = (Map<Byte, ConcurrentMap<Integer, MappedName>>) data.commonData();

        if (marshallerMappings != null)
            for (Map.Entry<Byte, ConcurrentMap<Integer, MappedName>> e : marshallerMappings.entrySet())
                marshallerContext.applyPlatformMapping(e.getKey(), e.getValue());
    }

    @Nullable
    @Override
    public DiscoveryDataExchangeType discoveryDataType() {
        return MARSHALLER_PROC;
    }
}
