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
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Peer assembly request.
    /// </summary>
    internal class AssemblyRequest : IBinaryWriteAware
    {
        /** */
        private readonly string _assemblyName;

        /** */
        private readonly int? _typeId;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyRequest"/> class.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="typeId">Type id.</param>
        public AssemblyRequest(string assemblyName, int? typeId)
        {
            IgniteArgumentCheck.Ensure(assemblyName != null || typeId != null, 
                "assemblyName", "AssemblyRequest must have either type of assembly specified");

            _assemblyName = assemblyName;
            _typeId = typeId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyRequest"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public AssemblyRequest(IBinaryRawReader reader)
        {
            _assemblyName = reader.ReadString();
            _typeId = reader.ReadIntNullable();
        }

        /** <inheritdoc /> */
        public void WriteBinary(IBinaryWriter writer)
        {
            var raw = writer.GetRawWriter();

            raw.WriteString(_assemblyName);
            raw.WriteIntNullable(_typeId);
        }

        /// <summary>
        /// Gets the name of the assembly.
        /// </summary>
        public string AssemblyName
        {
            get { return _assemblyName; }
        }

        /// <summary>
        /// Gets the type id.
        /// </summary>
        public int? TypeId
        {
            get { return _typeId; }
        }
    }
}
