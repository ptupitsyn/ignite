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

namespace Apache.Ignite.Core.Tests.ApiParity
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Impl.Common;
    using NUnit.Framework;

    /// <summary>
    /// Tests that .NET <see cref="CacheConfiguration"/> has all properties from Java configuration APIs.
    /// </summary>
    public class CacheConfigurationParityTest
    {
        /** Property regex. */
        private static readonly Regex JavaPropertyRegex = 
            new Regex("(@Deprecated)?\\s+public [^=^\r^\n]+ (\\w+)\\(\\) {", RegexOptions.Compiled);

        /** Known property name mappings. */
        private static readonly Dictionary<string, string> KnownMappings = new Dictionary<string, string>
        {
            {"isStoreKeepBinary", "KeepBinaryInStore"},
            {"Affinity", "AffinityFunction"},
            {"DefaultLockTimeout", "LockTimeout"}
        };

        /** Properties that are not needed on .NET side. */
        private static readonly HashSet<string> UnneededProperties = new HashSet<string>(new[]
        {
            // False matches.
            "toString",
            "writeReplace",
            "clearQueryEntities",
            
            // Java-specific.
            "CacheStoreSessionListenerFactories",
            "CacheEntryListenerConfigurations",
            "TopologyValidator",
            "SqlFunctionClasses",
            "Interceptor",
            "EvictionFilter",
            "IndexedTypes",

            // Deprecated, but not marked so.
            "AffinityMapper"
        });

        /** Properties that are missing on .NET side. */
        private static readonly HashSet<string> MissingProperties = new HashSet<string>(new[]
        {
            "NodeFilter",  // IGNITE-2890

            "IsOnheapCacheEnabled",
            "StoreConcurrentLoadAllThreshold",
            "isOnheapCacheEnabled",
            "RebalanceOrder",
            "RebalanceBatchesPrefetchCount",
            "MaxQueryIteratorsCount",
            "QueryDetailMetricsSize",
            "SqlSchema",
            "QueryParallelism",
            "KeyConfiguration"
        });

        /// <summary>
        /// Tests the cache configuration parity.
        /// </summary>
        [Test]
        public void TestCacheConfiguration()
        {
            var path = Path.Combine(IgniteHome.Resolve(null),
                @"modules\core\src\main\java\org\apache\ignite\configuration\CacheConfiguration.java");

            Assert.IsTrue(File.Exists(path));

            var dotNetProperties = typeof(CacheConfiguration).GetProperties()
                .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

            var javaProperties = GetJavaProperties(path);

            var missingProperties = javaProperties
                .Where(jp => !GetNameVariants(jp).Any(dotNetProperties.ContainsKey))
                .ToArray();

            if (missingProperties.Length > 0)
            {
                Assert.Fail("{0} CacheConfiguration properties are missing in .NET: \n{1}",
                    missingProperties.Length, string.Join("\n", missingProperties));
            }
        }

        /// <summary>
        /// Gets the java properties from file.
        /// </summary>
        private static IEnumerable<string> GetJavaProperties(string path)
        {
            var text = File.ReadAllText(path);

            return JavaPropertyRegex.Matches(text)
                .OfType<Match>()
                .Where(m => m.Groups[1].Value == string.Empty)
                .Select(m => m.Groups[2].Value.Replace("get", ""))
                .Where(x => !x.Contains(" void "))
                .Except(UnneededProperties)
                .Except(MissingProperties);
        }

        /// <summary>
        /// Gets the name variants for a property.
        /// </summary>
        private static IEnumerable<string> GetNameVariants(string javaPropertyName)
        {
            yield return javaPropertyName;

            if (javaPropertyName.StartsWith("is"))
            {
                yield return javaPropertyName.Substring(2);
            }

            string map;

            if (KnownMappings.TryGetValue(javaPropertyName, out map))
            {
                yield return map;
            }
        }
    }
}
