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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Text;
    using Remotion.Linq.Clauses;

    /// <summary>
    /// Alias dictionary.
    /// </summary>
    internal class AliasDictionary
    {
        /** */
        private int _aliasIndex;

        /** */
        private Dictionary<IQuerySource, string> _aliases = new Dictionary<IQuerySource, string>();

        /** */
        private readonly Stack<Dictionary<IQuerySource, string>> _stack = new Stack<Dictionary<IQuerySource, string>>();

        /// <summary>
        /// Pushes current aliases to stack.
        /// </summary>
        public void Push()
        {
            _stack.Push(_aliases);

            _aliases = new Dictionary<IQuerySource, string>();
        }

        /// <summary>
        /// Pops current aliases from stack.
        /// </summary>
        public void Pop()
        {
            _aliases = _stack.Pop();
        }

        /// <summary>
        /// Gets the table alias.
        /// </summary>
        public string GetTableAlias(Expression expression)
        {
            Debug.Assert(expression != null);

            var src = ExpressionWalker.GetQuerySource(expression);

            return GetTableAlias(src);
        }

        public string GetTableAlias(IFromClause fromClause)
        {
            // TODO: ExpressionWalker skips IQuerySource incorrecly. We probably should get rid of WalkUp, it is difficult to generify it
            return GetTableAlias(ExpressionWalker.GetQuerySource(fromClause.FromExpression) ?? fromClause);
        }

        public string GetTableAlias(JoinClause joinClause)
        {
            return GetTableAlias(ExpressionWalker.GetQuerySource(joinClause.InnerSequence) ?? joinClause);
        }

        public string GetTableAlias(IQuerySource querySource)
        {
            Debug.Assert(querySource != null);

            string alias;

            if (!_aliases.TryGetValue(querySource, out alias))
            {
                alias = "_T" + _aliasIndex++;

                _aliases[querySource] = alias;
            }

            return alias;
        }

        /// <summary>
        /// Appends as clause.
        /// </summary>
        public StringBuilder AppendAsClause(StringBuilder builder, IFromClause clause)
        {
            Debug.Assert(builder != null);
            Debug.Assert(clause != null);

            var queryable = ExpressionWalker.GetCacheQueryable(clause);
            var tableName = ExpressionWalker.GetTableNameWithSchema(queryable);

            builder.AppendFormat("{0} as {1}", tableName, GetTableAlias(clause));

            return builder;
        }
    }
}
