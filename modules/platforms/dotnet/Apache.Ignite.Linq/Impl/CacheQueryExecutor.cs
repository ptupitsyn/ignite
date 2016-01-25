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

namespace Apache.Ignite.Linq.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Impl.Common;
    using Remotion.Linq;

    /// <summary>
    /// Cache query executor.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    internal class CacheQueryExecutor<TKey, TValue> : IQueryExecutor
    {
        /** */
        private readonly ICache<TKey, TValue> _cache;

        /** */
        private readonly string _queryTypeName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheQueryExecutor{TKey, TValue}" /> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="queryTypeName">Name of the query type.</param>
        public CacheQueryExecutor(ICache<TKey, TValue> cache, string queryTypeName)
        {
            Debug.Assert(cache != null);
            Debug.Assert(!string.IsNullOrEmpty(queryTypeName));

            _cache = cache;
            _queryTypeName = queryTypeName;
        }

        /** <inheritdoc /> */
        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            // TODO
            throw new System.NotImplementedException();
        }

        /** <inheritdoc /> */
        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            // TODO
            throw new System.NotImplementedException();
        }

        /** <inheritdoc /> */
        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            ValidateCacheConfiguration();

            // Check for empty query
            if (queryModel.IsIdentityQuery() && queryModel.ResultOperators.Count == 0)
                return (IEnumerable<T>) _cache;

            var queryData = CacheQueryModelVisitor.GenerateQuery(queryModel, _cache);

            //var query = new SqlFieldsQuery(queryData.QueryText, queryData.Parameters);

            var query = new SqlQuery(_queryTypeName, queryData.QueryText, queryData.Parameters.ToArray());

            return (IEnumerable<T>) _cache.Query(query);
        }

        private void ValidateCacheConfiguration()
        {
            var config = _cache.GetConfiguration();

            if (config.QueryEntities == null || config.QueryEntities.Count == 0)
            {
                throw new CacheException(string.Format("Cache '{0}' is not configured for queries.",
                    _cache.Name ?? "null"));
            }

            if (!config.QueryEntities.Any(
                e => _queryTypeName.Equals(e.ValueTypeName, StringComparison.InvariantCultureIgnoreCase) ||
                     _queryTypeName.Equals(e.KeyTypeName, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new CacheException(string.Format("Cache '{0}' does not have query entity '{1}' configured.",
                    _cache.Name ?? "null", _queryTypeName));
            }
        }
    }
}