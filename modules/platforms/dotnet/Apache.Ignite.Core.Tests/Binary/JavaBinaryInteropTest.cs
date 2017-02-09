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
    using NUnit.Framework;

    /// <summary>
    /// Tests binary type interoperability between .NET and Java code.
    /// </summary>
    public class JavaBinaryInteropTest
    {
        /// <summary>
        /// Tests that all kinds of values from .NET can be handled properly on Java side.
        /// </summary>
        [Test]
        public void TestValueRoundtrip()
        {
            using (var ignite = Ignition.Start(TestUtils.GetTestConfiguration()))
            {
                var cache = ignite.CreateCache<int, object>("TestValueRoundtrip");

                // TODO
                // Basic types.

                // Basic type arrays.
                var guids = new[] {Guid.Empty, Guid.NewGuid()};
                cache[1] = guids;
                Assert.AreEqual(guids, cache[1]);

                // Custom types.
                cache[1] = new Foo {X = 10};
                Assert.AreEqual(10, ((Foo)cache[1]).X);
            }
        }

        /// <summary>
        /// Test custom class.
        /// </summary>
        private class Foo
        {
            public int X { get; set; }
        }
    }
}
