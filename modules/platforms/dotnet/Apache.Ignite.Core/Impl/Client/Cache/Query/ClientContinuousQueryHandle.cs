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

namespace Apache.Ignite.Core.Impl.Client.Cache.Query
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Client.Cache.Query.Continuous;

    /// <summary>
    /// Thin client continuous query handle.
    /// </summary>
    internal sealed class ClientContinuousQueryHandle<TK, TV> : IContinuousQueryHandleClient<ICacheEntry<TK, TV>>,
        IContinuousQueryHandleFieldsClient
    {
        /** Socket. */
        private readonly ClientSocket _socket;

        /** Keep binary flag. */
        private readonly bool _keepBinary;

        /** Cursor ID. */
        private readonly long _queryId;

        /** Columns (for fields initial query). */
        private readonly IList<string> _columns;

        /** */
        private readonly object _disposeSyncRoot = new object();

        /** */
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="ClientContinuousQueryHandle{TK, TV}"/>.
        /// </summary>
        public ClientContinuousQueryHandle(ClientSocket socket, bool keepBinary, long queryId, IList<string> columns)
        {
            _socket = socket;
            _keepBinary = keepBinary;
            _queryId = queryId;
            _columns = columns;
        }

        /** <inheritdoc /> */
        public event EventHandler<ContinuousQueryClientDisconnectedEventArgs> Disconnected;

        /** <inheritdoc /> */
        public IQueryCursor<ICacheEntry<TK, TV>> GetInitialQueryCursor()
        {
            // Initial query has the same resource id as the continuous query - do not close it on cursor dispose.
            return new ClientQueryCursor<TK, TV>(_socket, _queryId, _keepBinary, null, ClientOp.QueryScanCursorGetPage,
                closeCursorOnDispose: false);
        }

        /** <inheritdoc /> */
        IFieldsQueryCursor IContinuousQueryHandleFieldsClient.GetInitialQueryCursor()
        {
            Debug.Assert(_columns != null);

            return new ClientFieldsQueryCursor(_socket, _queryId, _keepBinary, null,
                ClientOp.QuerySqlFieldsCursorGetPage, _columns);
        }

        /** <inheritdoc /> */
        public void Dispose()
        {
            lock (_disposeSyncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                if (!_socket.IsDisposed)
                {
                    _socket.DoOutInOp<object>(ClientOp.ResourceClose,
                        ctx => ctx.Writer.WriteLong(_queryId), null);

                    // TODO: There is a race between removal and ongoing notifications
                    // It is possible to receive notifications even after ResourceClose call due to async nature
                    // of the server.
                    // TODO: Strive to fix this on the server side, otherwise we'll get a leak.
                    _socket.RemoveNotificationHandler(_queryId);
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Called when error occurs during continuous query execution.
        /// </summary>
        internal void OnError(Exception exception)
        {
            lock (_disposeSyncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                var disconnected = Disconnected;
                if (disconnected != null)
                {
                    disconnected.Invoke(this, new ContinuousQueryClientDisconnectedEventArgs(exception));
                }

                _disposed = true;
            }
        }

        /** <inheritdoc /> */
        ~ClientContinuousQueryHandle()
        {
            Dispose();
        }
    }
}
