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

namespace Apache.Ignite.Core.Cache.Configuration
{
    /// <summary>
    /// Defines custom data region configuration for Apache Ignite page memory
    /// (see <see cref="DataStorageConfiguration"/>). 
    /// <para />
    /// For each configured data region Apache Ignite instantiates respective memory regions with different
    /// parameters like maximum size, eviction policy, swapping options, etc.
    /// An Apache Ignite cache can be mapped to a particular region using
    /// <see cref="CacheConfiguration.DataRegionName"/> method.
    /// </summary>
    public class DataRegionConfiguration
    {
        // TODO
    }

    /// <summary>
    /// 
    /// </summary>
    public class DataStorageConfiguration
    {
        // TODO
    }
}
