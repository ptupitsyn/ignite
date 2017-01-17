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

namespace Apache.Ignite.Core.Tests.Binary
{
    using System;
    using System.Linq;
    using Apache.Ignite.Core.Impl.Binary;
    using NUnit.Framework;

    /// <summary>
    /// Separate class for string hash code tests so we can enable V2 string serialization.
    /// </summary>
    public class JavaStringHashCodeTest
    {
        /// <summary>
        /// Tests strings hash codes.
        /// </summary>
        [Test]
        public void TestStrings()
        {
            if (!"true".Equals(Environment.GetEnvironmentVariable(
                BinaryUtils.IgniteBinaryMarshallerUseStringSerializationVer2), StringComparison.OrdinalIgnoreCase))
            {
                // Run "TestOldMode" in a separate process with changed setting.
                Environment.SetEnvironmentVariable(BinaryUtils.IgniteBinaryMarshallerUseStringSerializationVer2,
                    "true");

                TestUtils.RunTestInNewProcess(GetType().FullName, "TestStrings");
            }
            else
            {
                using (Ignition.Start(TestUtils.GetTestConfiguration(false)))
                {
                    CheckStrings();
                }
            }
        }


        /// <summary>
        /// Checks the strings.
        /// </summary>
        private static void CheckStrings()
        {
            JavaHashCodeTest.CheckHashCode("");
            JavaHashCodeTest.CheckHashCode("foo");
            JavaHashCodeTest.CheckHashCode("Foo");
            JavaHashCodeTest.CheckHashCode(new string(Enumerable.Range(1, 255).Select(x => (char) x).ToArray()));
            JavaHashCodeTest.CheckHashCode(new string(new[] {(char) 0xD800}));

            foreach (var str in BinarySelfTest.SpecialStrings)
                JavaHashCodeTest.CheckHashCode(str);
        }
    }
}