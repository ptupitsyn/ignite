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
        public byte Byte { get; set; }
        public byte[] ByteArray { get; set; }
        public char Char { get; set; }
        public char[] CharArray { get; set; }
        public short Short { get; set; }
        public short[] ShortArray { get; set; }
        public int Int { get; set; }
        public int[] IntArray { get; set; }
        public long Long { get; set; }
        public long[] LongArray { get; set; }
        public bool Boolean { get; set; }
        public bool[] BooleanArray { get; set; }
        public float Float { get; set; }
        public float[] FloatArray { get; set; }
        public double Double { get; set; }
        public double[] DoubleArray { get; set; }
        public decimal? Decimal { get; set; }
        public decimal?[] DecimalArray { get; set; }
        public DateTime? Date { get; set; }
        public DateTime?[] DateArray { get; set; }
        public string String { get; set; }
        public string[] StringArray { get; set; }
        public Guid? Guid { get; set; }
        public Guid?[] GuidArray { get; set; }
    }
}
