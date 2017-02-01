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

namespace Apache.Ignite.Core.Tests.Binary.Serializable
{
    using System;
    using NUnit.Framework;

    /// <summary>
    /// Tests delegate serialization.
    /// </summary>
    public class DelegatesTest
    {
        /** Test int value. */
        private static int _int;

        /// <summary>
        /// Tests that delegates can be serialized.
        /// </summary>
        [Test]
        public void TestAction()
        {
            // Action with captured variable.
            var val = new PrimitivesTest.Primitives {Int = 135};

            Action act = () => {
                val.Int++;
                _int = val.Int;
            };

            var res = PrimitivesTest.SerializeDeserialize(act);
            Assert.AreEqual(act.Method, res.Method);
            Assert.AreNotEqual(act.Target, res.Target);

            res();
            Assert.AreEqual(135, val.Int);   // Captured variable is deserialized to a new instance.
            Assert.AreEqual(136, _int);

            // Action with arguments.
            Action<PrimitivesTest.Primitives, int> act1 = (p, i) => { p.Int = i; };

            var res1 = PrimitivesTest.SerializeDeserialize(act1);
            Assert.AreEqual(act1.Method, res1.Method);

            res1(val, 33);
            Assert.AreEqual(33, val.Int);
        }

        /// <summary>
        /// Tests the recursive function.
        /// </summary>
        [Test]
        public void TestRecursiveFunc()
        {
            // TODO: Fibonacci
        }
    }
}
