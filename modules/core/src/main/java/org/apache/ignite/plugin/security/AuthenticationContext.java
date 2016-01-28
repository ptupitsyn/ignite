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

package org.apache.ignite.plugin.security;

import java.net.InetSocketAddress;
import java.util.Collections;
import java.util.Map;
import java.util.UUID;

/**
 * Authentication context.
 */
public class AuthenticationContext {
    /** Subject type. */
    private SecuritySubjectType subjType;

    /** Subject ID.w */
    private UUID subjId;

    /** Credentials. */
    private SecurityCredentials creds;

    /** Subject address. */
    private InetSocketAddress addr;

    /** */
    private Map<String, Object> nodeAttrs;

    /**
     * Gets subject type.
     *
     * @return Subject type.
     */
    public SecuritySubjectType subjectType() {
        return subjType;
    }

    /**
     * Sets subject type.
     *
     * @param subjType Subject type.
     */
    public void subjectType(SecuritySubjectType subjType) {
        this.subjType = subjType;
    }

    /**
     * Gets subject ID.
     *
     * @return Subject ID.
     */
    public UUID subjectId() {
        return subjId;
    }

    /**
     * Sets subject ID.
     *
     * @param subjId Subject ID.
     */
    public void subjectId(UUID subjId) {
        this.subjId = subjId;
    }

    /**
     * Gets security credentials.
     *
     * @return Security credentials.
     */
    public SecurityCredentials credentials() {
        return creds;
    }

    /**
     * Sets security credentials.
     *
     * @param creds Security credentials.
     */
    public void credentials(SecurityCredentials creds) {
        this.creds = creds;
    }

    /**
     * Gets subject network address.
     *
     * @return Subject network address.
     */
    public InetSocketAddress address() {
        return addr;
    }

    /**
     * Sets subject network address.
     *
     * @param addr Subject network address.
     */
    public void address(InetSocketAddress addr) {
        this.addr = addr;
    }

    /**
     * Gets node attributes.
     *
     * @return Node attributes or empty map for {@link SecuritySubjectType#REMOTE_CLIENT}.
     */
    public Map<String, Object> nodeAttributes() {
        return nodeAttrs != null ? nodeAttrs : Collections.<String, Object>emptyMap();
    }

    /**
     * Sets node attributes.
     *
     * @param nodeAttrs Node attributes.
     */
    public void nodeAttributes(Map<String, Object> nodeAttrs) {
        this.nodeAttrs = nodeAttrs;
    }
}