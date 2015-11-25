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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary.IO;
    using Apache.Ignite.Core.Impl.Binary.Metadata;
    using Apache.Ignite.Core.Impl.Binary.Structure;

    /// <summary>
    /// Binary writer implementation.
    /// </summary>
    internal class BinaryWriter : IBinaryWriter, IBinaryRawWriter
    {
        /** Marshaller. */
        private readonly Marshaller _marsh;

        /** Stream. */
        private readonly IBinaryStream _stream;

        /** Builder (used only during build). */
        private BinaryObjectBuilder _builder;

        /** Handles. */
        private BinaryHandleDictionary<object, long> _hnds;

        /** Metadatas collected during this write session. */
        private IDictionary<int, BinaryType> _metas;

        /** Current type ID. */
        private int _curTypeId;

        /** Current name converter */
        private IBinaryNameMapper _curConverter;

        /** Current mapper. */
        private IBinaryIdMapper _curMapper;
        
        /** Current object start position. */
        private int _curPos;

        /** Current raw position. */
        private int _curRawPos;

        /** Whether we are currently detaching an object. */
        private bool _detaching;

        /** Current type structure tracker, */
        private BinaryStructureTracker _curStruct;

        /** Schema holder. */
        private readonly BinaryObjectSchemaHolder _schema = BinaryObjectSchemaHolder.Current;

        /// <summary>
        /// Gets the marshaller.
        /// </summary>
        internal Marshaller Marshaller
        {
            get { return _marsh; }
        }

        /// <summary>
        /// Write named boolean value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Boolean value.</param>
        public void WriteBoolean(string fieldName, bool val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeBool);

            _stream.WriteByte(BinaryUtils.TypeBool);
            _stream.WriteBool(val);
        }
        
        /// <summary>
        /// Write boolean value.
        /// </summary>
        /// <param name="val">Boolean value.</param>
        public void WriteBoolean(bool val)
        {
            _stream.WriteBool(val);
        }

        /// <summary>
        /// Write named boolean array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Boolean array.</param>
        public void WriteBooleanArray(string fieldName, bool[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayBool);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayBool);
                BinaryUtils.WriteBooleanArray(val, _stream);
            }
        }

        /// <summary>
        /// Write boolean array.
        /// </summary>
        /// <param name="val">Boolean array.</param>
        public void WriteBooleanArray(bool[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayBool);
                BinaryUtils.WriteBooleanArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named byte value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Byte value.</param>
        public void WriteByte(string fieldName, byte val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeBool);

            _stream.WriteByte(BinaryUtils.TypeByte);
            _stream.WriteByte(val);
        }

        /// <summary>
        /// Write byte value.
        /// </summary>
        /// <param name="val">Byte value.</param>
        public void WriteByte(byte val)
        {
            _stream.WriteByte(val);
        }

        /// <summary>
        /// Write named byte array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Byte array.</param>
        public void WriteByteArray(string fieldName, byte[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayByte);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayByte);
                BinaryUtils.WriteByteArray(val, _stream);
            }
        }

        /// <summary>
        /// Write byte array.
        /// </summary>
        /// <param name="val">Byte array.</param>
        public void WriteByteArray(byte[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayByte);
                BinaryUtils.WriteByteArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named short value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Short value.</param>
        public void WriteShort(string fieldName, short val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeShort);

            _stream.WriteByte(BinaryUtils.TypeShort);
            _stream.WriteShort(val);
        }

        /// <summary>
        /// Write short value.
        /// </summary>
        /// <param name="val">Short value.</param>
        public void WriteShort(short val)
        {
            _stream.WriteShort(val);
        }

        /// <summary>
        /// Write named short array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Short array.</param>
        public void WriteShortArray(string fieldName, short[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayShort);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayShort);
                BinaryUtils.WriteShortArray(val, _stream);
            }
        }

        /// <summary>
        /// Write short array.
        /// </summary>
        /// <param name="val">Short array.</param>
        public void WriteShortArray(short[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayShort);
                BinaryUtils.WriteShortArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named char value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Char value.</param>
        public void WriteChar(string fieldName, char val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeChar);

            _stream.WriteByte(BinaryUtils.TypeChar);
            _stream.WriteChar(val);
        }

        /// <summary>
        /// Write char value.
        /// </summary>
        /// <param name="val">Char value.</param>
        public void WriteChar(char val)
        {
            _stream.WriteChar(val);
        }

        /// <summary>
        /// Write named char array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Char array.</param>
        public void WriteCharArray(string fieldName, char[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayChar);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayChar);
                BinaryUtils.WriteCharArray(val, _stream);
            }
        }

        /// <summary>
        /// Write char array.
        /// </summary>
        /// <param name="val">Char array.</param>
        public void WriteCharArray(char[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayChar);
                BinaryUtils.WriteCharArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named int value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Int value.</param>
        public void WriteInt(string fieldName, int val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeInt);

            _stream.WriteByte(BinaryUtils.TypeInt);
            _stream.WriteInt(val);
        }

        /// <summary>
        /// Write int value.
        /// </summary>
        /// <param name="val">Int value.</param>
        public void WriteInt(int val)
        {
            _stream.WriteInt(val);
        }

        /// <summary>
        /// Write named int array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Int array.</param>
        public void WriteIntArray(string fieldName, int[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayInt);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayInt);
                BinaryUtils.WriteIntArray(val, _stream);
            }
        }

        /// <summary>
        /// Write int array.
        /// </summary>
        /// <param name="val">Int array.</param>
        public void WriteIntArray(int[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayInt);
                BinaryUtils.WriteIntArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named long value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Long value.</param>
        public void WriteLong(string fieldName, long val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeLong);

            _stream.WriteByte(BinaryUtils.TypeLong);
            _stream.WriteLong(val);
        }

        /// <summary>
        /// Write long value.
        /// </summary>
        /// <param name="val">Long value.</param>
        public void WriteLong(long val)
        {
            _stream.WriteLong(val);
        }

        /// <summary>
        /// Write named long array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Long array.</param>
        public void WriteLongArray(string fieldName, long[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayLong);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayLong);
                BinaryUtils.WriteLongArray(val, _stream);
            }
        }

        /// <summary>
        /// Write long array.
        /// </summary>
        /// <param name="val">Long array.</param>
        public void WriteLongArray(long[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayLong);
                BinaryUtils.WriteLongArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named float value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Float value.</param>
        public void WriteFloat(string fieldName, float val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeFloat);

            _stream.WriteByte(BinaryUtils.TypeFloat);
            _stream.WriteFloat(val);
        }

        /// <summary>
        /// Write float value.
        /// </summary>
        /// <param name="val">Float value.</param>
        public void WriteFloat(float val)
        {
            _stream.WriteFloat(val);
        }

        /// <summary>
        /// Write named float array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Float array.</param>
        public void WriteFloatArray(string fieldName, float[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayFloat);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayFloat);
                BinaryUtils.WriteFloatArray(val, _stream);
            }
        }

        /// <summary>
        /// Write float array.
        /// </summary>
        /// <param name="val">Float array.</param>
        public void WriteFloatArray(float[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayFloat);
                BinaryUtils.WriteFloatArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named double value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Double value.</param>
        public void WriteDouble(string fieldName, double val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeDouble);

            _stream.WriteByte(BinaryUtils.TypeDouble);
            _stream.WriteDouble(val);
        }

        /// <summary>
        /// Write double value.
        /// </summary>
        /// <param name="val">Double value.</param>
        public void WriteDouble(double val)
        {
            _stream.WriteDouble(val);
        }

        /// <summary>
        /// Write named double array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Double array.</param>
        public void WriteDoubleArray(string fieldName, double[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayDouble);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayDouble);
                BinaryUtils.WriteDoubleArray(val, _stream);
            }
        }

        /// <summary>
        /// Write double array.
        /// </summary>
        /// <param name="val">Double array.</param>
        public void WriteDoubleArray(double[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayDouble);
                BinaryUtils.WriteDoubleArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named decimal value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Decimal value.</param>
        public void WriteDecimal(string fieldName, decimal? val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeDecimal);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeDecimal);
                BinaryUtils.WriteDecimal(val.Value, _stream);
            }
        }

        /// <summary>
        /// Write decimal value.
        /// </summary>
        /// <param name="val">Decimal value.</param>
        public void WriteDecimal(decimal? val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeDecimal);
                BinaryUtils.WriteDecimal(val.Value, _stream);
            }
        }

        /// <summary>
        /// Write named decimal array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Decimal array.</param>
        public void WriteDecimalArray(string fieldName, decimal?[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayDecimal);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayDecimal);
                BinaryUtils.WriteDecimalArray(val, _stream);
            }
        }
        
        /// <summary>
        /// Write decimal array.
        /// </summary>
        /// <param name="val">Decimal array.</param>
        public void WriteDecimalArray(decimal?[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayDecimal);
                BinaryUtils.WriteDecimalArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named date value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Date value.</param>
        public void WriteTimestamp(string fieldName, DateTime? val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeTimestamp);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeTimestamp);
                BinaryUtils.WriteTimestamp(val.Value, _stream);
            }
        }
        
        /// <summary>
        /// Write date value.
        /// </summary>
        /// <param name="val">Date value.</param>
        public void WriteTimestamp(DateTime? val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeTimestamp);
                BinaryUtils.WriteTimestamp(val.Value, _stream);
            }
        }

        /// <summary>
        /// Write named date array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Date array.</param>
        public void WriteTimestampArray(string fieldName, DateTime?[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeTimestamp);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayTimestamp);
                BinaryUtils.WriteTimestampArray(val, _stream);
            }
        }

        /// <summary>
        /// Write date array.
        /// </summary>
        /// <param name="val">Date array.</param>
        public void WriteTimestampArray(DateTime?[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayTimestamp);
                BinaryUtils.WriteTimestampArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named string value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">String value.</param>
        public void WriteString(string fieldName, string val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeString);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeString);
                BinaryUtils.WriteString(val, _stream);
            }
        }

        /// <summary>
        /// Write string value.
        /// </summary>
        /// <param name="val">String value.</param>
        public void WriteString(string val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeString);
                BinaryUtils.WriteString(val, _stream);
            }
        }

        /// <summary>
        /// Write named string array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">String array.</param>
        public void WriteStringArray(string fieldName, string[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayString);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayString);
                BinaryUtils.WriteStringArray(val, _stream);
            }
        }

        /// <summary>
        /// Write string array.
        /// </summary>
        /// <param name="val">String array.</param>
        public void WriteStringArray(string[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayString);
                BinaryUtils.WriteStringArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named GUID value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">GUID value.</param>
        public void WriteGuid(string fieldName, Guid? val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeGuid);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeGuid);
                BinaryUtils.WriteGuid(val.Value, _stream);
            }
        }

        /// <summary>
        /// Write GUID value.
        /// </summary>
        /// <param name="val">GUID value.</param>
        public void WriteGuid(Guid? val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeGuid);
                BinaryUtils.WriteGuid(val.Value, _stream);
            }
        }

        /// <summary>
        /// Write named GUID array.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">GUID array.</param>
        public void WriteGuidArray(string fieldName, Guid?[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayGuid);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayGuid);
                BinaryUtils.WriteGuidArray(val, _stream);
            }
        }

        /// <summary>
        /// Write GUID array.
        /// </summary>
        /// <param name="val">GUID array.</param>
        public void WriteGuidArray(Guid?[] val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayGuid);
                BinaryUtils.WriteGuidArray(val, _stream);
            }
        }

        /// <summary>
        /// Write named enum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Enum value.</param>
        public void WriteEnum<T>(string fieldName, T val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeEnum);

            WriteEnum(val);
        }

        /// <summary>
        /// Write enum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val">Enum value.</param>
        public void WriteEnum<T>(T val)
        {
            _stream.WriteByte(BinaryUtils.TypeEnum);

            if (val == null)
                WriteNullField();
            else
            {
                // TODO: Save meta
                BinaryUtils.WriteEnum(this, val);
            }
        }

        /// <summary>
        /// Write named enum array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Enum array.</param>
        public void WriteEnumArray<T>(string fieldName, T[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArrayEnum);

            WriteEnumArray(val);
        }

        /// <summary>
        /// Write enum array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val">Enum array.</param>
        public void WriteEnumArray<T>(T[] val)
        {
            WriteEnumArrayInternal(val, null);
        }

        /// <summary>
        /// Writes the enum array.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <param name="elementTypeId">The element type id.</param>
        public void WriteEnumArrayInternal(Array val, int? elementTypeId)
        {
            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArrayEnum);

                var elTypeId = elementTypeId ?? BinaryUtils.GetEnumTypeId(val.GetType().GetElementType(), Marshaller);

                BinaryUtils.WriteArray(val, this, elTypeId);
            }
        }

        /// <summary>
        /// Write named object value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Object value.</param>
        public void WriteObject<T>(string fieldName, T val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeObject);

            if (val == null)
                WriteNullField();
            else
                Write(val);
        }

        /// <summary>
        /// Write object value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val">Object value.</param>
        public void WriteObject<T>(T val)
        {
            Write(val);
        }

        /// <summary>
        /// Write named object array.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Object array.</param>
        public void WriteArray<T>(string fieldName, T[] val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeArray);

            if (val == null)
                WriteNullField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArray);
                BinaryUtils.WriteArray(val, this);
            }
        }

        /// <summary>
        /// Write object array.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="val">Object array.</param>
        public void WriteArray<T>(T[] val)
        {
            WriteArrayInternal(val);
        }

        /// <summary>
        /// Write object array.
        /// </summary>
        /// <param name="val">Object array.</param>
        public void WriteArrayInternal(Array val)
        {
            if (val == null)
                WriteNullRawField();
            else
            {
                _stream.WriteByte(BinaryUtils.TypeArray);
                BinaryUtils.WriteArray(val, this);
            }
        }

        /// <summary>
        /// Write named collection.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Collection.</param>
        public void WriteCollection(string fieldName, ICollection val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeCollection);

            if (val == null)
                WriteNullField();
            else
                WriteCollection(val);
        }

        /// <summary>
        /// Write collection.
        /// </summary>
        /// <param name="val">Collection.</param>
        public void WriteCollection(ICollection val)
        {
            WriteByte(BinaryUtils.TypeCollection);
            BinaryUtils.WriteCollection(val, this);
        }

        /// <summary>
        /// Write named dictionary.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="val">Dictionary.</param>
        public void WriteDictionary(string fieldName, IDictionary val)
        {
            WriteFieldId(fieldName, BinaryUtils.TypeDictionary);

            if (val == null)
                WriteNullField();
            else
                WriteDictionary(val);
        }

        /// <summary>
        /// Write dictionary.
        /// </summary>
        /// <param name="val">Dictionary.</param>
        public void WriteDictionary(IDictionary val)
        {
            WriteByte(BinaryUtils.TypeDictionary);
            BinaryUtils.WriteDictionary(val, this);
        }

        /// <summary>
        /// Write NULL field.
        /// </summary>
        private void WriteNullField()
        {
            _stream.WriteByte(BinaryUtils.HdrNull);
        }

        /// <summary>
        /// Write NULL raw field.
        /// </summary>
        private void WriteNullRawField()
        {
            _stream.WriteByte(BinaryUtils.HdrNull);
        }

        /// <summary>
        /// Get raw writer.
        /// </summary>
        /// <returns>
        /// Raw writer.
        /// </returns>
        public IBinaryRawWriter GetRawWriter()
        {
            if (_curRawPos == 0)
                _curRawPos = _stream.Position;

            return this;
        }

        /// <summary>
        /// Set new builder.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <returns>Previous builder.</returns>
        internal BinaryObjectBuilder SetBuilder(BinaryObjectBuilder builder)
        {
            BinaryObjectBuilder ret = _builder;

            _builder = builder;

            return ret;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="marsh">Marshaller.</param>
        /// <param name="stream">Stream.</param>
        internal BinaryWriter(Marshaller marsh, IBinaryStream stream)
        {
            _marsh = marsh;
            _stream = stream;
        }

        /// <summary>
        /// Write object.
        /// </summary>
        /// <param name="obj">Object.</param>
        public void Write<T>(T obj)
        {
            // Handle special case for null.
            if (obj == null)
            {
                _stream.WriteByte(BinaryUtils.HdrNull);

                return;
            }

            // We use GetType() of a real object instead of typeof(T) to take advantage of 
            // automatic Nullable'1 unwrapping.
            Type type = obj.GetType();

            // Handle common case when primitive is written.
            if (type.IsPrimitive)
            {
                WritePrimitive(obj, type);

                return;
            }

            // Handle enums.
            if (type.IsEnum)
            {
                WriteEnum(obj);

                return;
            }

            // Handle special case for builder.
            if (WriteBuilderSpecials(obj))
                return;

            // Suppose that we faced normal object and perform descriptor lookup.
            IBinaryTypeDescriptor desc = _marsh.GetDescriptor(type);

            if (desc != null)
            {
                // Writing normal object.
                var pos = _stream.Position;

                // Dealing with handles.
                if (!(desc.Serializer is IBinarySystemTypeSerializer) && WriteHandle(pos, obj))
                    return;

                // Skip header length as not everything is known now
                _stream.Seek(BinaryObjectHeader.Size, SeekOrigin.Current);

                // Preserve old frame.
                int oldTypeId = _curTypeId;
                IBinaryNameMapper oldConverter = _curConverter;
                IBinaryIdMapper oldMapper = _curMapper;
                int oldRawPos = _curRawPos;
                var oldPos = _curPos;
                
                var oldStruct = _curStruct;

                // Push new frame.
                _curTypeId = desc.TypeId;
                _curConverter = desc.NameMapper;
                _curMapper = desc.IdMapper;
                _curRawPos = 0;
                _curPos = pos;

                _curStruct = new BinaryStructureTracker(desc, desc.WriterTypeStructure);
                var schemaIdx = _schema.PushSchema();

                try
                {
                    // Write object fields.
                    desc.Serializer.WriteBinary(obj, this);

                    // Write schema
                    var schemaOffset = _stream.Position - pos;

                    int schemaId;
                    
                    var flags = desc.UserType
                        ? BinaryObjectHeader.Flag.UserType
                        : BinaryObjectHeader.Flag.None;

                    var hasSchema = _schema.WriteSchema(_stream, schemaIdx, out schemaId, ref flags);

                    if (hasSchema)
                    {
                        flags |= BinaryObjectHeader.Flag.HasSchema;

                        // Calculate and write header.
                        if (_curRawPos > 0)
                            _stream.WriteInt(_curRawPos - pos); // raw offset is in the last 4 bytes
                    }
                    else
                        schemaOffset = BinaryObjectHeader.Size;

                    if (_curRawPos > 0)
                        flags |= BinaryObjectHeader.Flag.HasRaw;

                    var len = _stream.Position - pos;

                    var header = new BinaryObjectHeader(desc.TypeId, obj.GetHashCode(), len,
                        schemaId, schemaOffset, flags);

                    BinaryObjectHeader.Write(header, _stream, pos);

                    Stream.Seek(pos + len, SeekOrigin.Begin); // Seek to the end
                }
                finally
                {
                    _schema.PopSchema(schemaIdx);
                }

                // Apply structure updates if any.
                _curStruct.UpdateWriterStructure(this);

                // Restore old frame.
                _curTypeId = oldTypeId;
                _curConverter = oldConverter;
                _curMapper = oldMapper;
                _curRawPos = oldRawPos;
                _curPos = oldPos;

                _curStruct = oldStruct;
            }
            else
            {
                // Are we dealing with a well-known type?
                var handler = BinarySystemHandlers.GetWriteHandler(type);

                if (handler == null)  // We did our best, object cannot be marshalled.
                    throw new BinaryObjectException("Unsupported object type [type=" + type + ", object=" + obj + ']');
                
                handler(this, obj);
            }
        }

        /// <summary>
        /// Write primitive type.
        /// </summary>
        /// <param name="val">Object.</param>
        /// <param name="type">Type.</param>
        private unsafe void WritePrimitive<T>(T val, Type type)
        {
            // .Net defines 14 primitive types. We support 12 - excluding IntPtr and UIntPtr.
            // Types check sequence is designed to minimize comparisons for the most frequent types.

            if (type == typeof(int))
            {
                _stream.WriteByte(BinaryUtils.TypeInt);
                _stream.WriteInt((int)(object)val);
            }
            else if (type == typeof(long))
            {
                _stream.WriteByte(BinaryUtils.TypeLong);
                _stream.WriteLong((long)(object)val);
            }
            else if (type == typeof(bool))
            {
                _stream.WriteByte(BinaryUtils.TypeBool);
                _stream.WriteBool((bool)(object)val);
            }
            else if (type == typeof(byte))
            {
                _stream.WriteByte(BinaryUtils.TypeByte);
                _stream.WriteByte((byte)(object)val);
            }
            else if (type == typeof(short))
            {
                _stream.WriteByte(BinaryUtils.TypeShort);
                _stream.WriteShort((short)(object)val);
            }
            else if (type == typeof (char))
            {
                _stream.WriteByte(BinaryUtils.TypeChar);
                _stream.WriteChar((char)(object)val);
            }
            else if (type == typeof(float))
            {
                _stream.WriteByte(BinaryUtils.TypeFloat);
                _stream.WriteFloat((float)(object)val);
            }
            else if (type == typeof(double))
            {
                _stream.WriteByte(BinaryUtils.TypeDouble);
                _stream.WriteDouble((double)(object)val);
            }
            else if (type == typeof(sbyte))
            {
                sbyte val0 = (sbyte)(object)val;

                _stream.WriteByte(BinaryUtils.TypeByte);
                _stream.WriteByte(*(byte*)&val0);
            }
            else if (type == typeof(ushort))
            {
                ushort val0 = (ushort)(object)val;

                _stream.WriteByte(BinaryUtils.TypeShort);
                _stream.WriteShort(*(short*)&val0);
            }
            else if (type == typeof(uint))
            {
                uint val0 = (uint)(object)val;

                _stream.WriteByte(BinaryUtils.TypeInt);
                _stream.WriteInt(*(int*)&val0);
            }
            else if (type == typeof(ulong))
            {
                ulong val0 = (ulong)(object)val;

                _stream.WriteByte(BinaryUtils.TypeLong);
                _stream.WriteLong(*(long*)&val0);
            }
            else
                throw new BinaryObjectException("Unsupported object type [type=" + type.FullName + ", object=" + val + ']');
        }

        /// <summary>
        /// Try writing object as special builder type.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns>True if object was written, false otherwise.</returns>
        private bool WriteBuilderSpecials<T>(T obj)
        {
            if (_builder != null)
            {
                // Special case for binary object during build.
                BinaryObject portObj = obj as BinaryObject;

                if (portObj != null)
                {
                    if (!WriteHandle(_stream.Position, portObj))
                        _builder.ProcessBinary(_stream, portObj);

                    return true;
                }

                // Special case for builder during build.
                BinaryObjectBuilder portBuilder = obj as BinaryObjectBuilder;

                if (portBuilder != null)
                {
                    if (!WriteHandle(_stream.Position, portBuilder))
                        _builder.ProcessBuilder(_stream, portBuilder);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Add handle to handles map.
        /// </summary>
        /// <param name="pos">Position in stream.</param>
        /// <param name="obj">Object.</param>
        /// <returns><c>true</c> if object was written as handle.</returns>
        private bool WriteHandle(long pos, object obj)
        {
            if (_hnds == null)
            {
                // Cache absolute handle position.
                _hnds = new BinaryHandleDictionary<object, long>(obj, pos);

                return false;
            }

            long hndPos;

            if (!_hnds.TryGetValue(obj, out hndPos))
            {
                // Cache absolute handle position.
                _hnds.Add(obj, pos);

                return false;
            }

            _stream.WriteByte(BinaryUtils.HdrHnd);

            // Handle is written as difference between position before header and handle position.
            _stream.WriteInt((int)(pos - hndPos));

            return true;
        }

        /// <summary>
        /// Perform action with detached semantics.
        /// </summary>
        /// <param name="a"></param>
        internal void WithDetach(Action<BinaryWriter> a)
        {
            if (_detaching)
                a(this);
            else
            {
                _detaching = true;

                BinaryHandleDictionary<object, long> oldHnds = _hnds;
                _hnds = null;

                try
                {
                    a(this);
                }
                finally
                {
                    _detaching = false;

                    if (oldHnds != null)
                    {
                        // Merge newly recorded handles with old ones and restore old on the stack.
                        // Otherwise we can use current handles right away.
                        if (_hnds != null)
                            oldHnds.Merge(_hnds);

                        _hnds = oldHnds;
                    }
                }
            }
        }

        /// <summary>
        /// Stream.
        /// </summary>
        internal IBinaryStream Stream
        {
            get { return _stream; }
        }

        /// <summary>
        /// Gets collected metadatas.
        /// </summary>
        /// <returns>Collected metadatas (if any).</returns>
        internal IDictionary<int, BinaryType> GetBinaryTypes()
        {
            return _metas;
        }

        /// <summary>
        /// Check whether the given object is binarizeble, i.e. it can be serialized with binary marshaller.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns>True if binarizable.</returns>
        internal bool IsBinarizable(object obj)
        {
            if (obj != null)
            {
                Type type = obj.GetType();

                // We assume object as binarizable only in case it has descriptor.
                // Collections, Enums and non-primitive arrays do not have descriptors
                // and this is fine here because we cannot know whether their members are binarizable.
                return _marsh.GetDescriptor(type) != null || BinarySystemHandlers.GetWriteHandler(type) != null;
            }

            return true;
        }

        /// <summary>
        /// Write field ID.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="fieldTypeId">Field type ID.</param>
        private void WriteFieldId(string fieldName, byte fieldTypeId)
        {
            if (_curRawPos != 0)
                throw new BinaryObjectException("Cannot write named fields after raw data is written.");

            var fieldId = _curStruct.GetFieldId(fieldName, fieldTypeId);

            _schema.PushField(fieldId, _stream.Position - _curPos);
        }

        /// <summary>
        /// Saves metadata for this session.
        /// </summary>
        /// <param name="desc">The descriptor.</param>
        /// <param name="fields">Fields metadata.</param>
        internal void SaveMetadata(IBinaryTypeDescriptor desc, IDictionary<string, int> fields)
        {
            Debug.Assert(desc != null);

            if (_metas == null)
            {
                BinaryType meta =
                    new BinaryType(desc.TypeId, desc.TypeName, fields, desc.AffinityKeyFieldName, desc.IsEnum);

                _metas = new Dictionary<int, BinaryType>(1);

                _metas[desc.TypeId] = meta;
            }
            else
            {
                BinaryType meta;

                if (_metas.TryGetValue(desc.TypeId, out meta))
                {
                    IDictionary<string, int> existingFields = meta.GetFieldsMap();

                    foreach (KeyValuePair<string, int> field in fields)
                    {
                        if (!existingFields.ContainsKey(field.Key))
                            existingFields[field.Key] = field.Value;
                    }
                }
                else
                    _metas[desc.TypeId] = 
                        new BinaryType(desc.TypeId, desc.TypeName, fields, desc.AffinityKeyFieldName, desc.IsEnum);
            }
        }
    }
}
