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

namespace Apache.Ignite.Core.Impl.Binary
{
    using System;

    /// <summary>
    /// Java hash code implementations for basic types.
    /// </summary>
    internal static class JavaHashCode
    {
        /// <summary>
        /// Gets the hash code using Java algorithm for basic types.
        /// </summary>
        public static unsafe int GetHashCode(object val)
        {
            if (val == null)
                return 0;

            unchecked
            {
                // Types check sequence is designed to minimize comparisons for the most frequent types.
                // All checks support their nullable counterparts.
                if (val is int)
                    return (int) val;

                if (val is long)
                {
                    var l = (long) val;
                    return (int) (l ^ (l >> 32));
                }

                if (val is bool)
                    return (bool) val ? 1231 : 1237;

                if (val is byte)
                    return (sbyte) (byte) val;

                if (val is short)
                    return (short) val;

                if (val is char)
                    return (char) val;

                if (val is float)
                {
                    var f = (float) val;
                    return *(int*) &f;
                }

                if (val is double)
                {
                    var d = (double) val;
                    var l = *(long*) &d;
                    return (int) (l ^ (l >> 32));
                }

                if (val is decimal)
                    return DecimalUtils.GetJavaHashCode((decimal) val);

                if (val is sbyte)
                    return (sbyte) val;

                if (val is ushort)
                    return (short) (ushort) val;

                if (val is uint)
                    return (int) (uint) val;

                if (val is ulong)
                {
                    var l = (long) (ulong) val;
                    return (int) (l ^ (l >> 32));
                }

                if (val is Guid)
                {
                    return GetGuidHashCode((Guid) val);
                }

                if (val is DateTime)
                {
                    long hi;
                    int lo;
                    BinaryUtils.ToJavaDate((DateTime) val, out hi, out lo);

                    int nanos = (int) (hi % 1000L * 1000000L);

                    long time = hi / 1000L * 1000L;

                    if (nanos < 0)
                    {
                        nanos += 1000000000;

                        time = (hi / 1000L - 1L) * 1000L;
                    }

                    nanos += lo;

                    time += nanos / 1000000;

                    return (int) time ^ (int) (time >> 32);
                }

                var str = val as string;
                if (str != null)
                {
                    return GetStringHashCode(str);
                }
            }

            // Fall back to default for all other types.
            return val.GetHashCode();
        }

        /// <summary>
        /// Gets the Guid (UUID) hash code.
        /// </summary>
        private static unsafe int GetGuidHashCode(Guid val)
        {
            byte* guidBytes = stackalloc byte[16];

            BinaryUtils.GetGuidBytes(val, guidBytes);

            var mostSig = *(long*) guidBytes;
            var leastSig = *((long*) guidBytes + 1);

            var hash = mostSig ^ leastSig;
            return (int) ((hash >> 32) ^ hash);
        }

        /// <summary>
        /// Gets the string hash code.
        /// This corresponds to BinaryBasicIdMapper.lowerCaseHashCode.
        /// </summary>
        public static int GetBinaryBasicIdMapperLowerCaseHashCode(string val)
        {
            if (val == null)
                return 0;

            int hash = 0;

            unchecked
            {
                // ReSharper disable once LoopCanBeConvertedToQuery (performance)
                foreach (var c in val)
                    hash = 31 * hash + ('A' <= c && c <= 'Z' ? c | 0x20 : c);
            }

            return hash;
        }

        /// <summary>
        /// Gets the string hash code.
        /// </summary>
        public static int GetStringHashCode(string val)
        {
            if (val == null)
                return 0;

            int hash = 0;

            unchecked
            {
                // ReSharper disable once LoopCanBeConvertedToQuery (performance)
                foreach (var c in val)
                    hash = 31 * hash + c;
            }

            return hash;
        }
    }
}
