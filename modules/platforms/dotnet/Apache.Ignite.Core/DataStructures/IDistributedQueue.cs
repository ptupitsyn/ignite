﻿/*
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
    using System.Collections.Concurrent;

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
        /// Returns and instance of this queue with binary mode enabled.
        /// </summary>
        /// <typeparam name="T2">The type of the queue element.</typeparam>
        /// <returns>Binary mode instance.</returns>
        IDistributedQueue<T2> WithKeepBinary<T2>();
    }
}
