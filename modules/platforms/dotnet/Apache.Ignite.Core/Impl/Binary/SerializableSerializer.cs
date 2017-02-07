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
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary.Metadata;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Serializes classes that implement <see cref="ISerializable"/>.
    /// </summary>
    internal class SerializableSerializer : IBinarySerializerInternal
    {
        /** */
        private const string FieldNamesField = "Ignite.NET_SerializableSerializer_FieldNames";

        /** */
        private const string FieldTypeField = "Ignite.NET_SerializableSerializer_FieldType_";

        /** */
        private readonly SerializableTypeDescriptor _serializableTypeDesc;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableSerializer"/> class.
        /// </summary>
        public SerializableSerializer(Type type)
        {
            IgniteArgumentCheck.NotNull(type, "type");

            _serializableTypeDesc = SerializableTypeDescriptor.Get(type);
        }

        /** <inheritdoc /> */
        public void WriteBinary<T>(T obj, BinaryWriter writer)
        {
            var ctx = GetStreamingContext(writer);
            _serializableTypeDesc.OnSerializing(obj, ctx);

            var serializable = (ISerializable) obj;
            var objType = obj.GetType();

            var serInfo = new SerializationInfo(objType, new FormatterConverter());

            serializable.GetObjectData(serInfo, ctx);

            // Write fields.
            foreach (var entry in serInfo)
            {
                writer.WriteObject(entry.Name, entry.Value);

                var type = entry.ObjectType;

                if (type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong)
                    || type == typeof(sbyte[]) || type == typeof(ushort[])
                    || type == typeof(uint[]) || type == typeof(ulong[]))
                {
                    // Denote .NET-specific type.
                    // TODO: Write as raw.
                    writer.WriteBoolean(FieldTypeField + entry.Name, true);
                }
            }

            // Write additional information in raw mode.
            writer.GetRawWriter();

            WriteFieldNames(writer, serInfo);

            WriteCustomTypeInfo(writer, serInfo, serializable);

            _serializableTypeDesc.OnSerialized(obj, ctx);
        }

        /// <summary>
        /// Writes the field names.
        /// </summary>
        private static void WriteFieldNames(BinaryWriter writer, SerializationInfo serInfo)
        {
            if (writer.Marshaller.Ignite != null)
            {
                // Online mode: field names are in binary metadata.
                writer.WriteInt(-1);
                return;
            }

            // Offline mode: write all field names.
            // Even if MemberCount is 0, write empty array to denote offline mode.
            writer.WriteInt(serInfo.MemberCount);

            foreach (var entry in serInfo)
            {
                writer.WriteString(entry.Name);
            }
        }

        /// <summary>
        /// Writes the custom type information.
        /// </summary>
        private static void WriteCustomTypeInfo(BinaryWriter writer, SerializationInfo serInfo, 
            ISerializable serializable)
        {
            // ISerializable implementor may call SerializationInfo.SetType() or FullTypeName setter.
            // In that case there is no serialization ctor on objType. 
            // Instead, we should instantiate specified custom type and then call IObjectReference.GetRealObject().
            Type customType = null;

            if (serInfo.IsFullTypeNameSetExplicit)
            {
                customType = new TypeResolver().ResolveType(serInfo.FullTypeName, serInfo.AssemblyName);
            }
            else if (serInfo.ObjectType != serializable.GetType())
            {
                customType = serInfo.ObjectType;
            }

            var raw = writer.GetRawWriter();

            if (customType != null)
            {
                raw.WriteBoolean(true);

                var desc = writer.Marshaller.GetDescriptor(customType);

                if (desc.IsRegistered)
                {
                    raw.WriteBoolean(true);
                    raw.WriteInt(desc.TypeId);
                }
                else
                {
                    raw.WriteBoolean(false);
                    raw.WriteString(customType.FullName);
                }
            }
            else
            {
                raw.WriteBoolean(false);
            }
        }

        /** <inheritdoc /> */
        public T ReadBinary<T>(BinaryReader reader, IBinaryTypeDescriptor desc, int pos)
        {
            var res = FormatterServices.GetUninitializedObject(desc.Type);

            var ctx = GetStreamingContext(reader);

            _serializableTypeDesc.OnDeserializing(res, ctx);

            int objId = DeserializationCallbackProcessor.Push(res);

            try
            {
                reader.AddHandle(pos, res);

                ReadObject(res, reader, desc, objId, ctx);

                _serializableTypeDesc.OnDeserialized(res, ctx);
            }
            finally
            {
                DeserializationCallbackProcessor.Pop();
            }


            return (T) res;
        }

        /// <summary>
        /// Reads the object.
        /// </summary>
        private void ReadObject(object obj, BinaryReader reader, IBinaryTypeDescriptor desc, int objId, 
            StreamingContext ctx)
        {
            var serInfo = new SerializationInfo(desc.Type, new FormatterConverter());

            // Read additional information from raw part.
            reader.SeekToRaw();

            var fieldNames = ReadFieldNames(reader, desc);

            var customType = reader.ReadBoolean() ? ReadCustomTypeInfo(reader) : null;

            // Read field values.
            reader.SeekToFields();

            foreach (var fieldName in fieldNames)
            {
                var fieldVal = ReadField(reader, fieldName);

                serInfo.AddValue(fieldName, fieldVal);
            }

            // Construct object.
            if (customType != null)
            {
                // Custom type is present.
                var res = ReadAsCustomType(customType, serInfo, ctx);

                ReflectionUtils.CopyFields(res, obj);
                DeserializationCallbackProcessor.SetReference(objId, res);
            }
            else
            {
                _serializableTypeDesc.SerializationCtorUninitialized(obj, serInfo, ctx);
            }
        }

        /// <summary>
        /// Reads the object as a custom type.
        /// </summary>
        private static object ReadAsCustomType(Type customType, SerializationInfo serInfo, StreamingContext ctx)
        {
            var ctorFunc = SerializableTypeDescriptor.Get(customType).SerializationCtor;

            var customObj = ctorFunc(serInfo, ctx);

            var wrapper = customObj as IObjectReference;

            return wrapper == null
                ? customObj
                : wrapper.GetRealObject(ctx);
        }

        /// <summary>
        /// Reads the custom type information.
        /// </summary>
        private static Type ReadCustomTypeInfo(BinaryReader reader)
        {
            Type customType;

            if (reader.ReadBoolean())
            {
                // Registered type written as type id.
                var typeId = reader.ReadInt();
                customType = reader.Marshaller.GetDescriptor(true, typeId, true).Type;

                if (customType == null)
                {
                    throw new BinaryObjectException(string.Format(
                        "Failed to resolve custom type provided by SerializationInfo: [typeId={0}]", typeId));
                }
            }
            else
            {
                // Unregistered type written as type name.
                var typeName = reader.ReadString();
                customType = new TypeResolver().ResolveType(typeName);

                if (customType == null)
                {
                    throw new BinaryObjectException(string.Format(
                        "Failed to resolve custom type provided by SerializationInfo: [typeName={0}]", typeName));
                }
            }

            return customType;
        }

        /// <summary>
        /// Gets the streaming context.
        /// </summary>
        private static StreamingContext GetStreamingContext(IBinaryReader reader)
        {
            return new StreamingContext(StreamingContextStates.All, reader);
        }

        /// <summary>
        /// Gets the streaming context.
        /// </summary>
        private static StreamingContext GetStreamingContext(IBinaryWriter writer)
        {
            return new StreamingContext(StreamingContextStates.All, writer);
        }

        /// <summary>
        /// Reads the field.
        /// <para />
        /// Java side does not have counterparts for byte, ushort, uint, ulong.
        /// For such fields we write a special boolean field indicating the type.
        /// If special field is present, then the value has to be converted to .NET-specific type.
        /// </summary>
        private static object ReadField(IBinaryReader reader, string fieldName)
        {
            var fieldVal = reader.ReadObject<object>(fieldName);

            if (fieldVal == null)
                return null;

            var fieldType = fieldVal.GetType();

            unchecked
            {
                if (fieldType == typeof(byte))
                {
                    return IsSpecialType(reader, fieldName) ? (sbyte) (byte) fieldVal : fieldVal;
                }

                if (fieldType == typeof(short))
                {
                    return IsSpecialType(reader, fieldName) ? (ushort) (short) fieldVal : fieldVal;
                }

                if (fieldType == typeof(int))
                {
                    return IsSpecialType(reader, fieldName) ? (uint) (int) fieldVal : fieldVal;
                }

                if (fieldType == typeof(long))
                {
                    return IsSpecialType(reader, fieldName) ? (ulong) (long) fieldVal : fieldVal;
                }

                if (fieldType == typeof(byte[]))
                {
                    return IsSpecialType(reader, fieldName) ? ConvertArray<byte, sbyte>((byte[]) fieldVal) : fieldVal;
                }

                if (fieldType == typeof(short[]))
                {
                    return IsSpecialType(reader, fieldName) 
                        ? ConvertArray<short, ushort>((short[]) fieldVal) : fieldVal;
                }

                if (fieldType == typeof(int[]))
                {
                    return IsSpecialType(reader, fieldName) ? ConvertArray<int, uint>((int[]) fieldVal) : fieldVal;
                }

                if (fieldType == typeof(long[]))
                {
                    return IsSpecialType(reader, fieldName) ? ConvertArray<long, ulong>((long[]) fieldVal) : fieldVal;
                }
            }

            return fieldVal;
        }

        /// <summary>
        /// Converts the array.
        /// </summary>
        private static TU[] ConvertArray<T, TU>(T[] arr)
        {
            var res = new TU[arr.Length];

            for (var i = 0; i < arr.Length; i++)
            {
                res[i] = TypeCaster<TU>.Cast(arr[i]);
            }

            return res;
        }

        /// <summary>
        /// Determines whether specified field is of a special .NET type, such as byte, uint, ushort, ulong.
        /// </summary>
        private static bool IsSpecialType(IBinaryReader reader, string fieldName)
        {
            return reader.ReadObject<bool?>(FieldTypeField + fieldName) == true;
        }

        /// <summary>
        /// Gets the field names.
        /// </summary>
        private static IEnumerable<string> ReadFieldNames(BinaryReader reader, IBinaryTypeDescriptor desc)
        {
            var fieldNames = reader.ReadStringArray(FieldNamesField);

            if (fieldNames != null)
                return fieldNames;

            var binaryType = reader.Marshaller.GetBinaryType(desc.TypeId);

            if (binaryType == BinaryType.Empty)
            {
                // Object without fields.
                return Enumerable.Empty<string>();
            }

            return binaryType.Fields;
        }

        /** <inheritdoc /> */
        public bool SupportsHandles
        {
            get { return true; }
        }
    }
}
