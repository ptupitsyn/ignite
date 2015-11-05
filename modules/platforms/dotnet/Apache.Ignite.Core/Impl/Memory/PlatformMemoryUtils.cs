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

namespace Apache.Ignite.Core.Impl.Memory
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Utility methods for platform memory management.
    /// </summary>
    [CLSCompliant(false)]
    public static unsafe class PlatformMemoryUtils
    {
        #region CONSTANTS

        /** Memory chunk header length. */
        private const int MemHdrLen = 20;

        /** Pooled items count. */
        internal const int PoolSize = 3;

        /** Header length. */
        private const int PoolHdrLen = MemHdrLen * PoolSize;

        /** Offset: capacity. */
        private const int MemHdrOffCap = 8;

        /** Offset: length. */
        private const int MemHdrOffLen = 12;

        /** Offset: flags. */
        private const int MemHdrOffFlags = 16;

        /** Flag: external. */
        private const int FlagExt = 0x1;

        /** Flag: pooled. */
        private const int FlagPooled = 0x2;

        /** Flag: whether this pooled memory chunk is acquired. */
        private const int FlagAcquired = 0x4;

        #endregion

        #region COMMON

        /// <summary>
        /// Gets data pointer for the given memory chunk.
        /// </summary>
        /// <param name="memPtr">Memory pointer.</param>
        /// <returns>Data pointer.</returns>
        public static long GetData(long memPtr)
        {
            return *((long*)memPtr);
        }

        /// <summary>
        /// Gets capacity for the given memory chunk.
        /// </summary>
        /// <param name="memPtr">Memory pointer.</param>
        /// <returns>CalculateCapacity.</returns>
        public static int GetCapacity(long memPtr) 
        {
            return *((int*)(memPtr + MemHdrOffCap));
        }

        /// <summary>
        /// Gets length for the given memory chunk.
        /// </summary>
        /// <param name="memPtr">Memory pointer.</param>
        /// <returns>Length.</returns>
        public static int GetLength(long memPtr) 
        {
            return *((int*)(memPtr + MemHdrOffLen));
        }

        /// <summary>
        /// Sets length for the given memory chunk.
        /// </summary>
        /// <param name="memPtr">Memory pointer.</param>
        /// <param name="len">Length.</param>
        public static void SetLength(long memPtr, int len) 
        {
            *((int*)(memPtr + MemHdrOffLen)) = len;
        }

        /// <summary>
        /// Gets flags for the given memory chunk.
        /// </summary>
        /// <param name="memPtr">Memory pointer.</param>
        /// <returns>Flags.</returns>
        public static int GetFlags(long memPtr) 
        {
            return *((int*)(memPtr + MemHdrOffFlags));
        }

        /// <summary>
        /// Check whether flags denote that this memory chunk is external.
        /// </summary>
        /// <param name="flags">Flags.</param>
        /// <returns><c>True</c> if owned by Java.</returns>
        public static bool IsExternal(int flags) 
        {
            return (flags & FlagExt) != FlagExt;
        }

        /// <summary>
        /// Check whether flags denote pooled memory chunk.
        /// </summary>
        /// <param name="flags">Flags.</param>
        /// <returns><c>True</c> if pooled.</returns>
        public static bool IsPooled(int flags) 
        {
            return (flags & FlagPooled) != 0;
        }

        /// <summary>
        /// Check whether flags denote pooled and acquired memory chunk.
        /// </summary>
        /// <param name="flags">Flags.</param>
        /// <returns><c>True</c> if acquired.</returns>
        public static bool IsAcquired(int flags)
        {
            return (flags & FlagAcquired) != 0;
        }

        #endregion

        #region UNPOOLED MEMORY 

        /// <summary>
        /// Allocate unpooled memory chunk.
        /// </summary>
        /// <param name="cap">Minimum capacity.</param>
        /// <returns>New memory pointer.</returns>
        public static long AllocateUnpooled(int cap)
        {
            long memPtr = Marshal.AllocHGlobal(MemHdrLen).ToInt64();
            long dataPtr = Marshal.AllocHGlobal(cap).ToInt64();

            *((long*)memPtr) = dataPtr;
            *((int*)(memPtr + MemHdrOffCap)) = cap;
            *((int*)(memPtr + MemHdrOffLen)) = 0;
            *((int*)(memPtr + MemHdrOffFlags)) = FlagExt;

            return memPtr;
        }


        /// <summary>
        /// Reallocate unpooled memory chunk.
        /// </summary>
        /// <param name="memPtr">Memory pointer.</param>
        /// <param name="cap">Minimum capacity.</param>
        /// <returns></returns>
        public static void ReallocateUnpooled(long memPtr, int cap)
        {
            long dataPtr = GetData(memPtr);

            long newDataPtr = Marshal.ReAllocHGlobal((IntPtr)dataPtr, (IntPtr)cap).ToInt64();

            if (dataPtr != newDataPtr)
                *((long*)memPtr) = newDataPtr; // Write new data address if needed.

            *((int*)(memPtr + MemHdrOffCap)) = cap; // Write new capacity.
        }

        /// <summary>
        /// Release unpooled memory chunk.
        /// </summary>
        /// <param name="memPtr">Memory pointer.</param>
        public static void ReleaseUnpooled(long memPtr) 
        {
            Marshal.FreeHGlobal((IntPtr)GetData(memPtr));
            Marshal.FreeHGlobal((IntPtr)memPtr);
        }

        #endregion

        #region POOLED MEMORY

        /// <summary>
        /// Allocate pool memory.
        /// </summary>
        /// <returns>Pool pointer.</returns>
        public static PlatformMemoryHeader* AllocatePool()
        {
            // 1. Allocate memory.
            var poolPtr = (PlatformMemoryHeader*) Marshal.AllocHGlobal((IntPtr) PoolHdrLen).ToInt64();

            for (var i = 0; i < PoolSize; i++)
                *(poolPtr + i) = new PlatformMemoryHeader(0, 0, 0, FlagExt | FlagPooled);

            return poolPtr;
        }

        /// <summary>
        /// Release pool memory.
        /// </summary>
        /// <param name="poolPtr">Pool pointer.</param>
        public static void ReleasePool(PlatformMemoryHeader* poolPtr)
        {
            for (var i = 0; i < PoolSize; i++)
            {
                var mem = (poolPtr + i)->Pointer;

                if (mem != 0)
                    Marshal.FreeHGlobal((IntPtr) mem);
            }

            // Clean pool chunk.
            Marshal.FreeHGlobal((IntPtr) poolPtr);
        }

        /// <summary>
        /// Allocate pooled memory chunk.
        /// </summary>
        /// <param name="poolPtr">Pool pointer.</param>
        /// <param name="cap">CalculateCapacity.</param>
        /// <returns>Memory pointer or <c>0</c> in case there are no free memory chunks in the pool.</returns>
        public static PlatformMemoryHeader* AllocatePooled(PlatformMemoryHeader* poolPtr, int cap)
        {
            Debug.Assert(poolPtr != (void*)0);

            for (var i = 0; i < PoolSize; i++)
            {
                var hdr = poolPtr + i;

                if (!IsAcquired(hdr->Flags))
                {
                    AllocatePooled0(hdr, cap);
                    return hdr;
                }
            }

            return (PlatformMemoryHeader*) 0;
        }

        /// <summary>
        /// Internal pooled memory chunk allocation routine.
        /// </summary>
        /// <param name="hdr">Memory header.</param>
        /// <param name="cap">Capacity.</param>
        private static void AllocatePooled0(PlatformMemoryHeader* hdr, int cap) 
        {
            Debug.Assert(hdr != (void*)0);

            if (hdr->Pointer == 0)
            {
                // First allocation of the chunk.
                hdr->Pointer = Marshal.AllocHGlobal(cap).ToInt64();
                hdr->Capacity = cap;
            }
            else if (cap > hdr->Capacity)
            {
                // Ensure that we have enough capacity.
                hdr->Pointer = Marshal.ReAllocHGlobal((IntPtr) hdr->Pointer, (IntPtr) cap).ToInt64();
                hdr->Capacity = cap;
            }

            hdr->Flags = FlagExt | FlagPooled | FlagAcquired;
        }

        /// <summary>
        /// Reallocate pooled memory chunk.
        /// </summary>
        /// <param name="hdr">Memory header.</param>
        /// <param name="cap">Minimum capacity.</param>
        public static void ReallocatePooled(PlatformMemoryHeader* hdr, int cap) 
        {
            Debug.Assert(hdr != (void*) 0);

            if (cap > hdr->Capacity)
            {
                hdr->Pointer = Marshal.ReAllocHGlobal((IntPtr) hdr->Pointer, (IntPtr) cap).ToInt64();
                hdr->Capacity = cap;
            }
        }

        /// <summary>
        /// Release pooled memory chunk.
        /// </summary>
        /// <param name="hdr">Memory header.</param>
        public static void ReleasePooled(PlatformMemoryHeader* hdr)
        {
            hdr->Flags ^= FlagAcquired;
        }

        #endregion

        #region MEMCPY

        /** Array copy delegate. */
        private delegate void MemCopy(byte* a1, byte* a2, int len);

        /** memcpy function handle. */
        private static readonly MemCopy Memcpy;

        /** Whether src and dest arguments are inverted. */
        [SuppressMessage("Microsoft.Performance", "CA1802:UseLiteralsWhereAppropriate")]
        private static readonly bool MemcpyInverted;

        /// <summary>
        /// Static initializer.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static PlatformMemoryUtils()
        {
            Type type = typeof(Buffer);

            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            Type[] paramTypes = { typeof(byte*), typeof(byte*), typeof(int) };

            // Assume .Net 4.5.
            MethodInfo mthd = type.GetMethod("Memcpy", flags, null, paramTypes, null);

            MemcpyInverted = true;

            if (mthd == null)
            {
                // Assume .Net 4.0.
                mthd = type.GetMethod("memcpyimpl", flags, null, paramTypes, null);

                MemcpyInverted = false;

                if (mthd == null)
                    throw new InvalidOperationException("Unable to get memory copy function delegate.");
            }

            Memcpy = (MemCopy)Delegate.CreateDelegate(typeof(MemCopy), mthd);
        }

        /// <summary>
        /// Unsafe memory copy routine.
        /// </summary>
        /// <param name="src">Source.</param>
        /// <param name="dest">Destination.</param>
        /// <param name="len">Length.</param>
        public static void CopyMemory(void* src, void* dest, int len)
        {
            CopyMemory((byte*)src, (byte*)dest, len);
        }

        /// <summary>
        /// Unsafe memory copy routine.
        /// </summary>
        /// <param name="src">Source.</param>
        /// <param name="dest">Destination.</param>
        /// <param name="len">Length.</param>
        public static void CopyMemory(byte* src, byte* dest, int len)
        {
            if (MemcpyInverted)
                Memcpy.Invoke(dest, src, len);
            else
                Memcpy.Invoke(src, dest, len);
        }

        #endregion
    }
}
