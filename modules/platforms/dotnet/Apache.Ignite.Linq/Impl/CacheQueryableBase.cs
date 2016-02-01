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

namespace Apache.Ignite.Linq.Impl
{
    using System.Linq;
    using System.Linq.Expressions;
    using Apache.Ignite.Core;
    using Remotion.Linq;

    /// <summary>
    /// Base class for cache queryables.
    /// </summary>
    internal class CacheQueryableBase<T> : QueryableBase<T>, ICacheQueryable
    {
        /** <inheritdoc /> */
        public CacheQueryableBase(IQueryProvider provider) : base(provider)
        {
            // No-op.
        }

        /** <inheritdoc /> */
        public CacheQueryableBase(IQueryProvider provider, Expression expression) : base(provider, expression)
        {
            // No-op.
        }

        /** <inheritdoc /> */
        public string CacheName
        {
            get { return CacheQueryProvider.CacheName; }
        }

        /** <inheritdoc /> */
        public IIgnite Ignite
        {
            get { return CacheQueryProvider.Ignite; }
        }

        /// <summary>
        /// Gets the cache query provider.
        /// </summary>
        private CacheFieldsQueryProvider CacheQueryProvider
        {
            get { return (CacheFieldsQueryProvider)Provider; }
        }

        /** <inheritdoc /> */
        public string ToTraceString()
        {
            var model = CacheQueryProvider.GenerateQueryModel(Expression);

            return ((ICacheQueryExecutor)CacheQueryProvider.Executor).GetQueryData(model).ToString();
        }
    }
}