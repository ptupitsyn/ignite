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

namespace Apache.Ignite.BenchmarkDotNet.Models
{
    using System;

    /// <summary>
    /// Benchmark model: properties of all basic types
    /// </summary>
    public class BasicPropertyTypes
    {
        public byte Byte { get; set; } = 1;

        public byte[] ByteArray { get; set; } = {1, 2, 3};

        public char Char { get; set; } = 'c';

        public char[] CharArray { get; set; } = {'a', 'b'};

        public short Short { get; set; } = 3;

        public short[] ShortArray { get; set; } = {4, 5, 6, 7};

        public int Int { get; set; } = 10;

        public int[] IntArray { get; set; } = {10, 11, 12};

        public long Long { get; set; } = 13;

        public long[] LongArray { get; set; } = {14, 15, 16};

        public bool Boolean { get; set; } = true;

        public bool[] BooleanArray { get; set; } = {true, false};

        public float Float { get; set; } = 1.2f;

        public float[] FloatArray { get; set; } = {2.5f, 6.8f};

        public double Double { get; set; } = 34.56;

        public double[] DoubleArray { get; set; } = {0, -1.1, 2.2};

        public decimal? Decimal { get; set; } = 33.66m;

        public decimal?[] DecimalArray { get; set; } = {-6.6m, 9.9m, 0};

        public string String { get; set; } = "Foo bar";

        public string[] StringArray { get; set; } = {"foo", "bar", "baz"};

        public Guid? Guid { get; set; } = System.Guid.NewGuid();

        public Guid?[] GuidArray { get; set; } = {System.Guid.NewGuid(), System.Guid.NewGuid()};
    }
}
