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
    using System.Runtime.Serialization;
    using Apache.Ignite.Core.Impl.Binary;
    using NUnit.Framework;

    /// <summary>
    /// Tests [Serializable] mechanism handling.
    /// </summary>
    public class SerializableTest
    {
        // TODO: 
        // Attribute Callbacks
        // IDeserializationCallback
        // with ISerializable
        // without ISerializable
        // check built-in types: collections, delegates, types, memberInfos, etc


        /// <summary>
        /// Tests that primitive types can be serialized with ISerializable mechanism.
        /// </summary>
        [Test]
        public void TestPrimitives()
        {
            var marsh = new Marshaller(null);

            var val = new Primitives
            {
                Byte = 1
            };

            Assert.IsFalse(val.GetObjectDataCalled);
            Assert.IsFalse(val.SerializationCtorCalled);

            var res = marsh.Unmarshal<Primitives>(marsh.Marshal(val));

            Assert.IsTrue(val.GetObjectDataCalled);
            Assert.IsFalse(val.SerializationCtorCalled);

            Assert.IsFalse(res.GetObjectDataCalled);
            Assert.IsTrue(res.SerializationCtorCalled);

            Assert.AreEqual(val.Byte, res.Byte);
        }

        [Serializable]
        private class Primitives : ISerializable
        {
            public bool GetObjectDataCalled { get; private set; }
            public bool SerializationCtorCalled { get; private set; }

            public byte Byte { get; set; }

            public Primitives()
            {
                // No-op.
            }

            public Primitives(SerializationInfo info, StreamingContext context)
            {
                SerializationCtorCalled = true;

                Byte = info.GetByte("byte");
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                GetObjectDataCalled = true;

                info.AddValue("byte", Byte);
            }
        }
    }
}
