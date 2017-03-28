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
    using System.Collections;
    using System.Diagnostics;
    using Apache.Ignite.Core.Binary;

    /// <summary>
    /// Wrapper over <see cref="BinaryWriter"/> which ensures that fields are written in alphabetic order.
    /// </summary>
    internal class SortedBinaryWriter : IBinaryWriter
    {
        /** */
        private readonly IBinaryWriter _writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedBinaryWriter"/> class.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public SortedBinaryWriter(IBinaryWriter writer)
        {
            Debug.Assert(writer != null);

            _writer = writer;
        }

        /** <inheritDoc /> */
        public void WriteByte(string fieldName, byte val)
        {
            _writer.WriteByte(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteByteArray(string fieldName, byte[] val)
        {
            _writer.WriteByteArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteChar(string fieldName, char val)
        {
            _writer.WriteChar(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteCharArray(string fieldName, char[] val)
        {
            _writer.WriteCharArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteShort(string fieldName, short val)
        {
            _writer.WriteShort(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteShortArray(string fieldName, short[] val)
        {
            _writer.WriteShortArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteInt(string fieldName, int val)
        {
            _writer.WriteInt(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteIntArray(string fieldName, int[] val)
        {
            _writer.WriteIntArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteLong(string fieldName, long val)
        {
            _writer.WriteLong(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteLongArray(string fieldName, long[] val)
        {
            _writer.WriteLongArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteBoolean(string fieldName, bool val)
        {
            _writer.WriteBoolean(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteBooleanArray(string fieldName, bool[] val)
        {
            _writer.WriteBooleanArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteFloat(string fieldName, float val)
        {
            _writer.WriteFloat(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteFloatArray(string fieldName, float[] val)
        {
            _writer.WriteFloatArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteDouble(string fieldName, double val)
        {
            _writer.WriteDouble(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteDoubleArray(string fieldName, double[] val)
        {
            _writer.WriteDoubleArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteDecimal(string fieldName, decimal? val)
        {
            _writer.WriteDecimal(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteDecimalArray(string fieldName, decimal?[] val)
        {
            _writer.WriteDecimalArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteTimestamp(string fieldName, DateTime? val)
        {
            _writer.WriteTimestamp(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteTimestampArray(string fieldName, DateTime?[] val)
        {
            _writer.WriteTimestampArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteString(string fieldName, string val)
        {
            _writer.WriteString(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteStringArray(string fieldName, string[] val)
        {
            _writer.WriteStringArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteGuid(string fieldName, Guid? val)
        {
            _writer.WriteGuid(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteGuidArray(string fieldName, Guid?[] val)
        {
            _writer.WriteGuidArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteEnum<T>(string fieldName, T val)
        {
            _writer.WriteEnum(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteEnumArray<T>(string fieldName, T[] val)
        {
            _writer.WriteEnumArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteObject<T>(string fieldName, T val)
        {
            _writer.WriteObject(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteArray<T>(string fieldName, T[] val)
        {
            _writer.WriteArray(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteCollection(string fieldName, ICollection val)
        {
            _writer.WriteCollection(fieldName, val);
        }

        /** <inheritDoc /> */
        public void WriteDictionary(string fieldName, IDictionary val)
        {
            _writer.WriteDictionary(fieldName, val);
        }

        /** <inheritDoc /> */
        public IBinaryRawWriter GetRawWriter()
        {
            return _writer.GetRawWriter();
        }
    }
}
