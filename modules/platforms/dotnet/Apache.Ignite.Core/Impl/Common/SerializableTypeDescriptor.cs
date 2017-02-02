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

namespace Apache.Ignite.Core.Impl.Common
{
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;

    /// <summary>
    /// Type descriptor with precompiled delegates to call serialization-related methods.
    /// </summary>
    internal class SerializableTypeDescriptor
    {
        /** Cached decriptors. */
        private static readonly CopyOnWriteConcurrentDictionary<Type, SerializableTypeDescriptor> Descriptors 
            = new CopyOnWriteConcurrentDictionary<Type, SerializableTypeDescriptor>();

        /** */
        private readonly Func<SerializationInfo, StreamingContext, object> _serializationCtor;

        /** */
        private readonly Action<object, SerializationInfo, StreamingContext> _serializationCtorUninitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableTypeDescriptor"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        private SerializableTypeDescriptor(Type type)
        {
            // Check if there is a serialization ctor.
            var argTypes = new[] {typeof(SerializationInfo), typeof(StreamingContext)};

            var serializationCtorInfo = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, argTypes, null);

            if (serializationCtorInfo != null)
            {
                _serializationCtor = DelegateConverter.CompileCtor<Func<SerializationInfo, StreamingContext, object>>(
                    serializationCtorInfo, argTypes, convertParamsFromObject: false);

                _serializationCtorUninitialized = DelegateConverter.CompileUninitializedObjectCtor<
                    Action<object, SerializationInfo, StreamingContext>>(serializationCtorInfo, argTypes);
            }
        }

        /// <summary>
        /// Gets the ctor for <see cref="ISerializable"/>.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>Precompiled invocator delegate.</returns>
        public static Func<SerializationInfo, StreamingContext, object> GetSerializationConstructor(Type type)
        {
            return Get(type)._serializationCtor;
        }

        /// <summary>
        /// Gets the ctor for <see cref="ISerializable"/> that acts on a
        /// result of <see cref="FormatterServices.GetUninitializedObject"/>.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>Precompiled invocator delegate.</returns>
        public static Action<object, SerializationInfo, StreamingContext> GetSerializationConstructorUninitialized(
            Type type)
        {
            return Get(type)._serializationCtorUninitialized;
        }

        /// <summary>
        /// Gets the <see cref="DelegateTypeDescriptor" /> by type.
        /// </summary>
        private static SerializableTypeDescriptor Get(Type type)
        {
            SerializableTypeDescriptor result;

            return Descriptors.TryGetValue(type, out result)
                ? result
                : Descriptors.GetOrAdd(type, t => new SerializableTypeDescriptor(t));
        }


    }
}
