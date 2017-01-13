﻿/*
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
    using System.Reflection;

    /// <summary>
    /// Loads assemblies from other nodes.
    /// </summary>
    internal static class PeerAssemblyResolver
    {
        /// <summary>
        /// Gets the assembly from remote nodes.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="marshaller">The marshaller.</param>
        /// <returns>Peer-loaded assembly or null.</returns>
        public static Assembly GetAssembly(string assemblyName, string typeName, Marshaller marshaller)
        {
            // TODO: We may get type name or assembly name!
            Debug.Assert(!string.IsNullOrWhiteSpace(assemblyName));
            Debug.Assert(marshaller != null);

            var ignite = marshaller.Ignite;
            Debug.Assert(ignite != null);

            if (!ignite.Configuration.IsPeerAssemblyLoadingEnabled)
                return null;

            // TODO: Track new nodes? Not sure if this makes sense, since some of the old nodes caused this call.
            var dotNetNodes = ignite.GetCluster().ForDotNet().ForRemotes().GetNodes();
            var func = new GetAssemblyFunc();
            var req = new AssemblyRequest(assemblyName, typeName);

            foreach (var node in dotNetNodes)
            {
                var compute = ignite.GetCluster().ForNodes(node).GetCompute();

                // TODO: What if the node leaves in process? We should handle this. Add test.
                var result = compute.Apply(func, req);

                if (result != null && result.AssemblyBytes != null)
                {
                    return AssemblyLoader.LoadAssembly(result.AssemblyBytes);
                }

                // TODO: Handle error messages
            }

            // TODO: Cache non-resolvable types (per Ignite instance).
            return null;
        }
    }
}
