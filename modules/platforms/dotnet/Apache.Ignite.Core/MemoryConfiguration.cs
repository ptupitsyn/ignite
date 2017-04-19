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

namespace Apache.Ignite.Core
{
    /// <summary>
    /// A page memory configuration for an Apache Ignite node. The page memory is a manageable off-heap based
    /// memory architecture that divides all continuously allocated memory regions into pages of fixed size.
    /// An individual page can store one or many cache key-value entries that allows reusing the memory
    /// in the most efficient way and avoid memory fragmentation issues. 
    /// <para />
    /// By default, the page memory allocates a single continuous memory region. All the caches that
    /// will be configured in an application will be mapped to this memory region by default,
    /// thus, all the cache data will reside in that memory region.
    /// <para />
    /// If initial size of the default memory region doesn't satisfy requirements or it's
    /// required to have multiple memory regions with different properties
    /// then <see cref="MemoryPolicyConfiguration" /> can be used for both scenarios.
    /// For instance, using memory policies you can define memory regions of different maximum size,
    /// eviction policies, swapping options, etc. Once you define a new memory region you can bind
    /// particular Ignite caches to it. <para />
    /// To learn more about memory policies refer to <see cref="MemoryPolicyConfiguration" /> documentation.
    /// </summary>
    public class MemoryConfiguration
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class MemoryPolicyConfiguration
    {
        
    }
}
