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

namespace Apache.Ignite.Core.Impl.Binary.Deployment
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Handles assembly loading and serialization.
    /// </summary>
    internal static class AssemblyLoader
    {
        /// <summary>
        /// Cache of assemblies that are peer-loaded from byte array.
        /// Keep these byte arrays to be able to send them further, because Location for such assemblies is empty.
        /// </summary>
        private static readonly CopyOnWriteConcurrentDictionary<string, KeyValuePair<Assembly, byte[]>>
            InMemoryAssemblies
                = new CopyOnWriteConcurrentDictionary<string, KeyValuePair<Assembly, byte[]>>();

        /// <summary>
        /// Loads the assembly from bytes.
        /// </summary>
        public static Assembly LoadAssembly(byte[] bytes, string assemblyName)
        {
            Debug.Assert(bytes != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(assemblyName));

            // TODO: We must be sure to never load the same assembly twice, because it leads to
            // InvalidCastException with message like "Type A originates from <>. Type A originates from <>."

            // TODO: We should not use Load(byte[]) overload, because it loads an assembly outside any context.
            // BUT can we use LoadFrom to load same assembly with different version?
            // Instead, we should probably use LoadFrom: https://msdn.microsoft.com/en-us/library/1009fa28(v=vs.110).aspx
            // This one suits us well:
            // * If an assembly with the same identity is already loaded, LoadFrom returns the loaded assembly even if a different path was specified.

            // Even though we synchronize peer loading in marshaller, there can be multiple nodes.

            return InMemoryAssemblies.GetOrAdd(assemblyName, _ =>
            {
                var asm = Assembly.Load(bytes);

                Debug.Assert(assemblyName == asm.FullName);

                return new KeyValuePair<Assembly, byte[]>(asm, bytes);
            }).Key;
        }

        /// <summary>
        /// Gets the assembly bytes.
        /// </summary>
        public static byte[] GetAssemblyBytes(Assembly assembly)
        {
            Debug.Assert(assembly != null);
            Debug.Assert(!assembly.IsDynamic);

            KeyValuePair<Assembly, byte[]> pair;

            if (InMemoryAssemblies.TryGetValue(assembly.FullName, out pair))
                return pair.Value;

            if (string.IsNullOrEmpty(assembly.Location))
                return null;

            return File.ReadAllBytes(assembly.Location);
        }
    }
}
