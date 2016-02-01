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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using Remotion.Linq;
    using Remotion.Linq.Clauses;
    using Remotion.Linq.Clauses.Expressions;
    using Remotion.Linq.Clauses.ResultOperators;

    /// <summary>
    /// Query visitor, transforms LINQ expression to SQL.
    /// </summary>
    internal sealed class CacheQueryModelVisitor : QueryModelVisitorBase
    {
        /** */
        private readonly StringBuilder _builder = new StringBuilder();

        /** */
        private readonly List<object> _parameters = new List<object>();

        /// <summary>
        /// Generates the query.
        /// </summary>
        public static QueryData GenerateQuery(QueryModel queryModel)
        {
            Debug.Assert(queryModel != null);

            var visitor = new CacheQueryModelVisitor();

            visitor.VisitQueryModel(queryModel);

            var resultBuilder = new StringBuilder("select ");
            int parenCount = 0;
            var resultOpParameters = new List<object>();

            foreach (var op in queryModel.ResultOperators.Reverse())
            {
                if (op is CountResultOperator)
                {
                    resultBuilder.Append("count (");
                    parenCount++;
                }
                else if (op is SumResultOperator)
                {
                    resultBuilder.Append("sum (");
                    parenCount++;
                }
                else if (op is MinResultOperator)
                {
                    resultBuilder.Append("min (");
                    parenCount++;
                }
                else if (op is MaxResultOperator)
                {
                    resultBuilder.Append("max (");
                    parenCount++;
                }
                else if (op is UnionResultOperator)
                {
                    var union = (UnionResultOperator)op;

                    resultBuilder.Append("union (");

                    // TODO: SubQuery expression OR ?
                    var unionSql = GetSqlExpression(union.Source2);

                    resultOpParameters.AddRange(unionSql.Parameters);
                    resultBuilder.Append(unionSql.QueryText);
                    resultBuilder.Append(")");
                }
                else if (op is DistinctResultOperator)
                    resultBuilder.Append("distinct ");
                else if (op is FirstResultOperator || op is SingleResultOperator)
                    resultBuilder.Append("top 1 ");
                else if (op is TakeResultOperator)
                    resultBuilder.AppendFormat("top {0} ", ((TakeResultOperator)op).Count);
                else
                    throw new NotSupportedException("Operator is not supported: " + op);
            }

            var selectExp = GetSqlExpression(queryModel.SelectClause.Selector, parenCount > 0);
            resultBuilder.Append(selectExp.QueryText).Append(')', parenCount);

            var queryData = visitor.GetQuery();
            var queryText = resultBuilder.Append(" ").Append(queryData.QueryText).ToString();
            var parameters = selectExp.Parameters.Concat(queryData.Parameters).Concat(resultOpParameters);

            return new QueryData(queryText, parameters.ToArray(), true);
        }

        /// <summary>
        /// Gets the query.
        /// </summary>
        /// <returns>Query data.</returns>
        private QueryData GetQuery()
        {
            return new QueryData(_builder.ToString().TrimEnd(), _parameters);
        }

        /** <inheritdoc /> */
        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
        {
            base.VisitMainFromClause(fromClause, queryModel);

            _builder.AppendFormat("from {0} ", TableNameMapper.GetTableNameWithSchema(fromClause));

            foreach (var additionalFrom in queryModel.BodyClauses.OfType<AdditionalFromClause>())
                _builder.AppendFormat(", {0} ", TableNameMapper.GetTableNameWithSchema(additionalFrom));
        }

        /** <inheritdoc /> */
        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            base.VisitWhereClause(whereClause, queryModel, index);

            var whereSql = GetSqlExpression(whereClause.Predicate);

            _builder.Append(_parameters.Any() ? "and" : "where");
            _builder.AppendFormat(" {0} ", whereSql.QueryText);

            _parameters.AddRange(whereSql.Parameters);
        }

        /** <inheritdoc /> */
        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index)
        {
            base.VisitJoinClause(joinClause, queryModel, index);

            var subQuery = joinClause.InnerSequence as SubQueryExpression;
            var innerExpr = joinClause.InnerSequence as ConstantExpression;
            bool isOuter = false;

            if (subQuery != null)
            {
                if (!subQuery.QueryModel.IsIdentityQuery())
                    throw new NotSupportedException("Unexpected JOIN inner sequence (subqueries are not supported): " +
                                                    joinClause.InnerSequence);

                innerExpr = subQuery.QueryModel.MainFromClause.FromExpression as ConstantExpression;

                foreach (var resultOperator in subQuery.QueryModel.ResultOperators)
                {
                    if (resultOperator is DefaultIfEmptyResultOperator)
                        isOuter = true;
                    else
                        throw new NotSupportedException(
                            "Unexpected JOIN inner sequence (subqueries are not supported): " +
                            joinClause.InnerSequence);
                }
            }

            if (innerExpr == null)
                throw new NotSupportedException("Unexpected JOIN inner sequence (subqueries are not supported): " +
                                                joinClause.InnerSequence);

            if (!(innerExpr.Value is ICacheQueryable))
                throw new NotSupportedException("Unexpected JOIN inner sequence " +
                                                "(only results of cache.ToQueryable() are supported): " +
                                                innerExpr.Value);

            _builder.AppendFormat("{0} join {1} on ({2}) ", isOuter ? "left outer" : "inner",
                TableNameMapper.GetTableNameWithSchema(joinClause),
                BuildJoinCondition(joinClause.InnerKeySelector, joinClause.OuterKeySelector));
        }

        /// <summary>
        /// Builds the join condition ('x=y AND foo=bar').
        /// </summary>
        /// <param name="innerKey">The inner key selector.</param>
        /// <param name="outerKey">The outer key selector.</param>
        /// <returns>Condition string.</returns>
        private string BuildJoinCondition(Expression innerKey, Expression outerKey)
        {
            var innerNew = innerKey as NewExpression;
            var outerNew = outerKey as NewExpression;

            if (innerNew == null && outerNew == null)
                return BuildJoinSubCondition(innerKey, outerKey);

            if (innerNew != null && outerNew != null)
            {
                if (innerNew.Constructor != outerNew.Constructor)
                    throw new NotSupportedException(
                        string.Format("Unexpected JOIN condition. Multi-key joins should have " +
                                      "the same initializers on both sides: '{0} = {1}'", innerKey, outerKey));

                var builder = new StringBuilder();

                for (var i = 0; i < innerNew.Arguments.Count; i++)
                {
                    if (i > 0)
                        builder.Append(" and ");

                    builder.Append(BuildJoinSubCondition(innerNew.Arguments[i], outerNew.Arguments[i]));
                }

                return builder.ToString();
            }

            throw new NotSupportedException(
                string.Format("Unexpected JOIN condition. Multi-key joins should have " +
                              "anonymous type instances on both sides: '{0} = {1}'", innerKey, outerKey));
        }

        /// <summary>
        /// Builds the join sub condition.
        /// </summary>
        /// <param name="innerKey">The inner key.</param>
        /// <param name="outerKey">The outer key.</param>
        /// <returns>Condition string</returns>
        private string BuildJoinSubCondition(Expression innerKey, Expression outerKey)
        {
            var inner = GetSqlExpression(innerKey);
            var outer = GetSqlExpression(outerKey);

            _parameters.AddRange(inner.Parameters);
            _parameters.AddRange(outer.Parameters);

            return string.Format("{0} = {1}", inner.QueryText, outer.QueryText);
        }

        /// <summary>
        /// Gets the SQL expression.
        /// </summary>
        private static QueryData GetSqlExpression(Expression expression, bool aggregating = false)
        {
            return CacheQueryExpressionVisitor.GetSqlExpression(expression, aggregating);
        }
    }
}
