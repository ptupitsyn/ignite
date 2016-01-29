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
    using System.Diagnostics;
    using System.Linq.Expressions;
    using Apache.Ignite.Core.Cache;
    using Remotion.Linq.Clauses;
    using Remotion.Linq.Clauses.Expressions;

    internal static class TableNameMapper
    {
        public static string GetTableName<TKey, TValue>(ICache<TKey, TValue> cache)
        {
            return GetTableNameFromEntryValueType(typeof (TValue));
        }

        public static string GetTableNameFromEntryValueType(Type entryValueType)
        {
            Debug.Assert(entryValueType != null);

            return entryValueType.Name;
        }

        public static string GetTableNameFromEntryType(Type cacheEntryType)
        {
            Debug.Assert(cacheEntryType != null);
            
            if (!(cacheEntryType.IsGenericType && cacheEntryType.GetGenericTypeDefinition() == typeof(ICacheEntry<,>)))
                throw new NotSupportedException("Unexpected cache query entry type: " + cacheEntryType);

            return GetTableNameFromEntryValueType(cacheEntryType.GetGenericArguments()[1]);
        }

        public static string GetTableNameWithSchema(QuerySourceReferenceExpression expression)
        {
            Debug.Assert(expression != null);

            return GetTableNameFromEntryType(expression.ReferencedQuerySource.ItemType);
        }

        public static string GetTableNameWithSchema(MemberExpression expression)
        {
            Debug.Assert(expression != null);

            var querySrc = expression.Expression as QuerySourceReferenceExpression;

            if (querySrc != null)
                return GetTableNameWithSchema(querySrc);

            var innerMember = expression.Expression as MemberExpression;

            if (innerMember != null)
                return GetTableNameWithSchema(innerMember);
;
            throw new NotSupportedException("Unexpected member expression, cannot find query source: " + expression);
        }

        public static string GetTableNameWithSchema(MainFromClause fromClause)
        {
            return GetTableNameFromEntryType(fromClause.ItemType);
        }

        public static string GetTableNameWithSchema(JoinClause joinClause)
        {
            return GetTableNameFromEntryType(joinClause.ItemType);
        }
    }
}
