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
    using System.Threading.Tasks;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Client data streamer buffer.
    /// </summary>
    internal sealed class DataStreamerClientBuffer<TK, TV> : IEnumerable<DataStreamerClientEntry<TK, TV>>
    {
        // TODO: try other collections?
        // TODO: Use an array - we can safely populate it at given index, and we know the size upfront!
        /** Concurrent bag already has per-thread buffers. */
        private readonly ConcurrentBag<DataStreamerClientEntry<TK, TV>> _entries =
            new ConcurrentBag<DataStreamerClientEntry<TK, TV>>();

        /** */
        private readonly int _maxSize;

        /** */
        private readonly TaskCompletionSource<object> _flushCompletionSource = new TaskCompletionSource<object>();

        /** */
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        /** */
        private readonly Task _flushTask;

        /** */
        private readonly DataStreamerClientPerNodeBuffer<TK,TV> _parent;

        /** */
        private long _size;

        /** */
        private volatile bool _flushing;

        public DataStreamerClientBuffer(
            int maxSize,
            DataStreamerClientPerNodeBuffer<TK, TV> parent,
            DataStreamerClientBuffer<TK, TV> previous)
        {
            Debug.Assert(maxSize > 0);
            Debug.Assert(parent != null);

            _maxSize = maxSize;
            _parent = parent;
            _flushTask = previous == null || previous.FlushTask.IsCompleted
                ? _flushCompletionSource.Task
                : TaskRunner.WhenAll(new[] {previous.FlushTask, _flushCompletionSource.Task});
        }

        public int Count
        {
            get { return _entries.Count; }
        }

        public Task FlushTask
        {
            get { return _flushTask; }
        }

        public bool Add(DataStreamerClientEntry<TK, TV> entry)
        {
            var newSize = Interlocked.Increment(ref _size);
            if (newSize > _maxSize)
            {
                return false;
            }

            if (!_rwLock.TryEnterReadLock(0))
                return false;

            try
            {
                if (_flushing)
                {
                    return false;
                }

                _entries.Add(entry);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            if (newSize == _maxSize)
            {
                TryRunFlushAction();
            }

            return true;
        }

        /// <summary>
        /// Returns true if flushing has started as a result of this call or before that.
        /// </summary>
        public bool ScheduleFlush()
        {
            if (Interlocked.CompareExchange(ref _size, -1, -1) == 0)
            {
                // Empty buffer.
                return false;
            }

            TryRunFlushAction();

            return true;
        }

        public IEnumerator<DataStreamerClientEntry<TK, TV>> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void TryRunFlushAction()
        {
            _rwLock.EnterWriteLock();

            try
            {
                if (_flushing)
                {
                    return;
                }

                _flushing = true;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }

            // Run outside of the lock - reduce possible contention.
            RunFlushAction();
        }

        private void RunFlushAction()
        {
            // TODO: Thread pool? - seems to reduce multithreaded performance.
            // TODO: Error handling
            // ThreadPool.QueueUserWorkItem(__ =>
            // {

            _parent.FlushAsync(this).ContinueWith(
                    _ =>
                    {
                        // TODO: This runs on socket thread - be careful with completions
                        return ThreadPool.QueueUserWorkItem(__ => _flushCompletionSource.TrySetResult(null));
                    },
                    TaskContinuationOptions.ExecuteSynchronously);
            // });
        }
    }
}
