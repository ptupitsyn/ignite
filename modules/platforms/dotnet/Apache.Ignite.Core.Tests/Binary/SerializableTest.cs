﻿/*
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
            var marsh = GetMarshaller();

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

            Assert.AreEqual(val.Bool, res.Bool);
            Assert.AreEqual(val.Bool, bin.GetField<bool>("bool"));

            Assert.AreEqual(val.Bools, res.Bools);
            Assert.AreEqual(val.Bools, bin.GetField<bool[]>("bools"));

            Assert.AreEqual(val.Char, res.Char);
            Assert.AreEqual(val.Char, bin.GetField<char>("char"));

            Assert.AreEqual(val.Chars, res.Chars);
            Assert.AreEqual(val.Chars, bin.GetField<char[]>("chars"));

            Assert.AreEqual(val.Short, res.Short);
            Assert.AreEqual(val.Short, bin.GetField<short>("short"));

            Assert.AreEqual(val.Shorts, res.Shorts);
            Assert.AreEqual(val.Shorts, bin.GetField<short[]>("shorts"));

            Assert.AreEqual(val.Ushort, res.Ushort);
            Assert.AreEqual(val.Ushort, bin.GetField<ushort>("ushort"));

            Assert.AreEqual(val.Ushorts, res.Ushorts);
            Assert.AreEqual(val.Ushorts, bin.GetField<ushort[]>("ushorts"));

            Assert.AreEqual(val.Int, res.Int);
            Assert.AreEqual(val.Int, bin.GetField<int>("int"));

            Assert.AreEqual(val.Ints, res.Ints);
            Assert.AreEqual(val.Ints, bin.GetField<int[]>("ints"));

            Assert.AreEqual(val.Uint, res.Uint);
            Assert.AreEqual(val.Uint, bin.GetField<uint>("uint"));

            Assert.AreEqual(val.Uints, res.Uints);
            Assert.AreEqual(val.Uints, bin.GetField<uint[]>("uints"));

            Assert.AreEqual(val.Long, res.Long);
            Assert.AreEqual(val.Long, bin.GetField<long>("long"));

            Assert.AreEqual(val.Longs, res.Longs);
            Assert.AreEqual(val.Longs, bin.GetField<long[]>("longs"));

            Assert.AreEqual(val.Ulong, res.Ulong);
            Assert.AreEqual(val.Ulong, bin.GetField<ulong>("ulong"));

            Assert.AreEqual(val.Ulongs, res.Ulongs);
            Assert.AreEqual(val.Ulongs, bin.GetField<ulong[]>("ulongs"));

            Assert.AreEqual(val.Float, res.Float);
            Assert.AreEqual(val.Float, bin.GetField<float>("float"));

            Assert.AreEqual(val.Floats, res.Floats);
            Assert.AreEqual(val.Floats, bin.GetField<float[]>("floats"));

            Assert.AreEqual(val.Double, res.Double);
            Assert.AreEqual(val.Double, bin.GetField<double>("double"));

            Assert.AreEqual(val.Doubles, res.Doubles);
            Assert.AreEqual(val.Doubles, bin.GetField<double[]>("doubles"));

            Assert.AreEqual(val.Decimal, res.Decimal);
            Assert.AreEqual(val.Decimal, bin.GetField<decimal>("decimal"));

            Assert.AreEqual(val.Decimals, res.Decimals);
            Assert.AreEqual(val.Decimals, bin.GetField<decimal[]>("decimals"));

            Assert.AreEqual(val.Guid, res.Guid);
            Assert.AreEqual(val.Guid, bin.GetField<Guid>("guid"));

            Assert.AreEqual(val.Guids, res.Guids);
            Assert.AreEqual(val.Guids, bin.GetField<Guid[]>("guids"));

            Assert.AreEqual(val.DateTime, res.DateTime);
            Assert.AreEqual(val.DateTime, bin.GetField<DateTime>("datetime"));

            Assert.AreEqual(val.DateTimes, res.DateTimes);
            Assert.AreEqual(val.DateTimes, bin.GetField<DateTime[]>("datetimes"));

            Assert.AreEqual(val.String, res.String);
            Assert.AreEqual(val.String, bin.GetField<string>("string"));

            Assert.AreEqual(val.Strings, res.Strings);
            Assert.AreEqual(val.Strings, bin.GetField<string[]>("strings"));
        }

        /// <summary>
        /// Gets the marshaller.
        /// </summary>
        private static Marshaller GetMarshaller()
        {
            return new Marshaller(null) {CompactFooter = false};
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

                Bool = info.GetBoolean("bool");
                Bools = (bool[]) info.GetValue("bools", typeof(bool[]));

                Char = info.GetChar("char");
                Chars = (char[]) info.GetValue("chars", typeof(char[]));

                Short = info.GetInt16("short");
                Shorts = (short[]) info.GetValue("shorts", typeof(short[]));

                Ushort = info.GetUInt16("ushort");
                Ushorts = (ushort[]) info.GetValue("ushorts", typeof(ushort[]));

                Int = info.GetInt32("int");
                Ints = (int[]) info.GetValue("ints", typeof(int[]));

                Uint = info.GetUInt32("uint");
                Uints = (uint[]) info.GetValue("uints", typeof(uint[]));

                Long = info.GetInt64("long");
                Longs = (long[]) info.GetValue("longs", typeof(long[]));

                Ulong = info.GetUInt64("ulong");
                Ulongs = (ulong[]) info.GetValue("ulongs", typeof(ulong[]));

                Float = info.GetSingle("float");
                Floats = (float[]) info.GetValue("floats", typeof(float[]));

                Double = info.GetDouble("double");
                Doubles = (double[]) info.GetValue("doubles", typeof(double[]));

                Decimal = info.GetDecimal("decimal");
                Decimals = (decimal[]) info.GetValue("decimals", typeof(decimal[]));

                Guid = (Guid) info.GetValue("guid", typeof(Guid));
                Guids = (Guid[]) info.GetValue("guids", typeof(Guid[]));

                DateTime = info.GetDateTime("datetime");
                DateTimes = (DateTime[]) info.GetValue("datetimes", typeof(DateTime[]));

                String = info.GetString("string");
                Strings = (string[]) info.GetValue("strings", typeof(string[]));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                GetObjectDataCalled = true;

                info.AddValue("byte", Byte);
                info.AddValue("bytes", Bytes);
                info.AddValue("bool", Bool);
                info.AddValue("bools", Bools);
                info.AddValue("char", Char);
                info.AddValue("chars", Chars);
                info.AddValue("short", Short);
                info.AddValue("shorts", Shorts);
                info.AddValue("ushort", Ushort);
                info.AddValue("ushorts", Ushorts);
                info.AddValue("int", Int);
                info.AddValue("ints", Ints);
                info.AddValue("uint", Uint);
                info.AddValue("uints", Uints);
                info.AddValue("long", Long);
                info.AddValue("longs", Longs);
                info.AddValue("ulong", Ulong);
                info.AddValue("ulongs", Ulongs);
                info.AddValue("float", Float);
                info.AddValue("floats", Floats);
                info.AddValue("double", Double);
                info.AddValue("doubles", Doubles);
                info.AddValue("decimal", Decimal);
                info.AddValue("decimals", Decimals);
                info.AddValue("guid", Guid);
                info.AddValue("guids", Guids);
                info.AddValue("datetime", DateTime);
                info.AddValue("datetimes", DateTimes);
                info.AddValue("string", String);
                info.AddValue("strings", Strings);
            }
        }
    }
}
