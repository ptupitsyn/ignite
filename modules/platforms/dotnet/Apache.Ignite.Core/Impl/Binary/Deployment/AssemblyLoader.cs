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
        private static readonly CopyOnWriteConcurrentDictionary<Assembly, byte[]> InMemoryAssemblies 
            = new CopyOnWriteConcurrentDictionary<Assembly, byte[]>();

        /// <summary>
        /// Loads the assembly from bytes.
        /// </summary>
        public static Assembly LoadAssembly(byte[] bytes)
        {
            Debug.Assert(bytes != null);

            // TODO: We must be sure to never load the same assembly twice, because it leads to
            // InvalidCastException with message like "Type A originates from <>. Type A originates from <>."

            // Even though we synchronize peer loading in marshaller, there can be multiple nodes.
            var asm = Assembly.Load(bytes);

            InMemoryAssemblies.GetOrAdd(asm, _ => bytes);

            return asm;
        }

        /// <summary>
        /// Gets the assembly bytes.
        /// </summary>
        public static byte[] GetAssemblyBytes(Assembly assembly)
        {
            Debug.Assert(assembly != null);
            Debug.Assert(!assembly.IsDynamic);

            byte[] bytes;

            if (InMemoryAssemblies.TryGetValue(assembly, out bytes))
                return bytes;

            if (string.IsNullOrEmpty(assembly.Location))
                return null;

            return File.ReadAllBytes(assembly.Location);
        }
    }
}
