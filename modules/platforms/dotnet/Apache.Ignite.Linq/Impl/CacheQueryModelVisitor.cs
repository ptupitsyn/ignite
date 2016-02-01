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
        public QueryData GenerateQuery(QueryModel queryModel)
        {
            // SELECT TOP 1
            _builder.Append("select ");

            var parenCount = ProcessResultOperators(queryModel);

            // FIELD1, FIELD2 FROM TABLE1, TABLE2
            BuildSqlExpression(queryModel.SelectClause.Selector, parenCount > 0);
            _builder.Append(')', parenCount).Append(" ");

            // WHERE ... JOIN ...
            VisitQueryModel(queryModel);

            if (char.IsWhiteSpace(_builder[_builder.Length - 1]))
                _builder.Remove(_builder.Length - 1, 1);  // TrimEnd

            var queryText = _builder.ToString();

            return new QueryData(queryText, _parameters, true);
        }

        /// <summary>
        /// Processes the result operators.
        /// </summary>
        private int ProcessResultOperators(QueryModel queryModel)
        {
            int parenCount = 0;

            foreach (var op in queryModel.ResultOperators.Reverse())
            {
                if (op is CountResultOperator)
                {
                    _builder.Append("count (");
                    parenCount++;
                }
                else if (op is SumResultOperator)
                {
                    _builder.Append("sum (");
                    parenCount++;
                }
                else if (op is MinResultOperator)
                {
                    _builder.Append("min (");
                    parenCount++;
                }
                else if (op is MaxResultOperator)
                {
                    _builder.Append("max (");
                    parenCount++;
                }
                // SELECT TOP 10 * FROM "person_cache".PERSON where _KEY>10 UNION (select * from "".PERSON where _key > 50 limit 20)
                // TODO: This is incorrect, UNION should go at the very end
                /*else if (op is UnionResultOperator)  
                {
                    var union = (UnionResultOperator) op;

                    resultBuilder.Append("union (");

                    // TODO: SubQuery expression OR ConstantExpression. See how Joins work..
                    var unionSql = GetSqlExpression(union.Source2);

                    resultOpParameters.AddRange(unionSql.Parameters);
                    resultBuilder.Append(unionSql.QueryText);
                    resultBuilder.Append(") ");
                }*/
                else if (op is DistinctResultOperator)
                    _builder.Append("distinct ");
                else if (op is FirstResultOperator || op is SingleResultOperator)
                    _builder.Append("top 1 ");
                else if (op is TakeResultOperator)
                    _builder.AppendFormat("top {0} ", ((TakeResultOperator) op).Count);
                else
                    throw new NotSupportedException("Operator is not supported: " + op);
            }
            return parenCount;
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

            _builder.Append(index > 0 ? "and " : "where ");

            BuildSqlExpression(whereClause.Predicate);

            _builder.Append(" ");
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

            _builder.AppendFormat("{0} join {1} on (", isOuter ? "left outer" : "inner",
                TableNameMapper.GetTableNameWithSchema(joinClause));

            BuildJoinCondition(joinClause.InnerKeySelector, joinClause.OuterKeySelector);

            _builder.Append(") ");
        }

        /// <summary>
        /// Builds the join condition ('x=y AND foo=bar').
        /// </summary>
        /// <param name="innerKey">The inner key selector.</param>
        /// <param name="outerKey">The outer key selector.</param>
        /// <returns>Condition string.</returns>
        private void BuildJoinCondition(Expression innerKey, Expression outerKey)
        {
            var innerNew = innerKey as NewExpression;
            var outerNew = outerKey as NewExpression;

            if (innerNew == null && outerNew == null)
            {
                BuildJoinSubCondition(innerKey, outerKey);
                return;
            }

            if (innerNew != null && outerNew != null)
            {
                if (innerNew.Constructor != outerNew.Constructor)
                    throw new NotSupportedException(
                        string.Format("Unexpected JOIN condition. Multi-key joins should have " +
                                      "the same initializers on both sides: '{0} = {1}'", innerKey, outerKey));

                for (var i = 0; i < innerNew.Arguments.Count; i++)
                {
                    if (i > 0)
                        _builder.Append(" and ");

                    BuildJoinSubCondition(innerNew.Arguments[i], outerNew.Arguments[i]);
                }

                return;
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
        private void BuildJoinSubCondition(Expression innerKey, Expression outerKey)
        {
            BuildSqlExpression(innerKey);
            _builder.Append(" = ");
            BuildSqlExpression(outerKey);
        }

        /// <summary>
        /// Builds the SQL expression.
        /// </summary>
        private void BuildSqlExpression(Expression expression, bool aggregating = false)
        {
            new CacheQueryExpressionVisitor(_builder, _parameters, aggregating).Visit(expression);
        }
    }
}
