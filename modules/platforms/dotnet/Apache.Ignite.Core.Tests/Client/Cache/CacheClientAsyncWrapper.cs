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

namespace Apache.Ignite.Core.Tests.Client.Cache
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Client.Cache;

    /// <summary>
    /// Cache client async wrapper.
    /// </summary>
    public class CacheClientAsyncWrapper<TK, TV> : ICacheClient<TK, TV>
    {
        /** */
        private readonly ICacheClient<TK, TV> _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheClientAsyncWrapper{TK, TV}"/> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        public CacheClientAsyncWrapper(ICacheClient<TK, TV> cache)
        {
            _cache = cache;
        }

        public string Name
        {
            get { return _cache.Name; }
        }

        public void Put(TK key, TV val)
        {
            _cache.Put(key, val);
        }

        public Task PutAsync(TK key, TV val)
        {
            return _cache.PutAsync(key, val);
        }

        public TV Get(TK key)
        {
            return _cache.Get(key);
        }

        public Task<TV> GetAsync(TK key)
        {
            return _cache.GetAsync(key);
        }

        public bool TryGet(TK key, out TV value)
        {
            return _cache.TryGet(key, out value);
        }

        public ICollection<ICacheEntry<TK, TV>> GetAll(IEnumerable<TK> keys)
        {
            return _cache.GetAll(keys);
        }

        public TV this[TK key]
        {
            get { return _cache[key]; }
            set { _cache[key] = value; }
        }

        public bool ContainsKey(TK key)
        {
            return _cache.ContainsKey(key);
        }

        public bool ContainsKeys(IEnumerable<TK> keys)
        {
            return _cache.ContainsKeys(keys);
        }

        public IQueryCursor<ICacheEntry<TK, TV>> Query(ScanQuery<TK, TV> scanQuery)
        {
            return _cache.Query(scanQuery);
        }

        public IQueryCursor<ICacheEntry<TK, TV>> Query(SqlQuery sqlQuery)
        {
            return _cache.Query(sqlQuery);
        }

        public IFieldsQueryCursor Query(SqlFieldsQuery sqlFieldsQuery)
        {
            return _cache.Query(sqlFieldsQuery);
        }

        public CacheResult<TV> GetAndPut(TK key, TV val)
        {
            return _cache.GetAndPut(key, val);
        }

        public CacheResult<TV> GetAndReplace(TK key, TV val)
        {
            return _cache.GetAndReplace(key, val);
        }

        public CacheResult<TV> GetAndRemove(TK key)
        {
            return _cache.GetAndRemove(key);
        }

        public bool PutIfAbsent(TK key, TV val)
        {
            return _cache.PutIfAbsent(key, val);
        }

        public CacheResult<TV> GetAndPutIfAbsent(TK key, TV val)
        {
            return _cache.GetAndPutIfAbsent(key, val);
        }

        public bool Replace(TK key, TV val)
        {
            return _cache.Replace(key, val);
        }

        public bool Replace(TK key, TV oldVal, TV newVal)
        {
            return _cache.Replace(key, oldVal, newVal);
        }

        public void PutAll(IEnumerable<KeyValuePair<TK, TV>> vals)
        {
            _cache.PutAll(vals);
        }

        public Task PutAllAsync(IEnumerable<KeyValuePair<TK, TV>> vals)
        {
            return _cache.PutAllAsync(vals);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public void Clear(TK key)
        {
            _cache.Clear(key);
        }

        public void ClearAll(IEnumerable<TK> keys)
        {
            _cache.ClearAll(keys);
        }

        public bool Remove(TK key)
        {
            return _cache.Remove(key);
        }

        public bool Remove(TK key, TV val)
        {
            return _cache.Remove(key, val);
        }

        public void RemoveAll(IEnumerable<TK> keys)
        {
            _cache.RemoveAll(keys);
        }

        public void RemoveAll()
        {
            _cache.RemoveAll();
        }

        public long GetSize(params CachePeekMode[] modes)
        {
            return _cache.GetSize(modes);
        }

        public CacheClientConfiguration GetConfiguration()
        {
            return _cache.GetConfiguration();
        }

        public ICacheClient<TK1, TV1> WithKeepBinary<TK1, TV1>()
        {
            return _cache.WithKeepBinary<TK1, TV1>();
        }

        /// <summary>
        /// Waits the result of a task, unwraps exceptions.
        /// </summary>
        /// <param name="task">The task.</param>
        private static void WaitResult(Task task)
        {
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        /// <summary>
        /// Gets the result of a task, unwraps exceptions.
        /// </summary>
        private static T GetResult<T>(Task<T> task)
        {
            try
            {
                return task.Result;
            }
            catch (Exception ex)
            {
                throw ex.InnerException ?? ex;
            }
        }
    }
}