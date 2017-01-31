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
    using Apache.Ignite.Core.Binary;
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
                Byte = 1,
                Bytes = new byte[] {2, 3}
            };

            Assert.IsFalse(val.GetObjectDataCalled);
            Assert.IsFalse(val.SerializationCtorCalled);

            // Unmarshal in full and binary form.
            var bytes = marsh.Marshal(val);
            var res = marsh.Unmarshal<Primitives>(bytes);
            var bin = marsh.Unmarshal<IBinaryObject>(bytes, BinaryMode.ForceBinary);

            // Verify flags.
            Assert.IsTrue(val.GetObjectDataCalled);
            Assert.IsFalse(val.SerializationCtorCalled);

            Assert.IsFalse(res.GetObjectDataCalled);
            Assert.IsTrue(res.SerializationCtorCalled);

            // Verify values.
            Assert.AreEqual(val.Byte, res.Byte);
            Assert.AreEqual(val.Byte, bin.GetField<byte>("byte"));

            Assert.AreEqual(val.Bytes, res.Bytes);
            Assert.AreEqual(val.Bytes, bin.GetField<byte[]>("bytes"));
        }

        [Serializable]
        private class Primitives : ISerializable
        {
            public bool GetObjectDataCalled { get; private set; }
            public bool SerializationCtorCalled { get; private set; }

            public byte Byte { get; set; }
            public byte[] Bytes { get; set; }
            public bool Bool { get; set; }
            public bool[] Bools { get; set; }
            public char Char { get; set; }
            public char[] Chars { get; set; }
            public short Short { get; set; }
            public short[] Shorts { get; set; }
            public ushort Ushort { get; set; }
            public ushort[] Ushorts { get; set; }
            public int Int { get; set; }
            public int[] Ints { get; set; }
            public uint Uint { get; set; }
            public uint[] Uints { get; set; }
            public long Long { get; set; }
            public long[] Longs { get; set; }
            public ulong Ulong { get; set; }
            public ulong[] Ulongs { get; set; }
            public float Float { get; set; }
            public float[] Floats { get; set; }
            public double Double { get; set; }
            public double[] Doubles { get; set; }
            public decimal Decimal { get; set; }
            public decimal[] Decimals { get; set; }
            public Guid Guid { get; set; }
            public Guid[] Guids { get; set; }
            public DateTime DateTime { get; set; }
            public DateTime[] DateTimes { get; set; }
            public string String { get; set; }
            public string[] Strings { get; set; }

            public Primitives()
            {
                // No-op.
            }

            protected Primitives(SerializationInfo info, StreamingContext context)
            {
                SerializationCtorCalled = true;

                Byte = info.GetByte("byte");
                Bytes = (byte[]) info.GetValue("bytes", typeof(byte[]));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                GetObjectDataCalled = true;

                info.AddValue("byte", Byte);
                info.AddValue("bytes", Bytes);
            }
        }
    }
}
