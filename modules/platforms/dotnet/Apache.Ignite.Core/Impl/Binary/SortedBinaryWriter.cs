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

        public void WriteByte(string fieldName, byte val)
        {
            _writer.WriteByte(fieldName, val);
        }

        public void WriteByteArray(string fieldName, byte[] val)
        {
            _writer.WriteByteArray(fieldName, val);
        }

        public void WriteChar(string fieldName, char val)
        {
            _writer.WriteChar(fieldName, val);
        }

        public void WriteCharArray(string fieldName, char[] val)
        {
            _writer.WriteCharArray(fieldName, val);
        }

        public void WriteShort(string fieldName, short val)
        {
            _writer.WriteShort(fieldName, val);
        }

        public void WriteShortArray(string fieldName, short[] val)
        {
            _writer.WriteShortArray(fieldName, val);
        }

        public void WriteInt(string fieldName, int val)
        {
            _writer.WriteInt(fieldName, val);
        }

        public void WriteIntArray(string fieldName, int[] val)
        {
            _writer.WriteIntArray(fieldName, val);
        }

        public void WriteLong(string fieldName, long val)
        {
            _writer.WriteLong(fieldName, val);
        }

        public void WriteLongArray(string fieldName, long[] val)
        {
            _writer.WriteLongArray(fieldName, val);
        }

        public void WriteBoolean(string fieldName, bool val)
        {
            _writer.WriteBoolean(fieldName, val);
        }

        public void WriteBooleanArray(string fieldName, bool[] val)
        {
            _writer.WriteBooleanArray(fieldName, val);
        }

        public void WriteFloat(string fieldName, float val)
        {
            _writer.WriteFloat(fieldName, val);
        }

        public void WriteFloatArray(string fieldName, float[] val)
        {
            _writer.WriteFloatArray(fieldName, val);
        }

        public void WriteDouble(string fieldName, double val)
        {
            _writer.WriteDouble(fieldName, val);
        }

        public void WriteDoubleArray(string fieldName, double[] val)
        {
            _writer.WriteDoubleArray(fieldName, val);
        }

        public void WriteDecimal(string fieldName, decimal? val)
        {
            _writer.WriteDecimal(fieldName, val);
        }

        public void WriteDecimalArray(string fieldName, decimal?[] val)
        {
            _writer.WriteDecimalArray(fieldName, val);
        }

        public void WriteTimestamp(string fieldName, DateTime? val)
        {
            _writer.WriteTimestamp(fieldName, val);
        }

        public void WriteTimestampArray(string fieldName, DateTime?[] val)
        {
            _writer.WriteTimestampArray(fieldName, val);
        }

        public void WriteString(string fieldName, string val)
        {
            _writer.WriteString(fieldName, val);
        }

        public void WriteStringArray(string fieldName, string[] val)
        {
            _writer.WriteStringArray(fieldName, val);
        }

        public void WriteGuid(string fieldName, Guid? val)
        {
            _writer.WriteGuid(fieldName, val);
        }

        public void WriteGuidArray(string fieldName, Guid?[] val)
        {
            _writer.WriteGuidArray(fieldName, val);
        }

        public void WriteEnum<T>(string fieldName, T val)
        {
            _writer.WriteEnum(fieldName, val);
        }

        public void WriteEnumArray<T>(string fieldName, T[] val)
        {
            _writer.WriteEnumArray(fieldName, val);
        }

        public void WriteObject<T>(string fieldName, T val)
        {
            _writer.WriteObject(fieldName, val);
        }

        public void WriteArray<T>(string fieldName, T[] val)
        {
            _writer.WriteArray(fieldName, val);
        }

        public void WriteCollection(string fieldName, ICollection val)
        {
            _writer.WriteCollection(fieldName, val);
        }

        public void WriteDictionary(string fieldName, IDictionary val)
        {
            _writer.WriteDictionary(fieldName, val);
        }

        public IBinaryRawWriter GetRawWriter()
        {
            return _writer.GetRawWriter();
        }
    }
}
