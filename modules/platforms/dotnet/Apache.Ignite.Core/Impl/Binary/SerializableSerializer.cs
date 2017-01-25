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

    /// <summary>
    /// Serializes classes that implement <see cref="ISerializable"/>
    /// </summary>
    internal class SerializableSerializer : IBinarySerializerInternal
    {
        /** */
        private readonly Func<SerializationInfo, StreamingContext, object> _ctorFunc;

        /** */
        public SerializableSerializer(Type type)
        {
            // TODO: DelegateTypeDescriptor.
            _ctorFunc = (info, ctx) => Activator.CreateInstance(type, info, ctx);
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
            var serInfo = new SerializationInfo(desc.Type, new FormatterConverter());
            var ctx = new StreamingContext(StreamingContextStates.All);

            var binaryType = reader.Marshaller.GetBinaryType(desc.TypeId);

            foreach (var fieldName in binaryType.Fields)
            {
                var fieldVal = reader.ReadObject<object>(fieldName);

                serInfo.AddValue(fieldName, fieldVal);
            }

            // TODO: Unwrap IObjectReference when needed.

            return (T) _ctorFunc(serInfo, ctx);
        }

        /** <inheritdoc /> */
        public bool SupportsHandles
        {
            // Can't support handles, since deserialization happens via constructor call.
            get { return false; }
        }
    }
}
