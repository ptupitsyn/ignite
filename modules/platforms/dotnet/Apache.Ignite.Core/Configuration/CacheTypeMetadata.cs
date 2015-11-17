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

namespace Apache.Ignite.Core.Configuration
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Cache type metadata describes cache type for queries.
    /// </summary>
    public class CacheTypeMetadata
    {
        /// <summary>
        /// Gets or sets the name of the database schema.
        /// </summary>
        public string DatabaseSchemaName { get; set; }

        /// <summary>
        /// Gets or sets the name of the database table.
        /// </summary>
        public string DatabaseTableName { get; set; }

        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        public Type KeyType { get; set; }

        /// <summary>
        /// Gets or sets the type of the value.
        /// </summary>
        public Type ValueType { get; set; }

        /// <summary>
        /// Gets or sets the query fields.
        /// </summary>
        public IDictionary<string, Type> QueryFields { get; set; }

        /// <summary>
        /// Gets or sets the text fields.
        /// </summary>
        public IDictionary<string, Type> TextFields { get; set; }
        /// <summary>
        /// Gets or sets the ascending fields.
        /// </summary>
        public IDictionary<string, Type> AscendingFields { get; set; }

        /// <summary>
        /// Gets or sets the descending fields.
        /// </summary>
        public IDictionary<string, Type> DescendingFields { get; set; }

        /// <summary>
        /// Gets or sets the aliases.
        /// </summary>
        public IDictionary<string, string> Aliases { get; set; }
    }
}
