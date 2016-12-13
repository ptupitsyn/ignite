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

        /// <summary>
        /// Tests numeric types.
        /// </summary>
        [Test]
        public void TestNumericTypes()
        {
            // 0 for null
            Assert.AreEqual(0, JavaHashCode.GetHashCode(null));

            // byte
            CheckHashCode((byte) 0);
            CheckHashCode(byte.MinValue);
            CheckHashCode(byte.MaxValue);
            CheckHashCode(byte.MinValue/2);
            CheckHashCode(byte.MaxValue/2);

            // sbyte
            CheckHashCode((sbyte) 0);
            CheckHashCode(sbyte.MinValue);
            CheckHashCode(sbyte.MaxValue);
            CheckHashCode(sbyte.MinValue/2);
            CheckHashCode(sbyte.MaxValue/2);

            // short
            CheckHashCode((short) 0);
            CheckHashCode(short.MinValue);
            CheckHashCode(short.MaxValue);
            CheckHashCode(short.MinValue/2);
            CheckHashCode(short.MaxValue/2);

            // ushort
            CheckHashCode((ushort) 0);
            CheckHashCode(ushort.MinValue);
            CheckHashCode(ushort.MaxValue);
            CheckHashCode(ushort.MinValue/2);
            CheckHashCode(ushort.MaxValue/2);

            // int
            CheckHashCode(0);
            CheckHashCode(int.MinValue);
            CheckHashCode(int.MaxValue);
            CheckHashCode(int.MinValue/2);
            CheckHashCode(int.MaxValue/2);

            // uint
            CheckHashCode((uint) 0);
            CheckHashCode(uint.MinValue);
            CheckHashCode(uint.MaxValue);
            CheckHashCode(uint.MinValue/2);
            CheckHashCode(uint.MaxValue/2);

            // long
            CheckHashCode((long) 0);
            CheckHashCode(long.MinValue);
            CheckHashCode(long.MaxValue);
            CheckHashCode(long.MinValue/2);
            CheckHashCode(long.MaxValue/2);

            // ulong
            CheckHashCode((ulong) 0);
            CheckHashCode(ulong.MinValue);
            CheckHashCode(ulong.MaxValue);
            CheckHashCode(ulong.MinValue/2);
            CheckHashCode(ulong.MaxValue/2);

            // float
            CheckHashCode((float) 0);
            CheckHashCode((float) Math.PI);
            CheckHashCode(-(float) Math.PI);
            CheckHashCode(float.MinValue);
            CheckHashCode(float.MaxValue);
            CheckHashCode(float.MinValue/2);
            CheckHashCode(float.MaxValue/2);

            // double
            CheckHashCode((double) 0);
            CheckHashCode(Math.PI);
            CheckHashCode(-Math.PI);
            CheckHashCode(double.MinValue);
            CheckHashCode(double.MaxValue);
            CheckHashCode(double.MinValue/2);
            CheckHashCode(double.MaxValue/2);

            // decimal
            // TODO: Test with a range of values, decimal serialization is complicated
            CheckHashCode((decimal) 0);
            CheckHashCode((decimal) Math.PI);
            CheckHashCode((decimal) -Math.PI);
            CheckHashCode((decimal) int.MaxValue);
            CheckHashCode((decimal) -int.MaxValue);
            CheckHashCode((decimal) long.MaxValue);
            CheckHashCode((decimal) -long.MaxValue);
            CheckHashCode(decimal.MinValue);
            CheckHashCode(decimal.MaxValue);
            CheckHashCode(decimal.MinValue/2);
            CheckHashCode(decimal.MaxValue/2);
        }

        /// <summary>
        /// Tests other types.
        /// </summary>
        [Test]
        public void TestOtherTypes()
        {
            // bool
            CheckHashCode(true);
            CheckHashCode(false);

            // char
            CheckHashCode('a');
            CheckHashCode('b');
            CheckHashCode('\n');

            foreach (var ch in BinarySelfTest.SpecialStrings.SelectMany(x => x))
                CheckHashCode(ch);

            // string
            CheckHashCode("");
            CheckHashCode("foo");
            CheckHashCode("Foo");

            foreach (var str in BinarySelfTest.SpecialStrings)
                CheckHashCode(str);

            // Guid
            CheckHashCode(Guid.Empty);

            // DateTime
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
