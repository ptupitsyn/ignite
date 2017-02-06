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

namespace Apache.Ignite.Core.Impl.Binary
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Reflection utils.
    /// </summary>
    internal static class ReflectionUtils
    {
        private static readonly CopyOnWriteConcurrentDictionary<Type, Action<object, object>> CopyFieldsActions
            = new CopyOnWriteConcurrentDictionary<Type, Action<object, object>>();
                
        /// <summary>
        /// Gets all fields, including base classes.
        /// </summary>
        public static IEnumerable<FieldInfo> GetAllFields(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                       BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            var curType = type;

            while (curType != null)
            {
                foreach (var field in curType.GetFields(flags))
                {
                    yield return field;
                }

                curType = curType.BaseType;
            }
        }

        /// <summary>
        /// Copies the fields.
        /// </summary>
        public static void CopyFields(object x, object y)
        {
            Debug.Assert(x != null);
            Debug.Assert(y != null);
            Debug.Assert(x.GetType() == y.GetType());

            // TODO: Compiled delegate
            foreach (var fieldInfo in GetAllFields(x.GetType()))
            {
                var val = fieldInfo.GetValue(x);

                fieldInfo.SetValue(y, val);
            }
        }
    }
}
