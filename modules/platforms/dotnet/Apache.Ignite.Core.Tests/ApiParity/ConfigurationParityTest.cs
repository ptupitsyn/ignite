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
    using Apache.Ignite.Core.Impl.Common;
    using NUnit.Framework;

    /// <summary>
    /// Tests that .NET has all properties from Java configuration APIs.
    /// </summary>
    public class ConfigurationParityTest
    {
        private static readonly Regex JavaPropertyRegex = 
            new Regex("public [^=]+ (.+?)\\(\\)\\s+{", RegexOptions.Compiled);

        private static IEnumerable<string> GetJavaProperties(string path)
        {
            var text = File.ReadAllText(path);

            var exclude = new[] {"toString", "writeReplace"};

            return JavaPropertyRegex.Matches(text)
                .OfType<Match>()
                .Select(m => m.Groups[1].Value.Replace("get", ""))
                .Except(exclude);
        }

        /// <summary>
        /// Tests the cache configuration parity.
        /// </summary>
        [Test]
        public void TestCacheConfiguration()
        {
            var path = Path.Combine(IgniteHome.Resolve(null),
                @"modules\core\src\main\java\org\apache\ignite\configuration\CacheConfiguration.java");

            Assert.IsTrue(File.Exists(path));

            foreach (var javaProperty in GetJavaProperties(path))
            {
                Console.WriteLine(javaProperty);
            }
        }
    }
}
