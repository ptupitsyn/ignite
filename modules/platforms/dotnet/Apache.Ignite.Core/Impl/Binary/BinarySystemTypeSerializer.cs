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
    using System.Diagnostics;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Binary serializer for system types.
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    internal class BinarySystemTypeSerializer<T> : IBinarySerializerInternal
    {
        /** Ctor delegate. */
        private readonly Func<BinaryReader, T> _ctor;

        /** Writer delegate. */
        private readonly Action<BinaryWriter, T> _writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinarySystemTypeSerializer{T}"/> class.
        /// </summary>
        /// <param name="ctor">Constructor delegate.</param>
        public BinarySystemTypeSerializer(Func<BinaryReader, T> ctor, Action<BinaryWriter, T> writer)
        {
            Debug.Assert(ctor != null);
            Debug.Assert(writer != null);

            _ctor = ctor;
            _writer = writer;
        }

        /** <inheritDoc /> */
        public void WriteBinary<T1>(T1 obj, BinaryWriter writer)
        {
            _writer(writer, TypeCaster<T>.Cast(obj));
        }

        /** <inheritDoc /> */
        public T1 ReadBinary<T1>(BinaryReader reader, Type type, int pos)
        {
            return TypeCaster<T1>.Cast(_ctor(reader));
        }

        /** <inheritdoc /> */
        public bool SupportsHandles
        {
            get { return false; }
        }
    }
}