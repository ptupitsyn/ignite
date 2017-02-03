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
    using System.Diagnostics;
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
        private readonly Type _type;

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
            Debug.Assert(type != null);

            _type = type;

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
        /// Gets the serialization ctor.
        /// </summary>
        public Func<SerializationInfo, StreamingContext, object> SerializationCtor
        {
            get
            {
                if (_serializationCtor == null)
                    throw GetMissingCtorException();

                return _serializationCtor;
            }
        }

        /// <summary>
        /// Gets the serialization ctor to call on an uninitialized instance.
        /// </summary>
        public Action<object, SerializationInfo, StreamingContext> SerializationCtorUninitialized
        {
            get
            {
                if (_serializationCtorUninitialized == null)
                    throw GetMissingCtorException();

                return _serializationCtorUninitialized;
            }
        }

        /// <summary>
        /// Gets the <see cref="DelegateTypeDescriptor" /> by type.
        /// </summary>
        public static SerializableTypeDescriptor Get(Type type)
        {
            SerializableTypeDescriptor result;

            return Descriptors.TryGetValue(type, out result)
                ? result
                : Descriptors.GetOrAdd(type, t => new SerializableTypeDescriptor(t));
        }
                
        /// <summary>
        /// Gets the missing ctor exception.
        /// </summary>
        private SerializationException GetMissingCtorException()
        {
            // Same exception as .NET code throws.
            return new SerializationException(
                string.Format("The constructor to deserialize an object of type '{0}' was not found.", _type));
        }
    }
}
