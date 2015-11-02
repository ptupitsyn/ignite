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

namespace Apache.Ignite.Core.Impl.Memory
{
    using System;

    /// <summary>
    /// Platform unpooled memory chunk.
    /// </summary>
    internal struct PlatformUnpooledMemory : IPlatformMemory
    {
        private readonly long _memPtr;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="memPtr">Memory pointer.</param>
        public PlatformUnpooledMemory(long memPtr)
        {
            _memPtr = memPtr;
        }

        public long Pointer
        {
            get { return _memPtr; }
        }

        public long Data
        {
            get { return PlatformMemoryUtils.GetData(_memPtr); }
        }

        public int Capacity
        {
            get { return PlatformMemoryUtils.GetCapacity(_memPtr); }
        }

        public int Length
        {
            get { return PlatformMemoryUtils.GetLength(_memPtr); }
            set { PlatformMemoryUtils.SetLength(_memPtr, value); }
        }

        /** <inheritdoc /> */
        public void Reallocate(int cap)
        {
            // Try doubling capacity to avoid excessive allocations.
            int doubledCap = ((PlatformMemoryUtils.GetCapacity(Pointer) + 16) << 1) - 16;

            if (doubledCap > cap)
                cap = doubledCap;

            PlatformMemoryUtils.ReallocateUnpooled(Pointer, cap);
        }

        /** <inheritdoc /> */
        public void Release()
        {
            PlatformMemoryUtils.ReleaseUnpooled(Pointer);
        }

        public PlatformMemoryStream GetStream()
        {
            return BitConverter.IsLittleEndian ? new PlatformMemoryStream(this) : 
                new PlatformBigEndianMemoryStream(this);
        }
    }
}
