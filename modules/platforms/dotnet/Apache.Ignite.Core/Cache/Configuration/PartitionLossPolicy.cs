﻿/*
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

namespace Apache.Ignite.Core.Cache.Configuration
{
    /// <summary>
    /// Partition loss policy. Defines how cache will behave in a case when one or more partitions are
    /// lost because of a node(s) failure.
    /// <para />
    /// All *Safe policies prevent a user from interaction with partial data in lost partitions until {@link Ignite#resetLostPartitions(Collection)} method is called. <code>*_ALL</code> policies allow working with
* partial data in lost partitions.
* <p>
* <code>READ_ONLY_*</code> and<code> READ_WRITE_*</code> policies do not automatically change partition state
    * and thus do not change rebalancing assignments for such partitions.
    /// </summary>
    public enum PartitionLossPolicy
    {
    }
}
