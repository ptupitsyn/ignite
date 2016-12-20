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

namespace Apache.Ignite.Core.Tests.Plugin.Cache
{
    using System;
    using System.IO;
    using Apache.Ignite.Core.Plugin.Cache;

    /// <summary>
    /// Test cache plugin.
    /// </summary>
    public class CachePlugin : ICachePluginProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CachePlugin"/> class.
        /// </summary>
        /// <param name="pluginContext">The plugin context.</param>
        public CachePlugin(ICachePluginContext pluginContext)
        {
            Context = pluginContext;
        }

        /** <inheritdoc /> */
        public void Start()
        {
            Started = true;
        }

        /** <inheritdoc /> */
        public void Stop(bool cancel)
        {
            Stopped = cancel;
        }

        /** <inheritdoc /> */
        public void OnIgniteStart()
        {
            IgniteStarted = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CachePlugin"/> is started.
        /// </summary>
        public bool Started { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CachePlugin"/> is started.
        /// </summary>
        public bool IgniteStarted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CachePlugin"/> is stopped.
        /// </summary>
        public bool? Stopped { get; set; }

        /// <summary>
        /// Gets the context.
        /// </summary>
        public ICachePluginContext Context { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether error should be thrown from provider methods.
        /// </summary>
        public bool ThrowError { get; set; }

        /// <summary>
        /// Throws an error when <see cref="ThrowError"/> is <c>true</c>.
        /// </summary>
        private void Throw()
        {
            if (ThrowError)
                throw new IOException("Failure in cache plugin provider");
        }
    }

    [Serializable]
    public class CachePluginConfiguration : ICachePluginConfiguration
    {
        public ICachePluginProvider CreateProvider(ICachePluginContext pluginContext)
        {
            return new CachePlugin(pluginContext);
        }
    }
}