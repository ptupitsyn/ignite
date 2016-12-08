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
    using Apache.Ignite.Core.Impl.Binary;
    using NUnit.Framework;

    /// <summary>
    /// Tests for <see cref="JavaHashCode"/>.
    /// </summary>
    public class JavaHashCodeTest
    {
        [Test]
        public void TestBasicTypes()
        {
            // TODO: ExecuteJavaTask and test against real implementations.
            Assert.AreEqual(0, JavaHashCode.GetHashCode(null));

            Assert.AreEqual(25, JavaHashCode.GetHashCode(25));
            Assert.AreEqual(25, JavaHashCode.GetHashCode((long?)25));

            Assert.AreEqual(1231, JavaHashCode.GetHashCode(true));
            Assert.AreEqual(1237, JavaHashCode.GetHashCode(false));
            Assert.AreEqual(1231, JavaHashCode.GetHashCode((bool?)true));
            Assert.AreEqual(1237, JavaHashCode.GetHashCode((bool?)false));
        }

        [Test]
        public void TestBinaryObjects()
        {
            // TODO: Test nested
        }
    }
}
