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

namespace Apache.Ignite.Core.Impl.Client.Cache.Query
{
    using System.Collections;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Binary.IO;

    /// <summary>
    /// Client fields cursor.
    /// </summary>
    internal class ClientFieldsQueryCursor : ClientQueryCursorBase<IList>
    {
        /** Columns. */
        private readonly string[] _columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientQueryCursor{TK, TV}" /> class.
        /// </summary>
        /// <param name="ignite">The ignite.</param>
        /// <param name="cursorId">The cursor identifier.</param>
        /// <param name="keepBinary">Keep binary flag.</param>
        /// <param name="initialBatchStream">Optional stream with initial batch.</param>
        /// <param name="getPageOp">The get page op.</param>
        /// <param name="columns">The columns.</param>
        public ClientFieldsQueryCursor(IgniteClient ignite, long cursorId, bool keepBinary, 
            IBinaryStream initialBatchStream, ClientOp getPageOp, string[] columns)
            : base(ignite, cursorId, keepBinary, initialBatchStream, getPageOp)
        {
            Debug.Assert(columns != null);

            _columns = columns;
        }

        /** <inheritdoc /> */
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
        protected override IList Read(BinaryReader reader)
        {
            var res = new ArrayList(_columns.Length);

            for (var i = 0; i < _columns.Length; i++)
            {
                res.Add(reader.ReadObject<object>());
            }

            return res;
        }

        /// <summary>
        /// Reads the columns.
        /// </summary>
        internal static string[] ReadColumns(IBinaryRawReader reader)
        {
            var res = new string[reader.ReadInt()];

            for (var i = 0; i < res.Length; i++)
            {
                res[i] = reader.ReadString();
            }

            return res;
        }
    }
}
