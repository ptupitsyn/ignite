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

namespace Apache.Ignite.Core.Impl.Cache
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Cache.Expiry;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Cache.Query.Continuous;
    using Apache.Ignite.Core.Cluster;
    using Apache.Ignite.Core.Impl.Client;

    /// <summary>
    /// Client cache implementation.
    /// </summary>
    internal class CacheClient<TK, TV> : ICache<TK, TV>
    {
        /** Socket. */
        private readonly ClientSocket _socket;

        /** Cache name. */
        private string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheClient{TK, TV}" /> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="name">Cache name.</param>
        public CacheClient(ClientSocket socket, string name)
        {
            Debug.Assert(socket != null);
            Debug.Assert(name != null);

            _socket = socket;
            _name = name;
        }

        /** <inheritDoc /> */
        public IEnumerator<ICacheEntry<TK, TV>> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /** <inheritDoc /> */
        public string Name
        {
            get { throw new System.NotImplementedException(); }
        }

        /** <inheritDoc /> */
        public IIgnite Ignite
        {
            get { throw new System.NotImplementedException(); }
        }

        /** <inheritDoc /> */
        public CacheConfiguration GetConfiguration()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool IsEmpty()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool IsKeepBinary
        {
            get { throw new System.NotImplementedException(); }
        }

        /** <inheritDoc /> */
        public ICache<TK, TV> WithSkipStore()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICache<TK, TV> WithExpiryPolicy(IExpiryPolicy plc)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICache<TK1, TV1> WithKeepBinary<TK1, TV1>()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void LoadCache(ICacheEntryFilter<TK, TV> p, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task LoadCacheAsync(ICacheEntryFilter<TK, TV> p, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void LocalLoadCache(ICacheEntryFilter<TK, TV> p, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task LocalLoadCacheAsync(ICacheEntryFilter<TK, TV> p, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void LoadAll(IEnumerable<TK> keys, bool replaceExistingValues)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task LoadAllAsync(IEnumerable<TK> keys, bool replaceExistingValues)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool ContainsKey(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<bool> ContainsKeyAsync(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool ContainsKeys(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<bool> ContainsKeysAsync(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public TV LocalPeek(TK key, params CachePeekMode[] modes)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool TryLocalPeek(TK key, out TV value, params CachePeekMode[] modes)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public TV this[TK key]
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        /** <inheritDoc /> */
        public TV Get(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<TV> GetAsync(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool TryGet(TK key, out TV value)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<CacheResult<TV>> TryGetAsync(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICollection<ICacheEntry<TK, TV>> GetAll(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<ICollection<ICacheEntry<TK, TV>>> GetAllAsync(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void Put(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task PutAsync(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public CacheResult<TV> GetAndPut(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<CacheResult<TV>> GetAndPutAsync(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public CacheResult<TV> GetAndReplace(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<CacheResult<TV>> GetAndReplaceAsync(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public CacheResult<TV> GetAndRemove(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<CacheResult<TV>> GetAndRemoveAsync(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool PutIfAbsent(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<bool> PutIfAbsentAsync(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public CacheResult<TV> GetAndPutIfAbsent(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<CacheResult<TV>> GetAndPutIfAbsentAsync(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool Replace(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<bool> ReplaceAsync(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool Replace(TK key, TV oldVal, TV newVal)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<bool> ReplaceAsync(TK key, TV oldVal, TV newVal)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void PutAll(IEnumerable<KeyValuePair<TK, TV>> vals)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task PutAllAsync(IEnumerable<KeyValuePair<TK, TV>> vals)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void LocalEvict(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void Clear()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task ClearAsync()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void Clear(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task ClearAsync(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void ClearAll(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task ClearAllAsync(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void LocalClear(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void LocalClearAll(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool Remove(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<bool> RemoveAsync(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool Remove(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<bool> RemoveAsync(TK key, TV val)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void RemoveAll(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task RemoveAllAsync(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public void RemoveAll()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task RemoveAllAsync()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public int GetLocalSize(params CachePeekMode[] modes)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public int GetSize(params CachePeekMode[] modes)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<int> GetSizeAsync(params CachePeekMode[] modes)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public IQueryCursor<ICacheEntry<TK, TV>> Query(QueryBase qry)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public IQueryCursor<IList> QueryFields(SqlFieldsQuery qry)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public IContinuousQueryHandle QueryContinuous(ContinuousQuery<TK, TV> qry)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public IContinuousQueryHandle<ICacheEntry<TK, TV>> QueryContinuous(ContinuousQuery<TK, TV> qry, QueryBase initialQry)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public IEnumerable<ICacheEntry<TK, TV>> GetLocalEntries(params CachePeekMode[] peekModes)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public TRes Invoke<TArg, TRes>(TK key, ICacheEntryProcessor<TK, TV, TArg, TRes> processor, TArg arg)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<TRes> InvokeAsync<TArg, TRes>(TK key, ICacheEntryProcessor<TK, TV, TArg, TRes> processor, TArg arg)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICollection<ICacheEntryProcessorResult<TK, TRes>> InvokeAll<TArg, TRes>(IEnumerable<TK> keys, ICacheEntryProcessor<TK, TV, TArg, TRes> processor, TArg arg)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task<ICollection<ICacheEntryProcessorResult<TK, TRes>>> InvokeAllAsync<TArg, TRes>(IEnumerable<TK> keys, ICacheEntryProcessor<TK, TV, TArg, TRes> processor, TArg arg)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICacheLock Lock(TK key)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICacheLock LockAll(IEnumerable<TK> keys)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public bool IsLocalLocked(TK key, bool byCurrentThread)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICacheMetrics GetMetrics()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICacheMetrics GetMetrics(IClusterGroup clusterGroup)
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICacheMetrics GetLocalMetrics()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public Task Rebalance()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICache<TK, TV> WithNoRetries()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICache<TK, TV> WithPartitionRecover()
        {
            throw new System.NotImplementedException();
        }

        /** <inheritDoc /> */
        public ICollection<int> GetLostPartitions()
        {
            throw new System.NotImplementedException();
        }
    }
}
