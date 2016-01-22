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

namespace Apache.Ignite.Linq
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Impl.Common;
    using Remotion.Linq;
    using Remotion.Linq.Parsing.Structure;

    /// <summary>
    /// 
    /// </summary>
    public class CacheQueryable<T> : QueryableBase<T>
    {
        public CacheQueryable(ICache<object, object> cache)
            : base(QueryParser.CreateDefault(), new CacheFieldsQueryExecutor(cache.QueryFields))
        {

        }

        // This constructor is called indirectly by LINQ's query methods, just pass to base.
        // TODO: ???
        public CacheQueryable(IQueryProvider provider, Expression expression) : base(provider, expression)
        {
        }
    }

    public class CacheFieldsQueryExecutor : IQueryExecutor
    {
        private readonly Func<SqlFieldsQuery, IQueryCursor<IList>> _executor;

        public CacheFieldsQueryExecutor(Func<SqlFieldsQuery, IQueryCursor<IList>> executor)
        {
            IgniteArgumentCheck.NotNull(executor, "executor");

            _executor = executor;
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            throw new System.NotImplementedException();
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            var queryData = new QueryData(); // TODO: Generate

            var query = new SqlFieldsQuery(queryData.QueryText, queryData.Parameters);

            // TODO: This will fail, need to map fields to T, which is anonymous class
            return _executor(query).OfType<T>();
        }
    }

    public class QueryData
    {
        public ICollection<object> Parameters { get; set; }

        public string QueryText { get; set; }
    }
}
