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
    using System.Runtime.Serialization;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary.Metadata;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Serializes classes that implement <see cref="ISerializable"/>
    /// </summary>
    internal class SerializableSerializer : IBinarySerializerInternal
    {
        /** */
        private const string FieldNamesField = "Ignite.NET_SerializableSerializer_FieldNames";

        /** */
        private const string FieldTypeField = "Ignite.NET_SerializableSerializer_FieldType_";

        /** */
        private readonly Action<object, SerializationInfo, StreamingContext> _ctorAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableSerializer"/> class.
        /// </summary>
        public SerializableSerializer(Type type)
        {
            _ctorAction = DelegateTypeDescriptor.GetSerializationConstructorUninitialized(type);
        }

        /** <inheritdoc /> */
        public void WriteBinary<T>(T obj, BinaryWriter writer)
        {
            var serializable = (ISerializable) obj;
            var objType = obj.GetType();

            var serInfo = new SerializationInfo(objType, new FormatterConverter());
            var ctx = new StreamingContext(StreamingContextStates.All);

            serializable.GetObjectData(serInfo, ctx);

            WriteFieldNames(writer, serInfo);

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
                    writer.WriteBoolean(FieldTypeField + entry.Name, true);
                }
            }

            WriteCustomTypeInfo(writer, serInfo, serializable);
        }

        /// <summary>
        /// Writes the field names.
        /// </summary>
        private static void WriteFieldNames(BinaryWriter writer, SerializationInfo serInfo)
        {
            if (serInfo.MemberCount <= 0 || writer.Marshaller.Ignite != null)
            {
                // Online mode: field names are in binary metadata.
                return;
            }

            // Offline mode: write all field names.
            var fieldNames = new string[serInfo.MemberCount];
            int i = 0;

            foreach (var entry in serInfo)
            {
                fieldNames[i++] = entry.Name;
            }

            writer.WriteStringArray(FieldNamesField, fieldNames);
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
                customType = Type.GetType(serInfo.FullTypeName, true);
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
            var res = (T)FormatterServices.GetUninitializedObject(desc.Type);

            int objId = SerializableCallback.Push(res);

            reader.AddHandle(pos, res);

            ReadObject(res, reader, desc, objId);

            SerializableCallback.Pop();

            return res;
        }

        /// <summary>
        /// Reads the object.
        /// </summary>
        private void ReadObject(object obj, BinaryReader reader, IBinaryTypeDescriptor desc, int objId)
        {
            var serInfo = GetSerializationInfo(reader, desc);

            var raw = reader.GetRawReader();

            if (raw.ReadBoolean())
            {
                // Custom type is present.
                var res = ReadAsCustomType(raw, serInfo, reader.Marshaller);

                BinaryUtils.CopyFields(res, obj);
                SerializableCallback.SetReference(objId, res);
            }
            else
            {
                _ctorAction(obj, serInfo, DefaultStreamingContext);
            }
        }

        /// <summary>
        /// Reads the object as a custom type.
        /// </summary>
        private static object ReadAsCustomType(IBinaryRawReader raw, SerializationInfo serInfo, Marshaller marsh)
        {
            Type customType;

            if (raw.ReadBoolean())
            {
                // Registered type written as type id.
                var typeId = raw.ReadInt();
                customType = marsh.GetDescriptor(true, typeId, true).Type;

                if (customType == null)
                {
                    throw new BinaryObjectException(string.Format(
                        "Failed to resolve custom type provided by SerializationInfo: [typeId={0}]", typeId));
                }
            }
            else
            {
                // Unregistered type written as type name.
                var typeName = raw.ReadString();
                customType = new TypeResolver().ResolveType(typeName);

                if (customType == null)
                {
                    throw new BinaryObjectException(string.Format(
                        "Failed to resolve custom type provided by SerializationInfo: [typeName={0}]", typeName));
                }
            }

            var ctorFunc = DelegateTypeDescriptor.GetSerializationConstructor(customType);

            var customObj = ctorFunc(serInfo, DefaultStreamingContext);

            var wrapper = customObj as IObjectReference;

            return wrapper == null
                ? customObj
                : wrapper.GetRealObject(DefaultStreamingContext);
        }

        /// <summary>
        /// Gets the default streaming context.
        /// </summary>
        private static StreamingContext DefaultStreamingContext
        {
            get { return new StreamingContext(StreamingContextStates.All); }
        }

        /// <summary>
        /// Gets the serialization information.
        /// </summary>
        private static SerializationInfo GetSerializationInfo(BinaryReader reader, IBinaryTypeDescriptor desc)
        {
            var serInfo = new SerializationInfo(desc.Type, new FormatterConverter());
            var fieldNames = GetFieldNames(reader, desc);

            foreach (var fieldName in fieldNames)
            {
                var fieldVal = ReadField(reader, fieldName);

                serInfo.AddValue(fieldName, fieldVal);
            }

            return serInfo;
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
        private static IEnumerable<string> GetFieldNames(BinaryReader reader, IBinaryTypeDescriptor desc)
        {
            var fieldNames = reader.ReadStringArray(FieldNamesField);

            if (fieldNames != null)
                return fieldNames;

            var binaryType = reader.Marshaller.GetBinaryType(desc.TypeId);

            if (binaryType == BinaryType.Empty)
            {
                throw new BinaryObjectException(string.Format(
                    "Failed to find BinaryType for type [typeId={0}, typeName={1}]", desc.TypeId, desc.Type));
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
