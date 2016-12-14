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
    using System.IO;
    using Apache.Ignite.Core.Impl.Binary.IO;

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
                {
                    var dc = (decimal) val;

                    if (dc == decimal.Zero)
                        return 0;

                    if (dc < long.MaxValue)
                    {
                        // TODO: Scale?
                        return 31*((long) dc).GetHashCode() + 20;
                    }

                    var stream = new BinaryHeapStream(20);
                    // TODO: Use specialized method.
                    DecimalUtils.WriteDecimal((decimal)val, stream);

                    stream.Seek(0, SeekOrigin.Begin);

                    var scale = stream.ReadInt();
                    var len = stream.ReadInt();
                    var mag = stream.ReadByteArray(len);

                    // TODO
                    return scale.GetHashCode() + mag[0].GetHashCode();
                }

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
                    // TODO
                }

                if (val is DateTime)
                {
                    // TODO
                }

                if (val is string)
                {
                    // TODO
                }
            }

            // Fall back to default for all other types.
            return val.GetHashCode();
        }
    }
}
