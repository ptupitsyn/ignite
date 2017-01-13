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
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Common;
    using Apache.Ignite.Core.Compute;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Compute func that returns assembly for a specified name.
    /// </summary>
    internal class GetAssemblyFunc : IComputeFunc<AssemblyRequest, AssemblyRequestResult>, IBinaryWriteAware
    {
        /** Marshaller. */
        private readonly Marshaller _marshaller;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAssemblyFunc"/> class.
        /// </summary>
        /// <param name="marshaller">The marshaller.</param>
        public GetAssemblyFunc(Marshaller marshaller)
        {
            Debug.Assert(marshaller != null);

            _marshaller = marshaller;
        }

        /** <inheritdoc /> */
        public AssemblyRequestResult Invoke(AssemblyRequest arg)
        {
            // TODO: Return an object with status code and error messages.
            if (arg == null)
                throw new IgniteException("GetAssemblyFunc does not allow null arguments.");

            if (arg.AssemblyName != null)
            {
                var asm = LoadedAssembliesResolver.Instance.GetAssembly(arg.AssemblyName);

                if (asm == null)
                    return null;

                // Dynamic assemblies are not supported.
                if (asm.IsDynamic)
                {
                    return new AssemblyRequestResult(
                        "Peer assembly loading does not support dynamic assemblies: " + asm);
                }

                return new AssemblyRequestResult(AssemblyLoader.GetAssemblyBytes(asm));
            }

            if (arg.TypeId != null)
            {
                var desc = _marshaller.GetDescriptor(true, arg.TypeId.Value);

                if (desc == null || desc.Type == null)
                    return null;

                var type = desc.Type;

                // Dynamic assemblies are not supported.
                if (type.Assembly.IsDynamic)
                {
                    return new AssemblyRequestResult(
                        "Peer assembly loading does not support dynamic assemblies: " + type.Assembly);
                }

                return new AssemblyRequestResult(AssemblyLoader.GetAssemblyBytes(type.Assembly), type.FullName);
            }

            throw new IgniteException("AssemblyName and TypeId are null in AssemblyRequest.");
        }

        /** <inheritdoc /> */
        public void WriteBinary(IBinaryWriter writer)
        {
            // No-op.
        }
    }
}
