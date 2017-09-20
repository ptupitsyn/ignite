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

namespace Apache.Ignite.Core.Client.Cache
{
    using System.Collections.Generic;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Query;

    /// <summary>
    /// Client cache API. See <see cref="IIgniteClient.GetCache{K, V}"/>.
    /// </summary>
    // ReSharper disable once TypeParameterCanBeVariant (ICache shoul not be variant, more methods will be added)
    public interface ICacheClient<TK, TV>
    {
        /// <summary>
        /// Name of this cache (<c>null</c> for default cache).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Associates the specified value with the specified key in the cache.
        /// <para />
        /// If the cache previously contained a mapping for the key,
        /// the old value is replaced by the specified value.
        /// </summary>
        /// <param name="key">Key with which the specified value is to be associated.</param>
        /// <param name="val">Value to be associated with the specified key.</param>
        void Put(TK key, TV val);

        /// <summary>
        /// Retrieves value mapped to the specified key from cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value.</returns>
        /// <exception cref="KeyNotFoundException">If the key is not present in the cache.</exception>
        TV Get(TK key);

        /// <summary>
        /// Retrieves value mapped to the specified key from cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">When this method returns, the value associated with the specified key,
        /// if the key is found; otherwise, the default value for the type of the value parameter.
        /// This parameter is passed uninitialized.</param>
        /// <returns>
        /// true if the cache contains an element with the specified key; otherwise, false.
        /// </returns>
        bool TryGet(TK key, out TV value);

        /// <summary>
        /// Gets or sets a cache value with the specified key.
        /// Shortcut to <see cref="Get"/> and <see cref="Put"/>
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Cache value with the specified key.</returns>
        /// <exception cref="KeyNotFoundException">If the key is not present in the cache.</exception>
        TV this[TK key] { get; set; }

        /// <summary>
        /// Check if cache contains mapping for this key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>True if cache contains mapping for this key.</returns>
        bool ContainsKey(TK key);

        /// <summary>
        /// Check if cache contains mapping for these keys.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <returns>True if cache contains mapping for all these keys.</returns>
        bool ContainsKeys(IEnumerable<TK> keys);

        /// <summary>
        /// Executes a Scan query.
        /// </summary>
        /// <param name="scanQuery">Scan query.</param>
        /// <returns>Query cursor.</returns>
        IQueryCursor<ICacheEntry<TK, TV>> Query(ScanQuery<TK, TV> scanQuery);
    }
}
