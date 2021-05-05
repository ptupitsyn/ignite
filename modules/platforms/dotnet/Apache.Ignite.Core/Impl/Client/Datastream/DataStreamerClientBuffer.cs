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
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Apache.Ignite.Core.Client;
    using Apache.Ignite.Core.Client.Datastream;
    using Apache.Ignite.Core.Common;

    /// <summary>
    /// Client data streamer buffer.
    /// </summary>
    internal sealed class DataStreamerClientBuffer<TK, TV> : IEnumerable<DataStreamerClientEntry<TK, TV>>
    {
        /** */
        private readonly DataStreamerClientOptions<TK, TV> _options;

        /** Concurrent bag already has per-thread buffers. */
        private readonly ConcurrentBag<DataStreamerClientEntry<TK, TV>> _entries =
            new ConcurrentBag<DataStreamerClientEntry<TK, TV>>();

        /** */
        private readonly ReaderWriterLockSlim _flushLock = new ReaderWriterLockSlim();

        /** */
        private int _size;

        /** */
        private volatile bool _flushing;

        public DataStreamerClientBuffer(DataStreamerClientOptions<TK, TV> options)
        {
            Debug.Assert(options != null);

            _options = options;
        }

        public int Count
        {
            get { return _entries.Count; }
        }

        public bool Add(TK key, TV val)
        {
            return val == null
                ? Remove(key)
                : Add(new DataStreamerClientEntry<TK, TV>(key, val));
        }

        public bool Remove(TK key)
        {
            if (!_options.AllowOverwrite)
            {
                throw new IgniteClientException("DataStreamer can't remove data when AllowOverwrite is false.");
            }

            return Add(new DataStreamerClientEntry<TK, TV>(key));
        }

        public bool MarkForFlush()
        {
            if (_flushing)
            {
                return false;
            }

            _flushLock.EnterWriteLock();

            try
            {
                if (_flushing)
                {
                    return false;
                }

                _flushing = true;
                return true;
            }
            finally
            {
                _flushLock.ExitWriteLock();
            }
        }

        public IEnumerator<DataStreamerClientEntry<TK, TV>> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool Add(DataStreamerClientEntry<TK, TV> entry)
        {
            if (Interlocked.Increment(ref _size) > _options.ClientPerNodeBufferSize)
            {
                return false;
            }

            if (!_flushLock.TryEnterReadLock(0))
            {
                return false;
            }

            try
            {
                if (_flushing)
                {
                    return false;
                }

                _entries.Add(entry);

                return true;
            }
            finally
            {
                _flushLock.ExitReadLock();
            }
        }
    }
}
