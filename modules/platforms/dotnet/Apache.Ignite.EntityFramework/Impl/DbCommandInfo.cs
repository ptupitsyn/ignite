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

namespace Apache.Ignite.EntityFramework.Impl
{
    using System.Data.Entity.Core.Common.CommandTrees;

    /// <summary>
    /// Command info.
    /// </summary>
    internal class DbCommandInfo
    {
        /** */
        private readonly bool _isQuery;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbCommandInfo"/> class.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public DbCommandInfo(DbCommandTree tree)
        {
            _isQuery = tree is DbQueryCommandTree;
        }

        /// <summary>
        /// Gets a value indicating whether this command is a query and does not modify data.
        /// </summary>
        public bool IsQuery
        {
            get { return _isQuery; }
        }
    }
}
