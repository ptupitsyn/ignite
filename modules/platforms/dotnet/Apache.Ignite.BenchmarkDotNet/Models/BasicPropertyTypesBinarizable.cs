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

namespace Apache.Ignite.BenchmarkDotNet.Models
{
    using System;
    using Apache.Ignite.Core.Binary;

    /// <summary>
    /// Benchmark model: properties of all basic types with <see cref="IBinarizable"/> implementation.
    /// </summary>
    public class BasicPropertyTypesBinarizable : BasicPropertyTypes, IBinarizable
    {
        /** <inheritDoc /> */
        public void WriteBinary(IBinaryWriter writer)
        {
            writer.WriteByte("Byte", Byte);
            writer.WriteByteArray("ByteArray", ByteArray);
            writer.WriteChar("Char", Char);
            writer.WriteCharArray("CharArray", CharArray);
            writer.WriteShort("Short", Short);
            writer.WriteShortArray("ShortArray", ShortArray);
            writer.WriteInt("Int", Int);
            writer.WriteIntArray("IntArray", IntArray);
            writer.WriteLong("Long", Long);
            writer.WriteLongArray("LongArray", LongArray);
            writer.WriteBoolean("Boolean", Boolean);
            writer.WriteBooleanArray("BooleanArray", BooleanArray);
            writer.WriteFloat("Float", Float);
            writer.WriteFloatArray("FloatArray", FloatArray);
            writer.WriteDouble("Double", Double);
            writer.WriteDoubleArray("DoubleArray", DoubleArray);
            writer.WriteDecimal("Decimal", Decimal);
            writer.WriteDecimalArray("DecimalArray", DecimalArray);
            writer.WriteString("String", String);
            writer.WriteStringArray("StringArray", StringArray);
            writer.WriteGuid("Guid", Guid);
            writer.WriteGuidArray("GuidArray", GuidArray);
        }

        /** <inheritDoc /> */
        public void ReadBinary(IBinaryReader reader)
        {
            Byte = reader.ReadByte("Byte");
            ByteArray = reader.ReadByteArray("ByteArray");
            Char = reader.ReadChar("Char");
            CharArray = reader.ReadCharArray("CharArray");
            Short = reader.ReadShort("Short");
            ShortArray = reader.ReadShortArray("ShortArray");
            Int = reader.ReadInt("Int");
            IntArray = reader.ReadIntArray("IntArray");
            Long = reader.ReadLong("Long");
            LongArray = reader.ReadLongArray("LongArray");
            Boolean = reader.ReadBoolean("Boolean");
            BooleanArray = reader.ReadBooleanArray("BooleanArray");
            Float = reader.ReadFloat("Float");
            FloatArray = reader.ReadFloatArray("FloatArray");
            Double = reader.ReadDouble("Double");
            DoubleArray = reader.ReadDoubleArray("DoubleArray");
            Decimal = reader.ReadDecimal("Decimal");
            DecimalArray = reader.ReadDecimalArray("DecimalArray");
            String = reader.ReadString("String");
            StringArray = reader.ReadStringArray("StringArray");
            Guid = reader.ReadObject<Guid>("Guid");
            GuidArray = reader.ReadGuidArray("GuidArray");
        }
    }
}
