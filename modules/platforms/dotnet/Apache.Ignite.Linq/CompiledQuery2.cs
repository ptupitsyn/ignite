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

namespace Apache.Ignite.Linq
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Represents a compiled cache query.
    /// </summary>
    public static class CompiledQuery2
    {
        /// <summary>
        /// Creates a new delegate that represents the compiled cache query.
        /// </summary>
        /// <param name="query">The query to compile.</param>
        /// <returns>Delegate that represents the compiled cache query.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", 
            Justification = "Invalid warning, validation is present.")]
        public static Func<IQueryCursor<T>> Compile<T>(Expression<Func<IQueryable<T>>> query)
        {
            IgniteArgumentCheck.NotNull(query, "query");

            var compiledQuery = GetCompiledQuery<T>(query, query.Compile());

            return () => compiledQuery(new object[0]);
        }

        /// <summary>
        /// Creates a new delegate that represents the compiled cache query.
        /// </summary>
        /// <param name="query">The query to compile.</param>
        /// <returns>Delegate that represents the compiled cache query.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", 
            Justification = "Invalid warning, validation is present.")]
        public static Func<T1, IQueryCursor<T>> Compile<T, T1>(Expression<Func<T1, IQueryable<T>>> query)
        {
            IgniteArgumentCheck.NotNull(query, "query");

            var compiledQuery = GetCompiledQuery<T>(query, query.Compile());

            return x => compiledQuery(new object[] {x});
        }

        /// <summary>
        /// Creates a new delegate that represents the compiled cache query.
        /// </summary>
        /// <param name="query">The query to compile.</param>
        /// <returns>Delegate that represents the compiled cache query.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", 
            Justification = "Invalid warning, validation is present.")]
        public static Func<T1, T2, IQueryCursor<T>> Compile<T, T1, T2>(Expression<Func<T1, T2, 
            IQueryable<T>>> query)
        {
            IgniteArgumentCheck.NotNull(query, "query");

            var compiledQuery = GetCompiledQuery<T>(query, query.Compile());

            return (x, y) => compiledQuery(new object[] {x, y});
        }

        /// <summary>
        /// Creates a new delegate that represents the compiled cache query.
        /// </summary>
        /// <param name="query">The query to compile.</param>
        /// <returns>Delegate that represents the compiled cache query.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", 
            Justification = "Invalid warning, validation is present.")]
        public static Func<T1, T2, T3, IQueryCursor<T>> Compile<T, T1, T2, T3>(Expression<Func<T1, T2, T3,
            IQueryable<T>>> query)
        {
            IgniteArgumentCheck.NotNull(query, "query");

            var compiledQuery = GetCompiledQuery<T>(query, query.Compile());

            return (x, y, z) => compiledQuery(new object[] {x, y, z});
        }

        /// <summary>
        /// Creates a new delegate that represents the compiled cache query.
        /// </summary>
        /// <param name="query">The query to compile.</param>
        /// <returns>Delegate that represents the compiled cache query.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", 
            Justification = "Invalid warning, validation is present.")]
        public static Func<T1, T2, T3, T4, IQueryCursor<T>> Compile<T, T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4,
            IQueryable<T>>> query)
        {
            IgniteArgumentCheck.NotNull(query, "query");

            var compiledQuery = GetCompiledQuery<T>(query, query.Compile());

            return (x, y, z, a) => compiledQuery(new object[] {x, y, z, a});
        }

        /// <summary>
        /// Creates a new delegate that represents the compiled cache query.
        /// </summary>
        /// <param name="query">The query to compile.</param>
        /// <returns>Delegate that represents the compiled cache query.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", 
            Justification = "Invalid warning, validation is present.")]
        public static Func<T1, T2, T3, T4, T5, IQueryCursor<T>> Compile<T, T1, T2, T3, T4, T5>(
            Expression<Func<T1, T2, T3, T4, T5, IQueryable<T>>> query)
        {
            IgniteArgumentCheck.NotNull(query, "query");

            var compiledQuery = GetCompiledQuery<T>(query, query.Compile());

            return (x, y, z, a, b) => compiledQuery(new object[] {x, y, z, a, b});
        }

        /// <summary>
        /// Creates a new delegate that represents the compiled cache query.
        /// </summary>
        /// <param name="query">The query to compile.</param>
        /// <returns>Delegate that represents the compiled cache query.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", 
            Justification = "Invalid warning, validation is present.")]
        public static Func<T1, T2, T3, T4, T5, T6, IQueryCursor<T>> Compile<T, T1, T2, T3, T4, T5, T6>(
            Expression<Func<T1, T2, T3, T4, T5, T6, IQueryable<T>>> query)
        {
            IgniteArgumentCheck.NotNull(query, "query");

            var compiledQuery = GetCompiledQuery<T>(query, query.Compile());

            return (x, y, z, a, b, c) => compiledQuery(new object[] {x, y, z, a, b, c});
        }

        /// <summary>
        /// Creates a new delegate that represents the compiled cache query.
        /// </summary>
        /// <param name="query">The query to compile.</param>
        /// <returns>Delegate that represents the compiled cache query.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", 
            Justification = "Invalid warning, validation is present.")]
        public static Func<T1, T2, T3, T4, T5, T6, T7, IQueryCursor<T>> Compile<T, T1, T2, T3, T4, T5, T6, T7>(
            Expression<Func<T1, T2, T3, T4, T5, T6, T7, IQueryable<T>>> query)
        {
            IgniteArgumentCheck.NotNull(query, "query");

            var compiledQuery = GetCompiledQuery<T>(query, query.Compile());

            return (x, y, z, a, b, c, d) => compiledQuery(new object[] {x, y, z, a, b, c, d});
        }

        /// <summary>
        /// Creates a new delegate that represents the compiled cache query.
        /// </summary>
        /// <param name="query">The query to compile.</param>
        /// <returns>Delegate that represents the compiled cache query.</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", 
            Justification = "Invalid warning, validation is present.")]
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, IQueryCursor<T>> Compile<T, T1, T2, T3, T4, T5, T6, T7, T8>(
            Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, IQueryable<T>>> query)
        {
            IgniteArgumentCheck.NotNull(query, "query");

            var compiledQuery = GetCompiledQuery<T>(query, query.Compile());

            return (x, y, z, a, b, c, d, e) => compiledQuery(new object[] {x, y, z, a, b, c, d, e});
        }

        /// <summary>
        /// Gets the compiled query.
        /// </summary>
        private static Func<object[], IQueryCursor<T>> GetCompiledQuery<T>(Expression expression, Delegate queryCaller)
        {
            Debug.Assert(expression != null);

            // TODO
            return x => null;

            //var cacheQueryable = queryable as ICacheQueryableInternal;

            //if (cacheQueryable == null)
            //    throw new ArgumentException(
            //        string.Format("{0} can only compile cache queries produced by AsCacheQueryable method. " +
            //                      "Provided query is not valid: '{1}'", typeof (CompiledQuery2).FullName, queryable));

            ////Debug.WriteLine(queryable);

            //// TODO: Provide some parameter info from the calling method to mitigate ConstantExpression uncertainty.
            //return cacheQueryable.CompileQuery<T>(queryCaller);
        }
    }
}
