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

namespace Apache.Ignite.Core.DataStructures
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// Provides an API for working with distributed queues based on In-Memory Data Grid.
    /// </summary>
    public interface IDistributedQueue<T> : IProducerConsumerCollection<T>
    {
        /// <summary>
        /// Gets the queue name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Attempts to add an item.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="timeout">The timeout: how long to wait before giving up and returning false.</param>
        /// <returns>true if the item was added successfully; otherwise, false.</returns>
        bool TryAdd(T item, TimeSpan timeout);

        /// <summary>
        /// Adds multiple items.
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <returns>true if items were added successfully; otherwise, false.</returns>
        bool TryAddAll(IEnumerable<T> items);

        /// <summary>
        /// Determines whether this collection contains specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if this collection contains specified item; otherwise, false.</returns>
        bool Contains(T item);

        /// <summary>
        /// Determines whether this collection contains specified items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>True if this collection contains specified items; otherwise, false.</returns>
        bool ContainsAll(IEnumerable<T> items);

        /// <summary>
        /// Removes all elements from this collection.
        /// </summary>
        void Clear();

        bool Remove(T item);
        bool RemoveAll(IEnumerable<T> items);
        bool IsEmpty();
        bool RetainAll(IEnumerable<T> items);

        /// <summary>
        /// Closes this instance and removes data from the grid.
        /// </summary>
        void Close();

        /// <summary>
        /// Determines whether this instance is closed.
        /// </summary>
        /// <returns>True if corresponding queue has been closed; otherwise, false.</returns>
        bool IsClosed();

        /// <summary>
        /// Returns and instance of this queue with binary mode enabled.
        /// </summary>
        /// <typeparam name="T2">The type of the queue element.</typeparam>
        /// <returns>Binary mode instance.</returns>
        IDistributedQueue<T2> WithKeepBinary<T2>();
    }
}
