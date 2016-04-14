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

package org.apache.ignite.internal.processors.platform.cache.query;

import org.apache.ignite.Ignite;
import org.apache.ignite.resources.IgniteInstanceResource;

/**
 * Continuous query filter factory deployed on remote nodes.
 */
public class PlatformContinuousQueryRemoteFilterFactory implements PlatformContinuousQueryFilterFactory {
    /** Native filter in serialized form. */
    private Object filter;

    /** Grid hosting the filter. */
    @IgniteInstanceResource
    private transient Ignite grid;

    /**
     * Constructor.
     *
     * @param filter Serialized native filter.
     */
    public PlatformContinuousQueryRemoteFilterFactory(Object filter) {
        assert filter != null;

        this.filter = filter;
    }

    /** {@inheritDoc} */
    @Override public PlatformContinuousQueryFilter create() {
        // TODO: Inject
        return new PlatformContinuousQueryRemoteFilter(filter);
    }
}
