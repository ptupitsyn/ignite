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

namespace Apache.Ignite.Core.Impl.DataStructures
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Apache.Ignite.Core.DataStructures;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Unmanaged;

    /// <summary>
    /// Provides an API for working with distributed queues based on In-Memory Data Grid.
    /// </summary>
    internal class DistributedQueue<T> : PlatformTarget, IDistributedQueue<T>
    {
        /** */
        private readonly string _name;

        /** */
        private readonly bool _keepBinary;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedQueue{T}" /> class.
        /// </summary>
        /// <param name="target">Target.</param>
        /// <param name="marsh">Marshaller.</param>
        /// <param name="name">Collection name.</param>
        /// <param name="keepBinary">Binary mode flag.</param>
        public DistributedQueue(IUnmanagedTarget target, Marshaller marsh, string name, bool keepBinary) 
            : base(target, marsh)
        {
            _name = name;
            _keepBinary = keepBinary;
        }


        /** <inheritDoc /> */
        public string Name
        {
            get { return _name; }
        }

        /** <inheritDoc /> */
        public void Close()
        {
            throw new NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool IsClosed()
        {
            throw new NotImplementedException();
        }

        /** <inheritDoc /> */
        public IDistributedQueue<T2> WithKeepBinary<T2>()
        {
            if (_keepBinary)
            {
                var result = this as IDistributedQueue<T2>;

                if (result == null)
                    throw new InvalidOperationException(
                        "Can't change type of binary queue. WithKeepBinary has been called on an instance of " +
                        "binary queue with incompatible generic arguments.");

                return result;
            }

            return new DistributedQueue<T2>(Target, Marshaller, Name, true);
        }

        /** <inheritDoc /> */
        public IEnumerator<T> GetEnumerator()
        {
            IUnmanagedTarget target = null;  // TODO: Java call
            return new IgniteEnumerator<T>(target, Marshaller, _keepBinary);
        }

        /** <inheritDoc /> */
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /** <inheritDoc /> */
        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        /** <inheritDoc /> */
        public int Count
        {
            // TODO: Java call
            get { throw new NotImplementedException(); }
        }

        /** <inheritDoc /> */
        public object SyncRoot
        {
            get { return this; }
        }

        /** <inheritDoc /> */
        public bool IsSynchronized
        {
            get { return true; }
        }

        /** <inheritDoc /> */
        public void CopyTo(T[] array, int index)
        {
            // OutOp
            throw new NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool TryAdd(T item)
        {
            // OutOp
            throw new NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool TryTake(out T item)
        {
            // OutOp
            throw new NotImplementedException();
        }

        /** <inheritDoc /> */
        public T[] ToArray()
        {
            // OutOp
            throw new NotImplementedException();
        }
    }
}
