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
    using System.Collections.Generic;
    using System.Diagnostics;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary.Metadata;
    using Apache.Ignite.Core.Impl.Client;

    /// <summary>
    /// Thin client binary processor.
    /// </summary>
    internal class BinaryProcessorClient : IBinaryProcessor
    {
        /** Socket. */
        private readonly ClientSocket _socket;

        /** Marshaller. */
        private readonly Marshaller _marsh = BinaryUtils.Marshaller;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryProcessorClient"/> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        public BinaryProcessorClient(ClientSocket socket)
        {
            Debug.Assert(socket != null);

            _socket = socket;
        }

        /** <inheritdoc /> */
        public BinaryType GetBinaryType(int typeId)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritdoc /> */
        public List<IBinaryType> GetBinaryTypes()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritdoc /> */
        public int[] GetSchema(int typeId, int schemaId)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritdoc /> */
        public void PutBinaryTypes(ICollection<BinaryType> types)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritdoc /> */
        public bool RegisterType(int id, string typeName)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritdoc /> */
        public BinaryType RegisterEnum(string typeName, IEnumerable<KeyValuePair<string, int>> values)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritdoc /> */
        public string GetTypeName(int id)
        {
            return _socket.DoOutInOp(ClientOp.BinaryProcessorGetTypeName, w => w.WriteInt(id),
                s => _marsh.StartUnmarshal(s).ReadString());
        }
    }
}