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
    using System.Collections.Generic;
    using Apache.Ignite.BenchmarkDotNet.Models;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Memory;
    using global::BenchmarkDotNet.Attributes;

    public class BasicPropertyTypesReadBenchmark
    {
        private Marshaller _marsh;

        private PlatformMemoryManager _memMgr;

        private IPlatformMemory _mem;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _marsh = new Marshaller(new BinaryConfiguration(
                typeof (BasicPropertyTypes),
                typeof (BasicPropertyTypesBinarizable)));

            _memMgr = new PlatformMemoryManager(1024);
            _mem = _memMgr.Allocate();

            var stream = _mem.GetStream();

            //_marsh.StartMarshal(stream).Write(_model);
            _marsh.StartMarshal(stream).Write(_address);

            stream.SynchronizeOutput();

        }

    }
}
