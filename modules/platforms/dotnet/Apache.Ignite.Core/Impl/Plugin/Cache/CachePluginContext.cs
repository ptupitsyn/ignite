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

namespace Apache.Ignite.Core.Impl.Plugin.Cache
{
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Plugin.Cache;
    /// <summary>
    /// Cache plugin context.
    /// </summary>
    internal class CachePluginContext : ICachePluginContext
    {
        /** */
        private readonly IgniteConfiguration _igniteConfiguration;
        
        /** */
        private readonly CacheConfiguration _cacheConfiguration;
        
        /** */
        private readonly ICachePluginConfiguration _cachePluginConfiguration;

        /** */
        private readonly IIgnite _ignite;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachePluginContext"/> class.
        /// </summary>
        public CachePluginContext(IgniteConfiguration igniteConfiguration, CacheConfiguration cacheConfiguration, 
            ICachePluginConfiguration cachePluginConfiguration, IIgnite ignite)
        {
            _igniteConfiguration = igniteConfiguration;
            _cacheConfiguration = cacheConfiguration;
            _cachePluginConfiguration = cachePluginConfiguration;
            _ignite = ignite;
        }

        /** <inheritdoc /> */
        public IgniteConfiguration IgniteConfiguration
        {
            get { return _igniteConfiguration; }
        }

        /** <inheritdoc /> */
        public CacheConfiguration CacheConfiguration
        {
            get { return _cacheConfiguration; }
        }

        /** <inheritdoc /> */
        public ICachePluginConfiguration CachePluginConfiguration
        {
            get { return _cachePluginConfiguration; }
        }

        /** <inheritdoc /> */
        public IIgnite Ignite
        {
            get { return _ignite; }
        }
    }
}
