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

    /// <summary>
    /// Serializes classes that implement <see cref="ISerializable"/>
    /// </summary>
    internal class SerializableSerializer : IBinarySerializerInternal
    {
        /** <inheritdoc /> */
        public void WriteBinary<T>(T obj, BinaryWriter writer)
        {
            var ser = (ISerializable) obj;

            var serInfo = new SerializationInfo(ser.GetType(), new FormatterConverter());
            var ctx = new StreamingContext(StreamingContextStates.All);

            ser.GetObjectData(serInfo, ctx);

            // Write custom fields.
            foreach (var entry in serInfo)
            {
                writer.WriteObject(entry.Name, entry.Value);
            }

            // TODO: Write type info??
        }

        /** <inheritdoc /> */
        public T ReadBinary<T>(BinaryReader reader, IBinaryTypeDescriptor desc, int pos)
        {
            var serInfo = new SerializationInfo(desc.Type, new FormatterConverter());
            var ctx = new StreamingContext(StreamingContextStates.All);

            // TODO: How do we know field names?
            // 1) Get schema from reader
            // 2) Get BinaryType from marhaller
            // 3) Read fields one by one.

            //reader.Marshaller.GetBinaryType()

            // TODO: Compiled delegate.
            return (T) Activator.CreateInstance(desc.Type, serInfo, ctx);
        }

        /** <inheritdoc /> */
        public bool SupportsHandles
        {
            // Can't support handles, since deserialization happens via constructor call.
            get { return false; }
        }
    }
}
