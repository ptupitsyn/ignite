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
    public interface IQueue<T> : IProducerConsumerCollection<T>
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
        /// Attempts the remove specified item from the queue.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>True if an item has been removed; otherwise, false.</returns>
        bool TryRemove(T item);

        /// <summary>
        /// Attempts the remove specified items from the queue.
        /// </summary>
        /// <param name="items">The item to remove.</param>
        /// <returns>True if items has been removed; otherwise, false.</returns>
        bool TryRemoveAll(IEnumerable<T> items);

        /// <summary>
        /// Determines whether this queue is empty.
        /// </summary>
        /// <returns>True is the queue is empty; otherwise, false.</returns>
        bool IsEmpty();

        /// <summary>
        /// Attempts to remove all items that are not present in the provided collection.
        /// </summary>
        /// <param name="items">The items to be retained.</param>
        /// <returns>True if this collection has changed as a result of the call.</returns>
        bool TryRetainAll(IEnumerable<T> items);

        /// <summary>
        /// Attempts to remove and return the object at the beginning of the queue.
        /// </summary>
        /// <param name="item">The resulting item, if the operation succeeded.</param>
        /// <returns>False if the queue was empty; otherwise, true.</returns>
        bool TryPoll(out T item);

        /// <summary>
        /// Attempts to remove and return the object at the beginning of the queue, waiting up to the
        /// specified wait time if necessary for an element to become available.
        /// </summary>
        /// <param name="item">The resulting item, if the operation succeeded.</param>
        /// <param name="timeout">The time to wait before giving up and returning false.</param>
        /// <returns>
        /// False if the queue was empty; otherwise, true.
        /// </returns>
        bool TryPoll(out T item, TimeSpan timeout);

        /// <summary>
        /// Attempts to return (but not remove) the object at the beginning of the queue.
        /// </summary>
        /// <param name="item">The resulting item, if the operation succeeded.</param>
        /// <returns>False if the queue was empty; otherwise, true.</returns>
        bool TryPeek(out T item);

        /// <summary>
        /// Removes all of the elements from this collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Clears the specified batch size.
        /// </summary>
        /// <param name="batchSize">Size of the removal batch.</param>
        void Clear(int batchSize);

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
        /// Determines whether this queue is colocated, that is, can be kept on a single node.
        /// </summary>
        /// <returns></returns>
        bool IsColocated();

        /// <summary>
        /// Returns and instance of this queue with binary mode enabled.
        /// </summary>
        /// <typeparam name="T2">The type of the queue element.</typeparam>
        /// <returns>Binary mode instance.</returns>
        IQueue<T2> WithKeepBinary<T2>();

        /// <summary>
        /// Gets the maximum number of elements in the queue.
        /// </summary>
        /// <value>
        /// Maximum number of elements in the queue.
        /// </value>
        int Capacity { get; }
    }
}
