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
    /// <summary>
    /// Provides an API for working with distributed queues based on In-Memory Data Grid.
    /// </summary>
    public class DistributedQueue<T> : IDistributedQueue<T>
    {
        /** <inheritDoc /> */
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
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
            get { throw new NotImplementedException(); }
        }

        /** <inheritDoc /> */
        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        /** <inheritDoc /> */
        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        /** <inheritDoc /> */
        public void CopyTo(T[] array, int index)
        {
            throw new NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool TryAdd(T item)
        {
            throw new NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool TryTake(out T item)
        {
            throw new NotImplementedException();
        }

        /** <inheritDoc /> */
        public T[] ToArray()
        {
            throw new NotImplementedException();
        }
    }
}
