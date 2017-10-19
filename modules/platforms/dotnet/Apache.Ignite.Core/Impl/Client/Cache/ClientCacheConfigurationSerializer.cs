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

namespace Apache.Ignite.Core.Impl.Client.Cache
{
    using System.Diagnostics;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Binary.IO;

    /// <summary>
    /// Writes and reads <see cref="CacheConfiguration"/> for thin client mode.
    /// <para />
    /// Thin client supports a subset of <see cref="CacheConfiguration"/> properties, so
    /// <see cref="CacheConfiguration.Read"/> is not suitable.
    /// </summary>
    internal static class ClientCacheConfigurationSerializer
    {
        /// <summary>
        /// Writes the specified config.
        /// </summary>
        public static void Write(CacheConfiguration cfg, IBinaryStream stream)
        {
            Debug.Assert(cfg != null);
            Debug.Assert(stream != null);

            // Configuration should be written with a system marshaller.
            var w = BinaryUtils.Marshaller.StartMarshal(stream);

            w.WriteString(cfg.Name);
            // TODO
        }

        /// <summary>
        /// Reads the config.
        /// </summary>
        public static CacheConfiguration Read(IBinaryStream stream)
        {
            Debug.Assert(stream != null);

            var r = BinaryUtils.Marshaller.StartUnmarshal(stream);
         
            return new CacheConfiguration
            {
                Name = r.ReadString()

                // TODO
            };
        }
    }
}
