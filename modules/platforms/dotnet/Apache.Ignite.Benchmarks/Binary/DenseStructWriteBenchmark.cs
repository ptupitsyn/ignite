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

namespace Apache.Ignite.Benchmarks.Binary
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Memory;

    /// <summary>
    /// Binary write benchmark.
    /// </summary>
    internal class DenseStructWriteBenchmark : BenchmarkBase
    {
        /** Marshaller. */
        private readonly Marshaller _marsh;

        /** Memory manager. */
        private readonly PlatformMemoryManager _memMgr = new PlatformMemoryManager(1024);

        /** Pre-allocated model. */
        private readonly DenseStruct _model = new DenseStruct(167);

        /// <summary>
        /// Initializes a new instance of the <see cref="BinarizableWriteBenchmark"/> class.
        /// </summary>
        public DenseStructWriteBenchmark()
        {
            _marsh = new Marshaller(new BinaryConfiguration
            {
                TypeConfigurations = new List<BinaryTypeConfiguration>
                {
                    new BinaryTypeConfiguration(typeof (DenseStruct)) { Serializer = new BinaryReflectiveSerializer {RawMode = true} }
                }
            });
        }

        /// <summary>
        /// Populate descriptors.
        /// </summary>
        /// <param name="descs">Descriptors.</param>
        protected override void GetDescriptors(ICollection<BenchmarkOperationDescriptor> descs)
        {
            descs.Add(BenchmarkOperationDescriptor.Create("WriteStruct", WriteStruct, 1));
        }

        /// <summary>
        /// Write address.
        /// </summary>
        /// <param name="state">State.</param>
        private void WriteStruct(BenchmarkState state)
        {
            var mem = _memMgr.Allocate();

            try
            {
                var stream = mem.GetStream();

                var writer = _marsh.StartMarshal(stream);

                writer.Write(_model);
            }
            finally
            {
                mem.Release();
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DenseStruct
        {
            public readonly byte _byte;
            public readonly short _short;
            public readonly int _int;
            public readonly long _long;
            public readonly float _float;
            public readonly double _double;

            public DenseStruct(byte b)
            {
                _byte = b;
                _short = b;
                _int = b;
                _long = b;
                _float = b;
                _double = b;
            }
        }
    }
}
