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

namespace Apache.Ignite.Core.Tests.DotNetCore.Common
{
    using System;
    using System.Globalization;
    using System.IO;
    using Apache.Ignite.Core.Log;

    /// <summary>
    /// 'dotnet test' swallows console output. This logger writes to a file to overcome this.
    /// </summary>
    internal class TestLogger : ILogger
    {
        /** */
        public static readonly TestLogger Instance = new TestLogger();

        /** */
        private readonly string _file;

        /// <summary>
        /// Prevents a default instance of the <see cref="TestLogger"/> class from being created.
        /// </summary>
        private TestLogger()
        {
            var binDir = Path.GetDirectoryName(GetType().Assembly.Location);
            _file = Path.Combine(binDir, "dotnet-test.log");

            File.WriteAllText(_file, $"{DateTime.Now}: Starting tests.");
        }

        /** <inheritdoc /> */
        public void Log(LogLevel level, string message, object[] args, IFormatProvider formatProvider, string category,
            string nativeErrorInfo, Exception ex)
        {
            lock (_file)
            {
                var text = args != null
                    ? string.Format(formatProvider ?? CultureInfo.InvariantCulture, message, args)
                    : message;
                File.AppendAllText(_file, text);
            }
        }

        /** <inheritdoc /> */
        public bool IsEnabled(LogLevel level)
        {
            return level > LogLevel.Debug;
        }
    }
}
