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
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Command proxy.
    /// </summary>
    internal class DbCommandProxy : DbCommand
    {
        private readonly DbCommand _command;

        public DbCommandProxy(DbCommand command)
        {
            Debug.Assert(command != null);

            _command = command;
        }

        public override void Prepare()
        {
            _command.Prepare();
        }

        public override string CommandText
        {
            get { return _command.CommandText; }
            set { _command.CommandText = value; }
        }

        public override int CommandTimeout
        {
            get { return _command.CommandTimeout; }
            set { _command.CommandTimeout = value; }
        }

        public override CommandType CommandType
        {
            get { return _command.CommandType; }
            set { _command.CommandType = value; }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get { return _command.UpdatedRowSource; }
            set { _command.UpdatedRowSource = value; }
        }

        protected override DbConnection DbConnection
        {
            get { return _command.Connection; }
            set { _command.Connection = value; }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { return _command.Parameters; }
        }

        protected override DbTransaction DbTransaction
        {
            get { return _command.Transaction; }
            set { _command.Transaction = value; }
        }

        public override bool DesignTimeVisible
        {
            get { return _command.DesignTimeVisible; }
            set { _command.DesignTimeVisible = value; }
        }

        public override void Cancel()
        {
            _command.Cancel();
        }

        protected override DbParameter CreateDbParameter()
        {
            return _command.CreateParameter();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            // TODO: Check policy

            var cacheKey = GetKey();

            Console.WriteLine(cacheKey);

            var reader = _command.ExecuteReader(behavior);

            if (reader.RecordsAffected > 0)
                return reader;  // Queries that modify anything are never cached.


            var res = new DataReaderResult(reader);

            // TODO: Cache result

            return res.CreateReader();
        }

        private string GetKey()
        {
            var parameters = string.Join("|",
                Parameters.Cast<DbParameter>().Select(x => x.ParameterName + "=" + x.Value));

            return string.Format("{0}:{1}|{2}", Connection.Database, CommandText, parameters);
        }

#if !NET40
        // TODO: ExecuteDbDataReaderAsync in newer frameworks? How does this stack up?
#endif

        public override int ExecuteNonQuery()
        {
            return _command.ExecuteNonQuery();
        }

        public override object ExecuteScalar()
        {
            return _command.ExecuteScalar();
        }
    }
}
