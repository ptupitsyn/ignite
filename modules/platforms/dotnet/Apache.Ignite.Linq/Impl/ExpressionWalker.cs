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
    using System.Linq.Expressions;
    using System.Reflection;
    using Apache.Ignite.Core.Impl.Common;
    using Remotion.Linq.Clauses;
    using Remotion.Linq.Clauses.Expressions;

    /// <summary>
    /// Walks expression trees to extract query and table name info.
    /// </summary>
    internal static class ExpressionWalker
    {
        /** Compiled member readers. */
        private static readonly CopyOnWriteConcurrentDictionary<MemberInfo, Func<object, object>> MemberReaders =
            new CopyOnWriteConcurrentDictionary<MemberInfo, Func<object, object>>();

        /// <summary>
        /// Gets the cache queryable.
        /// </summary>
        public static ICacheQueryableInternal GetCacheQueryable(Expression expression, bool throwWhenNotFound = true)
        {
            var expr = WalkUp(expression).LastOrDefault();

            var constExpr = expr as ConstantExpression;

            if (constExpr != null)
            {
                var queryable = constExpr.Value as ICacheQueryableInternal;

                if (queryable != null)
                    return queryable;
            }

            if (throwWhenNotFound)
                throw new NotSupportedException("Unexpected query source: " + expression);

            return null;
        }

        /// <summary>
        /// Gets the cache queryable.
        /// </summary>
        public static ICacheQueryableInternal GetCacheQueryable(IFromClause fromClause)
        {
            return GetCacheQueryable(fromClause.FromExpression);
        }

        /// <summary>
        /// Gets the cache queryable.
        /// </summary>
        public static ICacheQueryableInternal GetCacheQueryable(JoinClause joinClause)
        {
            return GetCacheQueryable(joinClause.InnerSequence);
        }


        public static IQuerySource GetQuerySource(Expression expression)
        {
            return WalkUp(expression).OfType<QuerySourceReferenceExpression>().First().ReferencedQuerySource;
        }

        /// <summary>
        /// Walks up the expression tree.
        /// </summary>
        private static IEnumerable<Expression> WalkUp(Expression expression)
        {
            yield return expression;

            while (true)
            {
                if (expression is ConstantExpression)
                    yield break;

                Expression res = null;

                var subQueryExp = expression as SubQueryExpression;

                if (subQueryExp != null)
                {
                    res = subQueryExp.QueryModel.MainFromClause.FromExpression;
                }
                else if (expression is QuerySourceReferenceExpression)
                {
                    var srcRef = (QuerySourceReferenceExpression) expression;

                    var fromSource = srcRef.ReferencedQuerySource as IFromClause;

                    if (fromSource != null)
                        res = fromSource.FromExpression;
                    else if (srcRef.ReferencedQuerySource is JoinClause)
                        res = ((JoinClause) srcRef.ReferencedQuerySource).InnerSequence;
                    else
                        throw new NotSupportedException("Unexpected query source: " + srcRef.ReferencedQuerySource);
                }
                else if (expression is MemberExpression)
                {
                    var memberExpr = (MemberExpression) expression;

                    if (memberExpr.Type.IsGenericType && memberExpr.Type.GetGenericTypeDefinition() == typeof(IQueryable<>))
                        res = Expression.Constant(EvaluateExpression<ICacheQueryableInternal>(memberExpr));
                    else
                        res = memberExpr.Expression;
                }

                if (res == null)
                    yield break;

                yield return res;

                expression = res;
            }
        }

        /// <summary>
        /// Evaluates the expression.
        /// </summary>
        public static T EvaluateExpression<T>(Expression expr)
        {
            var memberExpr = expr as MemberExpression;

            if (memberExpr != null)
            {
                var targetExpr = memberExpr.Expression as ConstantExpression;

                if (memberExpr.Expression == null || targetExpr != null)
                {
                    // Instance or static member
                    var target = targetExpr == null ? null : targetExpr.Value;

                    Func<object, object> reader;
                    if (MemberReaders.TryGetValue(memberExpr.Member, out reader))
                        return (T) reader(target);

                    return (T) MemberReaders.GetOrAdd(memberExpr.Member, x => CompileMemberReader(memberExpr))(target);
                }
            }

            throw new NotSupportedException("Expression not supported: " + expr);
        }

        /// <summary>
        /// Compiles the member reader.
        /// </summary>
        private static Func<object, object> CompileMemberReader(MemberExpression memberExpr)
        {
            // Field or property
            var fld = memberExpr.Member as FieldInfo;

            if (fld != null)
                return DelegateConverter.CompileFieldGetter(fld);

            var prop = memberExpr.Member as PropertyInfo;

            if (prop != null)
                return DelegateConverter.CompilePropertyGetter(prop);

            throw new NotSupportedException("Expression not supported: " + memberExpr);
        }

        /// <summary>
        /// Gets the table name with schema.
        /// </summary>
        public static string GetTableNameWithSchema(ICacheQueryableInternal queryable)
        {
            Debug.Assert(queryable != null);

            return string.Format("\"{0}\".{1}", queryable.CacheConfiguration.Name, queryable.TableName);
        }
    }
}
