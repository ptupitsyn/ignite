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
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Helper class with MethodInfos for methods that are supported by LINQ provider.
    /// </summary>
    internal static class Methods
    {
        private delegate void VisitMethodDelegate(MethodCallExpression expression, CacheQueryExpressionVisitor visitor);

        private static Dictionary<MethodInfo, VisitMethodDelegate> _delegates = new Dictionary
            <MethodInfo, VisitMethodDelegate>
        {
            {typeof (string).GetMethod("ToLower", new Type[0]), GetFunc("lower")},
            {typeof (string).GetMethod("ToUpper", new Type[0]), GetFunc("upper")},
            {typeof (string).GetMethod("Contains"), (e, v) => VisitSqlLike(e, v, "%{0}%")},
            {typeof (string).GetMethod("StartsWith", new[] {typeof (string)}), (e, v) => VisitSqlLike(e, v, "{0}%")},
            {typeof (string).GetMethod("EndsWith", new[] {typeof (string)}), (e, v) => VisitSqlLike(e, v, "%{0}")},
            {typeof (DateTime).GetMethod("ToString", new[] {typeof (string)}), GetFunc("formatdatetime")}
        };

        public static void VisitMethodCall(MethodCallExpression expression, CacheQueryExpressionVisitor visitor)
        {
            var method = expression.Method;

            VisitMethodDelegate del;

            if (!_delegates.TryGetValue(method, out del))
                throw new NotSupportedException(string.Format("Method not supported: {0}.({1})",
                    method.DeclaringType == null ? "static" : method.DeclaringType.FullName, method));

            del(expression, visitor);
        }

        private static VisitMethodDelegate GetFunc(string func)
        {
            return (e, v) => VisitInstanceFunc(e, v, func);
        }

        private static void VisitInstanceFunc(MethodCallExpression expression, CacheQueryExpressionVisitor visitor, string func)
        {
            visitor.ResultBuilder.Append(func).Append("(");

            visitor.Visit(expression.Object);

            foreach (var arg in expression.Arguments)
            {
                visitor.ResultBuilder.Append(", ");
                visitor.Visit(arg);
            }

            visitor.ResultBuilder.Append(")");
        }

        /// <summary>
        /// Visits the SQL like expression.
        /// </summary>
        private static void VisitSqlLike(MethodCallExpression expression, CacheQueryExpressionVisitor visitor, string likeFormat)
        {
            visitor.ResultBuilder.Append("(");

            visitor.Visit(expression.Object);

            visitor.ResultBuilder.Append(" like ?) ");

            visitor.Parameters.Add(string.Format(likeFormat, GetConstantValue(expression)));
        }

        /// <summary>
        /// Gets the single constant value.
        /// </summary>
        private static object GetConstantValue(MethodCallExpression expression)
        {
            var arg = expression.Arguments[0] as ConstantExpression;

            if (arg == null)
                throw new NotSupportedException("Only constant expression is supported inside Contains call: " + expression);

            return arg.Value;
        }
    }
}