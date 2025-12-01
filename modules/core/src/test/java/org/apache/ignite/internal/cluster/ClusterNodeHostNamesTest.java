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

package org.apache.ignite.internal.cluster;

import org.apache.ignite.configuration.IgniteConfiguration;
import org.apache.ignite.testframework.junits.IgniteTestResources;
import org.apache.ignite.testframework.junits.common.GridCommonAbstractTest;
import org.junit.Test;

import java.util.Collection;

@SuppressWarnings("resource")
public class ClusterNodeHostNamesTest extends GridCommonAbstractTest {
    /** {@inheritDoc} */
    @Override protected void beforeTestsStarted() throws Exception {
        super.beforeTestsStarted();

        startGrid(0);
    }

    /** {@inheritDoc} */
    @Override protected IgniteConfiguration getConfiguration(String igniteInstanceName,
                                                             IgniteTestResources rsrcs) throws Exception {
        IgniteConfiguration cfg = super.getConfiguration(igniteInstanceName, rsrcs);

        cfg.setLocalHost("LoCalHost");

        return cfg;
    }


    @Test
    public void testHostNames() throws Exception {
        Collection<String> hostNames = grid(0).cluster().localNode().hostNames();
        assertNotNull(hostNames);
        assertFalse(hostNames.isEmpty());
    }
}
