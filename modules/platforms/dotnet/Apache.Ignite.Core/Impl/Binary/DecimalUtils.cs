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
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary.IO;

    /// <summary>
    /// Decimal handling.
    /// </summary>
    internal static class DecimalUtils
    {
        /// <summary>
        /// Writes the decimal.
        /// </summary>
        public static void WriteDecimal(decimal val, IBinaryStream stream)
        {
            // https://msdn.microsoft.com/en-us/library/system.decimal.getbits(v=vs.110).aspx
            // Vals are:
            // [0] = lo
            // [1] = mid
            // [2] = high
            // [3] = exponent + sign
            int[] vals = decimal.GetBits(val);

            // Get start index skipping leading zeros.
            int idx = vals[2] != 0 ? 2 : vals[1] != 0 ? 1 : vals[0] != 0 ? 0 : -1;

            // Write scale and negative flag.
            int expSign = vals[3] >> 16;  // trim unused word
            int scale = expSign & 0x00FF;  // clear sign bit
            int sign = expSign >> 15;  // 0 or -1
            stream.WriteInt(sign < 0 ? (int)((uint)scale | 0x80000000) : scale);

            if (idx == -1)
            {
                // Writing zero.
                stream.WriteInt(1);
                stream.WriteByte(0);
            }
            else
            {
                int len = (idx + 1) << 2;

                // Write data.
                for (int i = idx; i >= 0; i--)
                {
                    int curPart = vals[i];

                    int part24 = (curPart >> 24) & 0xFF;
                    int part16 = (curPart >> 16) & 0xFF;
                    int part8 = (curPart >> 8) & 0xFF;
                    int part0 = curPart & 0xFF;

                    if (i == idx)
                    {
                        // Possibly skipping some values here.
                        if (part24 != 0)
                        {
                            if ((part24 & 0x80) == 0x80)
                            {
                                stream.WriteInt(len + 1);

                                stream.WriteByte(BinaryUtils.ByteZero);
                            }
                            else
                                stream.WriteInt(len);

                            stream.WriteByte((byte)part24);
                            stream.WriteByte((byte)part16);
                            stream.WriteByte((byte)part8);
                            stream.WriteByte((byte)part0);
                        }
                        else if (part16 != 0)
                        {
                            if ((part16 & 0x80) == 0x80)
                            {
                                stream.WriteInt(len);

                                stream.WriteByte(BinaryUtils.ByteZero);
                            }
                            else
                                stream.WriteInt(len - 1);

                            stream.WriteByte((byte)part16);
                            stream.WriteByte((byte)part8);
                            stream.WriteByte((byte)part0);
                        }
                        else if (part8 != 0)
                        {
                            if ((part8 & 0x80) == 0x80)
                            {
                                stream.WriteInt(len - 1);

                                stream.WriteByte(BinaryUtils.ByteZero);
                            }
                            else
                                stream.WriteInt(len - 2);

                            stream.WriteByte((byte)part8);
                            stream.WriteByte((byte)part0);
                        }
                        else
                        {
                            if ((part0 & 0x80) == 0x80)
                            {
                                stream.WriteInt(len - 2);

                                stream.WriteByte(BinaryUtils.ByteZero);
                            }
                            else
                                stream.WriteInt(len - 3);

                            stream.WriteByte((byte)part0);
                        }
                    }
                    else
                    {
                        stream.WriteByte((byte)part24);
                        stream.WriteByte((byte)part16);
                        stream.WriteByte((byte)part8);
                        stream.WriteByte((byte)part0);
                    }
                }
            }
        }

        /// <summary>
        /// Reads the decimal.
        /// </summary>
        public static decimal? ReadDecimal(IBinaryStream stream)
        {
            int scale = stream.ReadInt();

            bool neg;

            if (scale < 0)
            {
                scale = scale & 0x7FFFFFFF;

                neg = true;
            }
            else
                neg = false;

            byte[] mag = BinaryUtils.ReadByteArray(stream);

            if (scale < 0 || scale > 28)
                throw new BinaryObjectException("Decimal value scale overflow (must be between 0 and 28): " + scale);

            if (mag.Length > 13)
                throw new BinaryObjectException("Decimal magnitude overflow (must be less than 96 bits): " +
                    mag.Length * 8);

            if (mag.Length == 13 && mag[0] != 0)
                throw new BinaryObjectException("Decimal magnitude overflow (must be less than 96 bits): " +
                        mag.Length * 8);

            int hi = 0;
            int mid = 0;
            int lo = 0;

            int ctr = -1;

            for (int i = mag.Length - 12; i < mag.Length; i++)
            {
                if (++ctr == 4)
                {
                    mid = lo;
                    lo = 0;
                }
                else if (ctr == 8)
                {
                    hi = mid;
                    mid = lo;
                    lo = 0;
                }

                if (i >= 0)
                    lo = (lo << 8) + mag[i];
            }

            return new decimal(lo, mid, hi, neg, (byte)scale);
        }

        /// <summary>
        /// Writes the decimal array.
        /// </summary>
        /// <param name="vals">The vals.</param>
        /// <param name="stream">The stream.</param>
        public static void WriteDecimalArray(decimal?[] vals, IBinaryStream stream)
        {
            stream.WriteInt(vals.Length);

            foreach (var val in vals)
            {
                if (val.HasValue)
                {
                    stream.WriteByte(BinaryUtils.TypeDecimal);

                    WriteDecimal(val.Value, stream);
                }
                else
                    stream.WriteByte(BinaryUtils.HdrNull);
            }
        }

        /// <summary>
        /// Reads the decimal array.
        /// </summary>
        public static decimal?[] ReadDecimalArray(IBinaryStream stream)
        {
            int len = stream.ReadInt();

            var vals = new decimal?[len];

            for (int i = 0; i < len; i++)
                vals[i] = stream.ReadByte() == BinaryUtils.HdrNull ? null : ReadDecimal(stream);

            return vals;
        }
    }
}
