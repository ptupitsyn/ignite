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
        /** */
        private const string JavaTask = "org.apache.ignite.platform.PlatformHashCodeTask";

        /// <summary>
        /// Fixture set up.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            Ignition.Start(TestUtils.GetTestConfiguration());
        }

        /// <summary>
        /// Fixture tear down.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            Ignition.StopAll(true);
        }

        [Test]
        public void TestBasicTypes()
        {
            // 0 for null
            Assert.AreEqual(0, JavaHashCode.GetHashCode(null));

            // byte
            CheckHashCode((byte) 0);
            CheckHashCode(byte.MinValue);
            CheckHashCode(byte.MaxValue);
            CheckHashCode(byte.MinValue / 2);
            CheckHashCode(byte.MaxValue / 2);

            // sbyte
            CheckHashCode((sbyte) 0);
            CheckHashCode(sbyte.MinValue);
            CheckHashCode(sbyte.MaxValue);
            CheckHashCode(sbyte.MinValue / 2);
            CheckHashCode(sbyte.MaxValue / 2);

            // short
            CheckHashCode((short) 0);
            CheckHashCode(short.MinValue);
            CheckHashCode(short.MaxValue);
            CheckHashCode(short.MinValue / 2);
            CheckHashCode(short.MaxValue / 2);

            // ushort
            CheckHashCode((ushort) 0);
            CheckHashCode(ushort.MinValue);
            CheckHashCode(ushort.MaxValue);
            CheckHashCode(ushort.MinValue / 2);
            CheckHashCode(ushort.MaxValue / 2);

            // int
            CheckHashCode(0);
            CheckHashCode(int.MinValue);
            CheckHashCode(int.MaxValue);
            CheckHashCode(int.MinValue / 2);
            CheckHashCode(int.MaxValue / 2);

            // uint
            CheckHashCode((uint) 0);
            CheckHashCode(uint.MinValue);
            CheckHashCode(uint.MaxValue);
            CheckHashCode(uint.MinValue / 2);
            CheckHashCode(uint.MaxValue / 2);

            // long
            CheckHashCode((long) 0);
            CheckHashCode(long.MinValue);
            CheckHashCode(long.MaxValue);
            CheckHashCode(long.MinValue / 2);
            CheckHashCode(long.MaxValue / 2);

            // ulong
            CheckHashCode((ulong) 0);
            CheckHashCode(ulong.MinValue);
            CheckHashCode(ulong.MaxValue);
            CheckHashCode(ulong.MinValue / 2);
            CheckHashCode(ulong.MaxValue / 2);
        }

        [Test]
        public void TestBinaryObjects()
        {
            // TODO: Test nested
        }

        /// <summary>
        /// Checks the hash code.
        /// </summary>
        private void CheckHashCode(object o)
        {
            Assert.AreEqual(GetJavaHashCode(o), JavaHashCode.GetHashCode(o));
        }

        /// <summary>
        /// Gets the Java hash code.
        /// </summary>
        private int GetJavaHashCode(object o)
        {
            return Ignition.GetIgnite().GetCompute().WithKeepBinary().ExecuteJavaTask<int>(JavaTask, o);
        }
    }
}
