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

namespace Apache.Ignite.BenchmarkDotNet.Serialization
{
    using Apache.Ignite.BenchmarkDotNet.Models;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary;
    using global::BenchmarkDotNet.Attributes;

    /// <summary>
    /// Deserialization benchmark.
    ///
    /// With TypeCaster (.NET Core 3.1):
    /// |          Method |     Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    /// |---------------- |---------:|----------:|----------:|-------:|------:|------:|----------:|
    /// |            Read | 2.522 us | 0.0103 us | 0.0097 us | 0.5951 |     - |     - |   3.66 KB |
    /// | ReadBinarizable | 2.274 us | 0.0092 us | 0.0077 us | 0.6065 |     - |     - |   3.73 KB |
    /// </summary>
    [MemoryDiagnoser]
    public class BasicPropertyTypesReadBenchmark
    {
        private Marshaller _marsh;

        private byte[] _bytes;

        private byte[] _bytesBinarizable;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _marsh = new Marshaller(new BinaryConfiguration(
                typeof (BasicPropertyTypes),
                typeof (BasicPropertyTypesBinarizable)));

            _bytes = _marsh.Marshal(new BasicPropertyTypes());
            _bytesBinarizable = _marsh.Marshal(new BasicPropertyTypesBinarizable());
        }

        [Benchmark]
        public void Read()
        {
            _marsh.Unmarshal<BasicPropertyTypes>(_bytes);
        }

        [Benchmark]
        public void ReadBinarizable()
        {
            _marsh.Unmarshal<BasicPropertyTypesBinarizable>(_bytesBinarizable);
        }
    }
}
