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

// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Apache.Ignite.Core.Tests.Cache.Query
{
    using System;
    using System.Linq;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Common;
    using Apache.Ignite.Core.Tests.Binary;
    using NUnit.Framework;

    /// <summary>
    /// Tests Data Manipulation Language queries.
    /// </summary>
    public class CacheDmlQueriesTest
    {
        /// <summary>
        /// Sets up test fixture.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            var cfg = new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                BinaryConfiguration = new BinaryConfiguration
                {
                    NameMapper = GetNameMapper()
                }
            };

            Ignition.Start(cfg);
        }

        /// <summary>
        /// Gets the name mapper.
        /// </summary>
        protected virtual IBinaryNameMapper GetNameMapper()
        {
            return BinaryBasicNameMapper.FullNameInstance;
        }

        /// <summary>
        /// Tears down test fixture.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            Ignition.StopAll(true);
        }

        /// <summary>
        /// Tests primitive key.
        /// </summary>
        [Test]
        public void TestPrimitiveKey()
        {
            var cfg = new CacheConfiguration("primitive_key", new QueryEntity(typeof(int), typeof(Foo)));
            var cache = Ignition.GetIgnite().CreateCache<int, Foo>(cfg);

            // Test insert.
            var res = cache.QueryFields(new SqlFieldsQuery("insert into foo(_key, id, name) " +
                                                           "values (?, ?, ?), (?, ?, ?)",
                1, 2, "John", 3, 4, "Mary")).GetAll();

            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(1, res[0].Count);
            Assert.AreEqual(2, res[0][0]);  // 2 affected rows

            var foos = cache.OrderBy(x => x.Key).ToArray();

            Assert.AreEqual(2, foos.Length);

            Assert.AreEqual(1, foos[0].Key);
            Assert.AreEqual(2, foos[0].Value.Id);
            Assert.AreEqual("John", foos[0].Value.Name);

            Assert.AreEqual(3, foos[1].Key);
            Assert.AreEqual(4, foos[1].Value.Id);
            Assert.AreEqual("Mary", foos[1].Value.Name);

            // Test key existence.
            Assert.IsTrue(cache.ContainsKey(1));
            Assert.IsTrue(cache.ContainsKey(3));
        }

        /// <summary>
        /// Tests all primitive key types.
        /// </summary>
        [Test]
        public void TestPrimitiveKeyAllTypes()
        {
            TestKey(byte.MinValue, byte.MaxValue);
            TestKey(sbyte.MinValue, sbyte.MaxValue);
        }

        /// <summary>
        /// Tests the key.
        /// </summary>
        private static void TestKey<T>(params T[] vals)
        {
            var cfg = new CacheConfiguration("primitive_key_dotnet_" + typeof(T), 
                new QueryEntity(typeof(T), typeof(string)));
            var cache = Ignition.GetIgnite().CreateCache<T, string>(cfg);

            foreach (var val in vals)
            {
                var res = cache.QueryFields(new SqlFieldsQuery(
                    "insert into string(_key, _val) values (?, ?)", val, val.ToString())).GetAll();

                Assert.AreEqual(1, res.Count);
                Assert.AreEqual(1, res[0].Count);
                Assert.AreEqual(1, res[0][0]);

                Assert.AreEqual(val.ToString(), cache[val]);
            }
        }

        /// <summary>
        /// Tests composite key (which requires QueryField.IsKeyField).
        /// </summary>
        [Test]
        public void TestCompositeKey()
        {
            var cfg = new CacheConfiguration("composite_key_arr", new QueryEntity(typeof(Key), typeof(Foo)));
            var cache = Ignition.GetIgnite().CreateCache<Key, Foo>(cfg);

            // Test insert.
            var res = cache.QueryFields(new SqlFieldsQuery("insert into foo(hi, lo, id, name) " +
                                               "values (-1, 65500, 3, 'John'), (255, 128, 6, 'Mary')")).GetAll();

            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(1, res[0].Count);
            Assert.AreEqual(2, res[0][0]);  // 2 affected rows

            var foos = cache.OrderByDescending(x => x.Key.Lo).ToArray();

            Assert.AreEqual(2, foos.Length);

            Assert.AreEqual(-1, foos[0].Key.Hi);
            Assert.AreEqual(65500, foos[0].Key.Lo);
            Assert.AreEqual(3, foos[0].Value.Id);
            Assert.AreEqual("John", foos[0].Value.Name);

            Assert.AreEqual(255, foos[1].Key.Hi);
            Assert.AreEqual(128, foos[1].Key.Lo);
            Assert.AreEqual(6, foos[1].Value.Id);
            Assert.AreEqual("Mary", foos[1].Value.Name);

            // Existence tests check that hash codes are consistent.
            var binary = cache.Ignite.GetBinary();
            var binCache = cache.WithKeepBinary<IBinaryObject, IBinaryObject>();

            Assert.IsTrue(cache.ContainsKey(new Key(65500, -1)));
            Assert.IsTrue(cache.ContainsKey(foos[0].Key));
            Assert.IsTrue(binCache.ContainsKey(
                binary.GetBuilder(typeof(Key)).SetField("hi", -1).SetField("lo", 65500).Build()));
            Assert.IsTrue(binCache.ContainsKey(  // Fields are sorted.
                binary.GetBuilder(typeof(Key)).SetField("lo", 65500).SetField("hi", -1).Build()));

            Assert.IsTrue(cache.ContainsKey(new Key(128, 255)));
            Assert.IsTrue(cache.ContainsKey(foos[1].Key));
            Assert.IsTrue(binCache.ContainsKey(
                binary.GetBuilder(typeof(Key)).SetField("hi", 255).SetField("lo", 128).Build()));
            Assert.IsTrue(binCache.ContainsKey(  // Fields are sorted.
                binary.GetBuilder(typeof(Key)).SetField("lo", 128).SetField("hi", 255).Build()));
        }

        /// <summary>
        /// Tests composite key (which requires QueryField.IsKeyField).
        /// </summary>
        [Test]
        public void TestCompositeKey2()
        {
            var cfg = new CacheConfiguration("composite_key_fld", new QueryEntity(typeof(Key2), typeof(Foo)));
            var cache = Ignition.GetIgnite().CreateCache<Key2, Foo>(cfg);

            // Test insert.
            var res = cache.QueryFields(new SqlFieldsQuery("insert into foo(hi, lo, str, id, name) " +
                                               "values (1, 2, 'Фу', 3, 'John'), (4, 5, 'Бар', 6, 'Mary')")).GetAll();

            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(1, res[0].Count);
            Assert.AreEqual(2, res[0][0]);  // 2 affected rows

            var foos = cache.OrderBy(x => x.Key.Lo).ToArray();

            Assert.AreEqual(2, foos.Length);

            Assert.AreEqual(1, foos[0].Key.Hi);
            Assert.AreEqual(2, foos[0].Key.Lo);
            Assert.AreEqual("Фу", foos[0].Key.Str);
            Assert.AreEqual(3, foos[0].Value.Id);
            Assert.AreEqual("John", foos[0].Value.Name);

            Assert.AreEqual(4, foos[1].Key.Hi);
            Assert.AreEqual(5, foos[1].Key.Lo);
            Assert.AreEqual("Бар", foos[1].Key.Str);
            Assert.AreEqual(6, foos[1].Value.Id);
            Assert.AreEqual("Mary", foos[1].Value.Name);

            // Existence tests check that hash codes are consistent.
            Assert.IsTrue(cache.ContainsKey(new Key2(2, 1, "Фу")));
            Assert.IsTrue(cache.ContainsKey(foos[0].Key));

            Assert.IsTrue(cache.ContainsKey(new Key2(5, 4, "Бар")));
            Assert.IsTrue(cache.ContainsKey(foos[1].Key));
        }

        /// <summary>
        /// Tests the composite key without IsKeyField configuration.
        /// </summary>
        [Test]
        public void TestInvalidCompositeKey()
        {
            var cfg = new CacheConfiguration("invalid_composite_key", new QueryEntity
            {
                KeyTypeName = "Key",
                ValueTypeName = "Foo",
                Fields = new[]
                {
                    new QueryField("Lo", typeof(int)),
                    new QueryField("Hi", typeof(int)),
                    new QueryField("Id", typeof(int)),
                    new QueryField("Name", typeof(string))
                }
            });

            var cache = Ignition.GetIgnite().CreateCache<Key, Foo>(cfg);

            var ex = Assert.Throws<IgniteException>(
                () => cache.QueryFields(new SqlFieldsQuery("insert into foo(lo, hi, id, name) " +
                                                           "values (1, 2, 3, 'John'), (4, 5, 6, 'Mary')")));

            Assert.AreEqual("Ownership flag not set for binary property. Have you set 'keyFields' " +
                            "property of QueryEntity in programmatic or XML configuration?", ex.Message);
        }

        /// <summary>
        /// Tests DML with pure binary cache mode, without classes.
        /// </summary>
        [Test]
        public void TestBinaryMode()
        {
            var cfg = new CacheConfiguration("binary_only", new QueryEntity
            {
                KeyTypeName = "CarKey",
                ValueTypeName = "Car",
                Fields = new[]
                {
                    new QueryField("VIN", typeof(string)) {IsKeyField = true},
                    new QueryField("Id", typeof(int)) {IsKeyField = true},
                    new QueryField("Make", typeof(string)),
                    new QueryField("Year", typeof(int))
                }
            });

            var cache = Ignition.GetIgnite().CreateCache<object, object>(cfg)
                .WithKeepBinary<IBinaryObject, IBinaryObject>();

            var res = cache.QueryFields(new SqlFieldsQuery("insert into car(VIN, Id, Make, Year) " +
                                                           "values ('DLRDMC', 88, 'DeLorean', 1982)")).GetAll();

            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(1, res[0].Count);
            Assert.AreEqual(1, res[0][0]);

            var car = cache.Single();
            Assert.AreEqual("CarKey", car.Key.GetBinaryType().TypeName);
            Assert.AreEqual("Car", car.Value.GetBinaryType().TypeName);
        }

        /// <summary>
        /// Tests the composite key with fields of all data types.
        /// </summary>
        [Test]
        public void TestCompositeKeyAllDataTypes()
        {
            var cfg = new CacheConfiguration("composite_key_all", new QueryEntity(typeof(KeyAll), typeof(string)));
            var cache = Ignition.GetIgnite().CreateCache<KeyAll, string>(cfg);

            var key = new KeyAll
            {
                Byte = byte.MaxValue,
                SByte = sbyte.MaxValue,
                Short = short.MaxValue,
                UShort = ushort.MaxValue,
                Int = int.MaxValue,
                UInt = uint.MaxValue,
                Long = long.MaxValue,
                ULong = ulong.MaxValue,
                Float = float.MaxValue,
                Double = double.MaxValue,
                Decimal = decimal.MaxValue,
                Guid = Guid.NewGuid(),
                String = BinarySelfTest.SpecialStrings.First(),
                Key = new Key(255, 65555),

                Bytes = new[] {byte.MinValue, byte.MaxValue},
                SBytes = new[] {sbyte.MinValue, sbyte.MaxValue},
                Shorts = new[] {short.MinValue, short.MaxValue},
                UShorts = new[] {ushort.MinValue, ushort.MaxValue},
                Ints = new[] {int.MinValue, int.MaxValue},
                UInts = new[] {uint.MinValue, uint.MaxValue},
                Longs = new[] {long.MinValue, long.MaxValue},
                ULongs = new[] {ulong.MinValue, ulong.MaxValue},
                Floats = new[] {float.MinValue, float.MaxValue},
                Doubles = new[] {double.MinValue, double.MaxValue},
                Decimals = new[] {decimal.MinValue, decimal.MaxValue},
                Guids = new[] {Guid.NewGuid()},
                Strings = BinarySelfTest.SpecialStrings,
                Keys = new[] {new Key(255, 65555), new Key(-1, 1)}
            };

            // Test insert.
            var res = cache.QueryFields(new SqlFieldsQuery(
                    "insert into string(byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal, " +
                    "guid, string, key, bytes, sbytes, shorts, ushorts, ints, uints, longs, ulongs, floats, " +
                    "doubles, decimals, guids, strings, keys, _val) " +
                    "values (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                    key.Byte, key.SByte, key.Short, key.UShort, key.Int, key.UInt, key.Long, key.ULong, key.Float,
                    key.Double, key.Decimal, key.Guid, key.String, key.Key, key.Bytes, key.SBytes, key.Shorts,
                    key.UShorts, key.Ints, key.UInts, key.Longs, key.ULongs, key.Floats, key.Doubles, key.Decimals,
                    key.Guids, key.Strings, key.Keys, "VALUE"))
                .GetAll();

            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(1, res[0].Count);
            Assert.AreEqual(1, res[0][0]);
        }

        /// <summary>
        /// Key.
        /// </summary>
        private struct Key
        {
            public Key(int lo, int hi) : this()
            {
                Lo = lo;
                Hi = hi;
            }

            [QuerySqlField] public int Lo { get; private set; }
            [QuerySqlField] public int Hi { get; private set; }
        }

        /// <summary>
        /// Key.
        /// </summary>
        private struct Key2
        {
            public Key2(int lo, int hi, string str) : this()
            {
                Lo = lo;
                Hi = hi;
                Str = str;
            }

            [QuerySqlField] public int Lo { get; private set; }
            [QuerySqlField] public int Hi { get; private set; }
            [QuerySqlField] public string Str { get; private set; }
        }

        /// <summary>
        /// Value.
        /// </summary>
        private class Foo
        {
            [QuerySqlField] public int Id { get; set; }
            [QuerySqlField] public string Name { get; set; }
        }

        /// <summary>
        /// Key with all kinds of fields.
        /// </summary>
        private class KeyAll
        {
            [QuerySqlField] public byte Byte { get; set; }
            [QuerySqlField] public sbyte SByte { get; set; }
            [QuerySqlField] public short Short { get; set; }
            [QuerySqlField] public ushort UShort { get; set; }
            [QuerySqlField] public int Int { get; set; }
            [QuerySqlField] public uint UInt { get; set; }
            [QuerySqlField] public long Long { get; set; }
            [QuerySqlField] public ulong ULong { get; set; }
            [QuerySqlField] public float Float { get; set; }
            [QuerySqlField] public double Double { get; set; }
            [QuerySqlField] public decimal Decimal { get; set; }
            [QuerySqlField] public Guid Guid { get; set; }
            [QuerySqlField] public string String { get; set; }
            [QuerySqlField] public Key Key { get; set; }
            
            [QuerySqlField] public byte[] Bytes { get; set; }
            [QuerySqlField] public sbyte[] SBytes { get; set; }
            [QuerySqlField] public short[] Shorts { get; set; }
            [QuerySqlField] public ushort[] UShorts { get; set; }
            [QuerySqlField] public int[] Ints { get; set; }
            [QuerySqlField] public uint[] UInts { get; set; }
            [QuerySqlField] public long[] Longs { get; set; }
            [QuerySqlField] public ulong[] ULongs { get; set; }
            [QuerySqlField] public float[] Floats { get; set; }
            [QuerySqlField] public double[] Doubles { get; set; }
            [QuerySqlField] public decimal[] Decimals { get; set; }
            [QuerySqlField] public Guid[] Guids { get; set; }
            [QuerySqlField] public string[] Strings { get; set; }
            [QuerySqlField] public Key[] Keys { get; set; }
        }
    }
}
