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

namespace Apache.Ignite.Core.Impl.Cache.Client.Query
{
    using System;
    using System.Collections.Generic;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Cache.Query;
    using Apache.Ignite.Core.Impl.Client;

    /// <summary>
    /// Client query cursor.
    /// </summary>
    internal class ClientQueryCursor<T> : QueryCursorBase<T>
    {
        /** Ignite. */
        private readonly IgniteClient _ignite;

        /** Cursor ID. */
        private long _cursorId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientQueryCursor{T}" /> class.
        /// </summary>
        /// <param name="ignite">The ignite.</param>
        /// <param name="cursorId">The cursor identifier.</param>
        /// <param name="keepBinary">Keep binary flag.</param>
        public ClientQueryCursor(IgniteClient ignite, long cursorId, bool keepBinary) 
            : base(ignite.Marshaller, keepBinary)
        {
            _ignite = ignite;
            _cursorId = cursorId;
        }

        /** <inheritdoc /> */
        protected override void InitIterator()
        {
            //_ignite.Socket.
        }

        /** <inheritdoc /> */
        protected override IList<T> GetAllInternal()
        {
            throw new NotImplementedException();
        }

        /** <inheritdoc /> */
        protected override T Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        /** <inheritdoc /> */
        protected override T[] GetBatch()
        {
            throw new NotImplementedException();
        }
    }
}
