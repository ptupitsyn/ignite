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

namespace Apache.Ignite.EntityFramework
{
    using System.Configuration;
    using System.Data;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Common;
    using Apache.Ignite.Core.Impl.Common;
    using Apache.Ignite.EntityFramework.Impl;

    /// <summary>
    /// <see cref="DbConfiguration"/> implementation that uses Ignite as a second-level cache 
    /// for Entity Framework queries.
    /// </summary>
    public class IgniteDbConfiguration : DbConfiguration
    {
        /// <summary>
        /// The configuration section name to be used when starting Ignite.
        /// </summary>
        private const string ConfigurationSectionName = "igniteConfiguration";

        /// <summary>
        /// The default cache name to be used for cached EF data.
        /// </summary>
        public const string DefaultCacheNamePrefix = "entityFrameworkQueryCache";

        /// <summary>
        /// Suffix for the meta cache name.
        /// </summary>
        private const string MetaCacheSuffix = "_metadata";

        /// <summary>
        /// Suffix for the data cache name.
        /// </summary>
        private const string DataCacheSuffix = "_data";

        /// <summary>
        /// Initializes a new instance of the <see cref="IgniteDbConfiguration"/> class.
        /// <para />
        /// This constructor uses default Ignite instance (with null <see cref="IgniteConfiguration.GridName"/>) 
        /// and a cache with <see cref="DefaultCacheNamePrefix"/> name.
        /// <para />
        /// Ignite instance will be started automatically, if it is not started yet.
        /// <para /> 
        /// <see cref="IgniteConfigurationSection"/> with name 
        /// <see cref="ConfigurationSectionName"/> will be picked up when starting Ignite, if present.
        /// </summary>
        public IgniteDbConfiguration() 
            : this(GetConfiguration(ConfigurationSectionName, false), 
                  GetDefaultMetaCacheConfiguration(DefaultCacheNamePrefix), 
                  GetDefaultDataCacheConfiguration(DefaultCacheNamePrefix), null)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IgniteDbConfiguration" /> class.
        /// </summary>
        /// <param name="configurationSectionName">Name of the configuration section.</param>
        /// <param name="cacheNamePrefix">The cache name prefix for Data and Metadata caches.</param>
        /// <param name="policy">The caching policy. Null for default <see cref="DbCachingPolicy" />.</param>
        public IgniteDbConfiguration(string configurationSectionName, string cacheNamePrefix, IDbCachingPolicy policy)
            : this(configurationSectionName,
                GetDefaultMetaCacheConfiguration(cacheNamePrefix),
                GetDefaultDataCacheConfiguration(cacheNamePrefix), policy)

        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IgniteDbConfiguration"/> class.
        /// </summary>
        /// <param name="configurationSectionName">Name of the configuration section.</param>
        /// <param name="metaCacheConfiguration">
        /// Configuration of the metadata cache which holds entity set information. Null for default configuration.
        /// <para />
        /// This cache holds small amount of data, but should not lose entries. At least one backup recommended.
        /// </param>
        /// <param name="dataCacheConfiguration">
        /// Configuration of the data cache which holds query results. Null for default configuration.
        /// <para />
        /// This cache tolerates lost data and can have no backups.
        /// </param>
        /// <param name="policy">The caching policy. Null for default <see cref="DbCachingPolicy"/>.</param>
        public IgniteDbConfiguration(string configurationSectionName, CacheConfiguration metaCacheConfiguration,
            CacheConfiguration dataCacheConfiguration, IDbCachingPolicy policy)
            : this(GetConfiguration(configurationSectionName, true), 
                  metaCacheConfiguration, dataCacheConfiguration, policy)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IgniteDbConfiguration" /> class.
        /// </summary>
        /// <param name="igniteConfiguration">The ignite configuration to use for starting Ignite instance.</param>
        /// <param name="metaCacheConfiguration">
        /// Configuration of the metadata cache which holds entity set information. Null for default configuration. 
        /// <para />
        /// This cache holds small amount of data, but should not lose entries. At least one backup recommended.
        /// </param>
        /// <param name="dataCacheConfiguration">
        /// Configuration of the data cache which holds query results. Null for default configuration.
        /// <para />
        /// This cache tolerates lost data and can have no backups.
        /// </param>
        /// <param name="policy">The caching policy. Null for default <see cref="DbCachingPolicy"/>.</param>
        public IgniteDbConfiguration(IgniteConfiguration igniteConfiguration,
            CacheConfiguration metaCacheConfiguration, CacheConfiguration dataCacheConfiguration,
            IDbCachingPolicy policy)
            : this(GetOrStartIgnite(igniteConfiguration), metaCacheConfiguration, dataCacheConfiguration, policy)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IgniteDbConfiguration" /> class.
        /// </summary>
        /// <param name="ignite">The ignite instance to use.</param>
        /// <param name="metaCacheConfiguration">
        /// Configuration of the metadata cache which holds entity set information. Null for default configuration. 
        /// <para />
        /// This cache holds small amount of data, but should not lose entries. At least one backup recommended.
        /// </param>
        /// <param name="dataCacheConfiguration">
        /// Configuration of the data cache which holds query results. Null for default configuration.
        /// <para />
        /// This cache tolerates lost data and can have no backups.
        /// </param>
        /// <param name="policy">The caching policy. Null for default <see cref="DbCachingPolicy" />.</param>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", 
            Justification = "Validation is present")]
        public IgniteDbConfiguration(IIgnite ignite, CacheConfiguration metaCacheConfiguration,
            CacheConfiguration dataCacheConfiguration, IDbCachingPolicy policy)
        {
            IgniteArgumentCheck.NotNull(ignite, "ignite");

            metaCacheConfiguration = metaCacheConfiguration ?? GetDefaultMetaCacheConfiguration();
            dataCacheConfiguration = dataCacheConfiguration ?? GetDefaultDataCacheConfiguration();

            var efCache = new DbCache(ignite, metaCacheConfiguration, dataCacheConfiguration);

            AddInterceptor(new DbTransactionInterceptor());

            // SetProviderServices is not suitable. We should replace whatever provider there is with our proxy.
            Loaded += (sender, args) => args.ReplaceService<DbProviderServices>(
                (services, a) => new DbProviderServicesProxy(services, policy, efCache));
        }

        /// <summary>
        /// Gets the Ignite instance.
        /// </summary>
        private static IIgnite GetOrStartIgnite(IgniteConfiguration cfg)
        {
            cfg = cfg ?? new IgniteConfiguration();

            return Ignition.TryGetIgnite(cfg.GridName) ?? Ignition.Start(cfg);
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        private static IgniteConfiguration GetConfiguration(string sectionName, bool throwIfAbsent)
        {
            IgniteArgumentCheck.NotNull(sectionName, "sectionName");

            var section = ConfigurationManager.GetSection(sectionName) as IgniteConfigurationSection;

            if (section != null)
                return section.IgniteConfiguration;

            if (!throwIfAbsent)
                return null;

            throw new IgniteException(string.Format(CultureInfo.InvariantCulture,
                "Failed to initialize {0}. Could not find {1} with name {2} in application configuration.",
                typeof (IgniteDbConfiguration), typeof (IgniteConfigurationSection), sectionName));
        }

        /// <summary>
        /// Gets the default meta cache configuration.
        /// </summary>
        private static CacheConfiguration GetDefaultMetaCacheConfiguration(string namePrefix = null)
        {
            return new CacheConfiguration((namePrefix ?? DefaultCacheNamePrefix) + MetaCacheSuffix)
            {
                Backups = 1,
                // TODO: Enforce on user caches. Necessary for proper entry processor updates.
                AtomicityMode = CacheAtomicityMode.Transactional
            };
        }

        /// <summary>
        /// Gets the default data cache configuration.
        /// </summary>
        private static CacheConfiguration GetDefaultDataCacheConfiguration(string namePrefix = null)
        {
            return new CacheConfiguration((namePrefix ?? DefaultCacheNamePrefix) + DataCacheSuffix);
        }
    }
}