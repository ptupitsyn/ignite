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
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary.Deployment;
    using Apache.Ignite.Core.Impl.Binary.IO;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Wraps Serializable item in a binarizable.
    /// </summary>
    internal class SerializableObjectHolder : IBinaryWriteAware
    {
        /** */
        private readonly object _item;

        /** */
        private static readonly ThreadLocal<BinaryReader> CurrentReader = new ThreadLocal<BinaryReader>();

        /// <summary>
        /// Initializes the <see cref="SerializableObjectHolder"/> class.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static SerializableObjectHolder()
        {
            // This is needed with [Serializable] user types.
            // When want to resolve assemblies based only on their name, excluding version.
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableObjectHolder"/> class.
        /// </summary>
        /// <param name="item">The item to wrap.</param>
        public SerializableObjectHolder(object item)
        {
            _item = item;
        }

        /// <summary>
        /// Gets the item to wrap.
        /// </summary>
        public object Item
        {
            get { return _item; }
        }

        /** <inheritDoc /> */
        public void WriteBinary(IBinaryWriter writer)
        {
            Debug.Assert(writer != null);

            var writer0 = (BinaryWriter)writer.GetRawWriter();

            writer0.WithDetach(w =>
            {
                using (var streamAdapter = new BinaryStreamAdapter(w.Stream))
                {
                    new BinaryFormatter().Serialize(streamAdapter, Item);
                }
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableObjectHolder"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public SerializableObjectHolder(BinaryReader reader)
        {
            Debug.Assert(reader != null);

            CurrentReader.Value = reader;

            try
            {
                using (var streamAdapter = new BinaryStreamAdapter(reader.Stream))
                {
                    _item = new BinaryFormatter().Deserialize(streamAdapter, null);
                }
            }
            finally
            {
                CurrentReader.Value = null;
            }
        }

        /// <summary>
        /// Handles the AssemblyResolve event of the CurrentDomain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ResolveEventArgs"/> instance containing the event data.</param>
        /// <returns>Manually resolved assembly, or null.</returns>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var reader = CurrentReader.Value;

            if (reader == null)
            {
                // Use our resolvers only when actually deserializing a user object.
                // Do not interfere with any other assembly loading process that user code may have.
                return null;
            }

            return LoadedAssembliesResolver.Instance.GetAssembly(args.Name)
                   ?? PeerAssemblyResolver.LoadAssembly(args.Name, reader.Marshaller);
        }
    }
}