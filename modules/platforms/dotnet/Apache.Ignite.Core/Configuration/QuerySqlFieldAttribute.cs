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

namespace Apache.Ignite.Core.Configuration
{
    using System;

    /// <summary>
    /// Marks field or property for SQL queries.
    /// <para />
    /// Using this attribute is an alternative to <see cref="QueryEntity.Fields"/> in <see cref="CacheConfiguration"/>.
    /// Explicit <see cref="QueryEntity"/> configuration overrides attribute-based configuration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class QuerySqlFieldAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the sql field name.
        /// If not provided, property or field name will be used.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether corresponding field should be indexed.
        /// Just like with databases, field indexing may require additional overhead during updates, 
        /// but makes select operations faster.
        /// </summary>
        public bool IsIndexed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether index for this field should be descending.
        /// Ignored when <see cref="IsIndexed"/> is <c>false</c>.
        /// </summary>
        public bool IsDescending { get; set; }
    }
}
