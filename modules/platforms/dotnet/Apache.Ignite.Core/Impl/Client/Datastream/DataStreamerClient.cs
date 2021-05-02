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

namespace Apache.Ignite.Core.Impl.Client.Datastream
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Apache.Ignite.Core.Client.Datastream;
    using Apache.Ignite.Core.Impl.Binary;

    /// <summary>
    ///
    /// </summary>
    internal sealed class DataStreamerClient<TK, TV> : IDataStreamerClient<TK, TV>
    {
        /** */
        private readonly ClientFailoverSocket _socket;

        /** */
        private readonly int _cacheId;

        /** */
        private readonly string _cacheName;

        /** */
        private readonly DataStreamerClientOptions<TK, TV> _options;

        /** */
        private readonly ConcurrentDictionary<ClientSocket, DataStreamerClientBuffer<TK, TV>> _buffers =
            new ConcurrentDictionary<ClientSocket, DataStreamerClientBuffer<TK, TV>>();
        
        public DataStreamerClient(ClientFailoverSocket socket, string cacheName, DataStreamerClientOptions<TK, TV> options)
        {
            Debug.Assert(socket != null);
            Debug.Assert(!string.IsNullOrEmpty(cacheName));

            _socket = socket;
            _cacheName = cacheName;
            _cacheId = BinaryUtils.GetCacheId(cacheName);

            // Copy to prevent modification.
            _options = new DataStreamerClientOptions<TK, TV>(options);
        }

        public void Dispose()
        {
            // TODO: Dispose should not throw - how can we achieve that?
            // Require Flush, like Transaction requires Commit?
            // Log errors, but don't throw?

        /** <inheritdoc /> */
        public string CacheName
        {
            get { return _cacheName; }
        }

        public DataStreamerClientOptions<TK, TV> Options
        {
            get
            {
                // Copy to prevent modification.
                return new DataStreamerClientOptions<TK, TV>(_options);
            }
        }

        public void Add(TK key, TV val)
        {
            // ClientFailoverSocket is responsible for maintaining connections and affinity logic.
            // We simply get the socket for the key.
            // TODO: Some buffers may become abandoned when a socket for them is disconnected
            // or server node leaves the cluster.
            // We should track such topology changes by subscribing to topology update events.  
            var socket = _socket.GetAffinitySocket(_cacheId, key) ?? _socket.GetSocket();
            var buffer = _buffers.GetOrAdd(socket, _ => CreateBuffer());

            while (true)
            {
                if (buffer.Add(key, val))
                {
                    return;
                }

                if (buffer.MarkForFlush())
                {
                    var oldBuffer = buffer;
                    ThreadPool.QueueUserWorkItem(_ => FlushBuffer(oldBuffer, socket));

                    buffer = CreateBuffer();
                    _buffers[socket] = buffer;
                }
            }
        }

        private void FlushBuffer(DataStreamerClientBuffer<TK, TV> buffer, ClientSocket socket)
        {
            socket.DoOutInOp(ClientOp.DataStreamerStart, ctx =>
            {
                var w = ctx.Writer;

                w.WriteInt(_cacheId);
                w.WriteByte(0x10); // Close
                w.WriteInt(_options.ServerPerNodeBufferSize); // PerNodeBufferSize
                w.WriteInt(_options.ServerPerThreadBufferSize); // PerThreadBufferSize
                w.WriteObject(_options.Receiver); // Receiver

                w.WriteInt(buffer.Count);

                foreach (var entry in buffer)
                {
                    w.WriteObjectDetached(entry.Key);

                    if (entry.Remove)
                    {
                        w.WriteObject<object>(null);
                    }
                    else
                    {
                        w.WriteObjectDetached(entry.Val);
                    }
                }
            }, ctx => ctx.Stream.ReadLong());
        }

        private DataStreamerClientBuffer<TK, TV> CreateBuffer()
        {
            return new DataStreamerClientBuffer<TK, TV>(_options.ClientPerNodeBufferSize);
        }

        public void Add(IEnumerable<KeyValuePair<TK, TV>> entries)
        {
            throw new System.NotImplementedException();
        }

        public void Remove(TK key)
        {
            throw new System.NotImplementedException();
        }

        public void Remove(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        public void Flush()
        {
            throw new System.NotImplementedException();
        }

        public Task FlushAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
