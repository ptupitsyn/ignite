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
    using System.Runtime.InteropServices;

    /// <summary>
    /// Mutable struct that represents memory layout of the platform memory header.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct PlatformMemoryHeader
    {
        /** Flag: external. */
        public const int FlagExt = 0x1;

        /** Flag: pooled. */
        public const int FlagPooled = 0x2;

        /** Flag: whether this pooled memory chunk is acquired. */
        public const int FlagAcquired = 0x4;

        /// <summary>
        /// The pointer to the memory chunk.
        /// </summary>
        public long Pointer;

        /// <summary>
        /// The capacity.
        /// </summary>
        public int Capacity;

        /// <summary>
        /// The length.
        /// </summary>
        public int Length;

        /// <summary>
        /// The flags.
        /// </summary>
        public int Flags;

        /// <summary>
        /// Check whether flags denote that this memory chunk is external.
        /// </summary>
        /// <value><c>True</c> if owned by Java.</value>
        public bool IsExternal
        {
            get { return (Flags & FlagExt) != FlagExt; }
        }

        /// <summary>
        /// Check whether flags denote pooled memory chunk.
        /// </summary>
        /// <value><c>True</c> if pooled.</value>
        public bool IsPooled
        {
            get { return (Flags & FlagPooled) != 0; }
        }

        /// <summary>
        /// Check whether flags denote pooled and acquired memory chunk.
        /// </summary>
        /// <value><c>True</c> if acquired.</value>
        public bool IsAcquired
        {
            get { return (Flags & FlagAcquired) != 0; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformMemoryHeader"/> struct.
        /// </summary>
        /// <param name="pointer">Pointer.</param>
        /// <param name="capacity">Capacity.</param>
        /// <param name="length">Length.</param>
        /// <param name="flags">Flags.</param>
        public PlatformMemoryHeader(long pointer, int capacity, int length, int flags)
        {
            Pointer = pointer;
            Capacity = capacity;
            Length = length;
            Flags = flags;
        }
    }
}