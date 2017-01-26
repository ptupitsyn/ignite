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
    using System.Runtime.Serialization;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Serializes classes that implement <see cref="ISerializable"/>
    /// </summary>
    internal class SerializableSerializer : IBinarySerializerInternal
    {
        /** */
        private readonly Func<SerializationInfo, StreamingContext, object> _ctorFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableSerializer"/> class.
        /// </summary>
        public SerializableSerializer(Type type)
        {
            _ctorFunc = DelegateTypeDescriptor.GetSerializationConstructor(type);
        }

        /** <inheritdoc /> */
        public void WriteBinary<T>(T obj, BinaryWriter writer)
        {
            var ser = (ISerializable) obj;
            var objType = obj.GetType();

            var serInfo = new SerializationInfo(objType, new FormatterConverter());
            var ctx = new StreamingContext(StreamingContextStates.All);

            ser.GetObjectData(serInfo, ctx);

            // Write custom fields.
            foreach (var entry in serInfo)
            {
                writer.WriteObject(entry.Name, entry.Value);
            }

            // Write custom type information.
            // ISerializable implementor may call SerializationInfo.SetType() or FullTypeName setter.
            // In that case there is no serialization ctor on objType. 
            // Instead, we should instantiate specified custom type and then call IObjectReference.GetRealObject().
            Type customType = null;

            if (serInfo.IsFullTypeNameSetExplicit)
            {
                customType = Type.GetType(serInfo.FullTypeName, true);
            }
            else if (serInfo.ObjectType != ser.GetType())
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
            var serInfo = GetSerializationInfo<T>(reader, desc);

            var raw = reader.GetRawReader();

            if (raw.ReadBoolean())
            {
                // Custom type is present.
                return ReadAsCustomType<T>(raw, serInfo, reader.Marshaller);
            }

            return (T) _ctorFunc(serInfo, DefaultStreamingContext);
        }

        /// <summary>
        /// Reads the object as a custom type.
        /// </summary>
        private static T ReadAsCustomType<T>(IBinaryRawReader raw, SerializationInfo serInfo, Marshaller marsh)
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
                ? (T) customObj
                : (T) wrapper.GetRealObject(DefaultStreamingContext);
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
        private static SerializationInfo GetSerializationInfo<T>(BinaryReader reader, IBinaryTypeDescriptor desc)
        {
            var serInfo = new SerializationInfo(desc.Type, new FormatterConverter());
            var binaryType = reader.Marshaller.GetBinaryType(desc.TypeId);

            foreach (var fieldName in binaryType.Fields)
            {
                var fieldVal = reader.ReadObject<object>(fieldName);

                serInfo.AddValue(fieldName, fieldVal);
            }
            return serInfo;
        }

        /** <inheritdoc /> */
        public bool SupportsHandles
        {
            // Can't support handles, since deserialization happens via constructor call.
            get { return false; }
        }
    }
}
