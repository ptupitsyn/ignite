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
            new Regex("public [^=]+ (.+?)\\(\\)\\s+{", RegexOptions.Compiled);

        /** Known property name mappings. */
        private static readonly Dictionary<string, string> KnownMappings = new Dictionary<string, string>
        {
            {"", ""}
        };

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
                Assert.Fail("CacheConfiguration properties are missing in .NET: \n" +
                            string.Join("\n", missingProperties));
            }
        }

        private static IEnumerable<string> GetJavaProperties(string path)
        {
            var text = File.ReadAllText(path);

            var exclude = new[] { "toString", "writeReplace" };

            return JavaPropertyRegex.Matches(text)
                .OfType<Match>()
                .Select(m => m.Groups[1].Value.Replace("get", ""))
                .Where(x => !x.Contains(" void "))
                .Except(exclude);
        }

        private static IEnumerable<string> GetNameVariants(string javaPropertyName)
        {
            yield return javaPropertyName;

            if (javaPropertyName.StartsWith("is"))
            {
                yield return javaPropertyName.Substring(2);
            }
        }
    }
}
