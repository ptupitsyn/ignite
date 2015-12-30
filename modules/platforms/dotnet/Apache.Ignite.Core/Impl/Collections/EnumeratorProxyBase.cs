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

namespace Apache.Ignite.Core.Impl.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Base enumerator proxy. Required to support reset and early native iterator cleanup.
    /// </summary>
    internal abstract class EnumeratorProxyBase<T> : IEnumerator<T>
    {
        /** Target enumerator. */
        private IEnumerator<T> _target;

        /** Dispose flag. */
        private bool _disposed;

        /** <inheritdoc /> */
        public bool MoveNext()
        {
            CheckDisposed();

            // No target => closed or finished.
            if (_target == null)
                return false;

            if (!_target.MoveNext())
            {
                // Failed to advance => end is reached.
                CloseTarget();

                return false;
            }

            return true;
        }

        /** <inheritdoc /> */
        public T Current
        {
            get
            {
                CheckDisposed();

                if (_target == null)
                    throw new InvalidOperationException("Invalid enumerator state (did you call MoveNext()?)");

                return _target.Current;
            }
        }

        /** <inheritdoc /> */
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /** <inheritdoc /> */
        public void Reset()
        {
            CheckDisposed();

            if (_target != null)
                CloseTarget();

            CreateTarget();
        }

        /** <inheritdoc /> */
        [SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly",
            Justification = "There is no finalizer.")]
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_target != null)
                    CloseTarget();

                _disposed = true;
            }
        }

        /// <summary>
        /// Get target enumerator.
        /// </summary>
        /// <returns>Target enumerator.</returns>
        protected void CreateTarget()
        {
            Debug.Assert(_target == null, "Previous target is not cleaned.");

            _target = GetEnumerator();
        }

        /// <summary>
        /// Gets underlying enumerator enumerator.
        /// </summary>
        /// <returns>Target enumerator.</returns>
        protected abstract IEnumerator<T> GetEnumerator();

        /// <summary>
        /// Close the target.
        /// </summary>
        private void CloseTarget()
        {
            Debug.Assert(_target != null);

            _target.Dispose();

            _target = null;
        }

        /// <summary>
        /// Check whether object is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("Cache enumerator has been disposed.");
        }
    }
}