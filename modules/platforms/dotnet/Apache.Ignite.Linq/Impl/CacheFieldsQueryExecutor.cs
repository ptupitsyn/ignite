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
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Impl.Cache;
    using Apache.Ignite.Core.Impl.Common;
    using Remotion.Linq;

    /// <summary>
    /// Fields query executor.
    /// </summary>
    internal class CacheFieldsQueryExecutor : IQueryExecutor
    {
        /** */
        private readonly ICacheQueryProxy _cache;

        /** */
        private static readonly CopyOnWriteConcurrentDictionary<ConstructorInfo, object> CtorCache =
            new CopyOnWriteConcurrentDictionary<ConstructorInfo, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheFieldsQueryExecutor" /> class.
        /// </summary>
        /// <param name="cache">The executor function.</param>
        public CacheFieldsQueryExecutor(ICacheQueryProxy cache)
        {
            Debug.Assert(cache != null);

            _cache = cache;
        }

        /** <inheritdoc /> */
        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            return ExecuteSingle<T>(queryModel, false);
        }

        /** <inheritdoc /> */
        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            var collection = ExecuteCollection<T>(queryModel);

            return returnDefaultWhenEmpty ? collection.SingleOrDefault() : collection.Single();
        }

        /** <inheritdoc /> */
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            var queryData = GetQueryData(queryModel);

            Debug.WriteLine("\nFields Query: {0} | {1}", queryData.QueryText,
                string.Join(", ", queryData.Parameters.Select(x => x == null ? "null" : x.ToString())));

            var query = new SqlFieldsQuery(queryData.QueryText, queryData.Parameters.ToArray());

            var selector = GetResultSelector<T>(queryModel.SelectClause.Selector);

            var queryCursor = _cache.QueryFields(query, selector);

            return queryCursor;
        }

        /// <summary>
        /// Compiles the query.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
        public Func<object[], IQueryCursor<T>> CompileQuery<T>(QueryModel queryModel, Expression queryCaller)
        {
            var queryData = GetQueryData(queryModel);

            var queryText = queryData.QueryText;

            var selector = GetResultSelector<T>(queryModel.SelectClause.Selector);

            if (queryCaller == null)
                return args => _cache.QueryFields(new SqlFieldsQuery(queryText, args), selector);

            var paramExpr = queryData.ParameterExpressions;

            if (paramExpr.Count != queryData.Parameters.Count)
                throw new InvalidOperationException("Error compiling query: all compiled query arguments " +
                                                    "should come from enclosing lambda expression");

            // TODO: Fix args order
            return args => _cache.QueryFields(new SqlFieldsQuery(queryText, args[1], args[0]), selector);
        }

        /** <inheritdoc /> */
        public static QueryData GetQueryData(QueryModel queryModel)
        {
            return new CacheQueryModelVisitor().GenerateQuery(queryModel);
        }

        /// <summary>
        /// Gets the result selector.
        /// </summary>
        private static Func<IBinaryRawReader, int, T> GetResultSelector<T>(Expression selectorExpression)
        {
            var newExpr = selectorExpression as NewExpression;

            if (newExpr != null)
                return GetCompiledCtor<T>(newExpr.Constructor);

            var entryCtor = GetCacheEntryCtorInfo(typeof(T));

            if (entryCtor != null)
                return GetCompiledCtor<T>(entryCtor);

            if (typeof(T) == typeof(bool))
                return ReadBool<T>;

            return (reader, count) => reader.ReadObject<T>();
        }

        /// <summary>
        /// Reads the bool. Actual data may be bool or int/long.
        /// </summary>
        private static T ReadBool<T>(IBinaryRawReader reader, int count)
        {
            var obj = reader.ReadObject<object>();

            if (obj is bool)
                return (T) obj;

            if (obj is long)
                return TypeCaster<T>.Cast((long) obj != 0);

            if (obj is int)
                return TypeCaster<T>.Cast((int) obj != 0);

            throw new InvalidOperationException("Expected bool, got: " + obj);
        }

        /// <summary>
        /// Gets the cache entry constructor.
        /// </summary>
        private static ConstructorInfo GetCacheEntryCtorInfo(Type entryType)
        {
            if (!entryType.IsGenericType || entryType.GetGenericTypeDefinition() != typeof(ICacheEntry<,>))
                return null;

            var args = entryType.GetGenericArguments();

            var targetType = typeof (CacheEntry<,>).MakeGenericType(args);

            return targetType.GetConstructors().Single();
        }

        /// <summary>
        /// Gets the compiled constructor.
        /// </summary>
        private static Func<IBinaryRawReader, int, T> GetCompiledCtor<T>(ConstructorInfo ctorInfo)
        {
            object result;

            if (CtorCache.TryGetValue(ctorInfo, out result))
                return (Func<IBinaryRawReader, int, T>) result;

            return (Func<IBinaryRawReader, int, T>) CtorCache.GetOrAdd(ctorInfo, x =>
            {
                var innerCtor1 = DelegateConverter.CompileCtor<T>(x, GetCacheEntryCtorInfo);

                return (Func<IBinaryRawReader, int, T>) ((r, c) => innerCtor1(r));
            });
        }
    }
}