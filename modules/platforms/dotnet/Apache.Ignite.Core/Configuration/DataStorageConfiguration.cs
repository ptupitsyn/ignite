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

namespace Apache.Ignite.Core.Configuration
{
    /// <summary>
    /// Data storage configuration for Ignite page memory.
    /// <para />
    /// The page memory is a manageable off-heap based memory architecture that divides all expandable data
    /// regions into pages of fixed size. An individual page can store one or many cache key-value entries
    /// that allows reusing the memory in the most efficient way and avoid memory fragmentation issues.
    /// <para />
    /// By default, the page memory allocates a single expandable data region. All the caches that will be
    /// configured in an application will be mapped to this data region by default, thus, all the cache data
    /// will reside in that data region.
    /// </summary>
    public class DataStorageConfiguration
    {
        // TODO
    }
}