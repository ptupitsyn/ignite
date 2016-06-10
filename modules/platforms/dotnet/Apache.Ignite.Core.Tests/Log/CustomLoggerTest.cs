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

namespace Apache.Ignite.Core.Tests.Log
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Common;
    using Apache.Ignite.Core.Communication.Tcp;
    using Apache.Ignite.Core.Compute;
    using Apache.Ignite.Core.Lifecycle;
    using Apache.Ignite.Core.Log;
    using NUnit.Framework;

    /// <summary>
    /// Tests that user-defined logger receives Ignite events.
    /// </summary>
    public class CustomLoggerTest
    {
        /** */
        private static readonly LogLevel[] AllLevels = Enum.GetValues(typeof (LogLevel)).OfType<LogLevel>().ToArray();

        /// <summary>
        /// Test setup.
        /// </summary>
        [SetUp]
        public void TestSetUp()
        {
            TestLogger.Entries.Clear();
        }

        /// <summary>
        /// Tests the startup output.
        /// </summary>
        [Test]
        public void TestStartupOutput()
        {
            using (Ignition.Start(GetConfigWithLogger(true)))
            {
                // Check initial message
                Assert.IsTrue(TestLogger.Entries[0].Message.StartsWith("Starting Ignite.NET"));

                // Check topology message
                Assert.IsTrue(
                    TestUtils.WaitForCondition(() =>
                    {
                        lock (TestLogger.Entries)
                        {
                            return TestLogger.Entries.Any(x => x.Message.Contains("Topology snapshot"));
                        }
                    }, 9000), "No topology snapshot");
            }

            // Test that all levels are present
            foreach (var level in AllLevels.Where(x => x != LogLevel.Error))
                Assert.IsTrue(TestLogger.Entries.Any(x => x.Level == level), "No messages with level " + level);
        }


        /// <summary>
        /// Tests startup error in Java.
        /// </summary>
        [Test]
        public void TestStartupJavaError()
        {
            // Invalid config
            Assert.Throws<IgniteException>(() =>
                Ignition.Start(new IgniteConfiguration(GetConfigWithLogger())
                {
                    CommunicationSpi = new TcpCommunicationSpi
                    {
                        IdleConnectionTimeout = TimeSpan.MinValue
                    }
                }));

            var err = TestLogger.Entries.First(x => x.Level == LogLevel.Error);
            Assert.IsTrue(err.NativeErrorInfo.Contains("SPI parameter failed condition check: idleConnTimeout > 0"));
            Assert.AreEqual("org.apache.ignite.internal.IgniteKernal", err.Category);
            Assert.IsNull(err.Exception);
        }

        /// <summary>
        /// Tests startup error in .NET.
        /// </summary>
        [Test]
        public void TestStartupDotNetError()
        {
            // Invalid bean
            Assert.Throws<IgniteException>(() =>
                Ignition.Start(new IgniteConfiguration(GetConfigWithLogger())
                {
                    LifecycleBeans = new[] {new FailBean()}
                }));

            var err = TestLogger.Entries.First(x => x.Level == LogLevel.Error);
            Assert.IsInstanceOf<ArithmeticException>(err.Exception);
        }

        /// <summary>
        /// Tests that .NET exception propagates through Java to the log.
        /// </summary>
        [Test]
        public void TestDotNetErrorPropagation()
        {
            // Start 2 nodes: PlatformNativeException does not occur in local scenario
            using (var ignite = Ignition.Start(GetConfigWithLogger()))
            using (Ignition.Start(new IgniteConfiguration(TestUtils.GetTestConfiguration()) {GridName = "1"}))
            {
                var compute = ignite.GetCluster().ForRemotes().GetCompute();

                Assert.Throws<ArithmeticException>(() => compute.Call(new FailFunc()));

                // Log updates may not arrive immediately
                TestUtils.WaitForCondition(() => TestLogger.Entries.Any(x => x.Exception != null), 3000);

                var errFromJava = TestLogger.Entries.Single(x => x.Exception != null);
                Assert.AreEqual("Error in func.", ((ArithmeticException) errFromJava.Exception.InnerException).Message);
            }
        }

        /// <summary>
        /// Tests the <see cref="QueryEntity"/> validation.
        /// </summary>
        [Test]
        public void TestQueryEntityValidation()
        {
            // TODO: QueryEntity warnings test
        }

        /// <summary>
        /// Tests the <see cref="LoggerExtensions"/> methods.
        /// </summary>
        [Test]
        public void TestExtensions()
        {
            var log = new TestLogger(LogLevel.Trace);

            // TODO: 20 level overloads, 4 log overloads, GetLogger
            log.Log(LogLevel.Trace, "trace");
            CheckLastMessage(LogLevel.Trace, "trace");
        }

        /// <summary>
        /// Checks the last message.
        /// </summary>
        private static void CheckLastMessage(LogLevel level, string message, object[] args = null, 
            IFormatProvider formatProvider = null, string category = null, string nativeErr = null, Exception e = null)
        {
            var msg = TestLogger.Entries.Last();

            Assert.AreEqual(msg.Level, level);
            Assert.AreEqual(msg.Message, message);
            Assert.AreEqual(msg.Args, args);
            Assert.AreEqual(msg.FormatProvider, formatProvider);
            Assert.AreEqual(msg.Category, category);
            Assert.AreEqual(msg.NativeErrorInfo, nativeErr);
            Assert.AreEqual(msg.Exception, e);
        }

        /// <summary>
        /// Gets the configuration with logger.
        /// </summary>
        private static IgniteConfiguration GetConfigWithLogger(bool verbose = false)
        {
            return new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                Logger = new TestLogger(verbose ? LogLevel.Trace : LogLevel.Info)
            };
        }

        /// <summary>
        /// Test log entry.
        /// </summary>
        private class LogEntry
        {
            public LogLevel Level;
            public string Message;
            public object[] Args;
            public IFormatProvider FormatProvider;
            public string Category;
            public string NativeErrorInfo;
            public Exception Exception;

            public override string ToString()
            {
                return string.Format("Level: {0}, Message: {1}, Args: {2}, FormatProvider: {3}, Category: {4}, " +
                                     "NativeErrorInfo: {5}, Exception: {6}", Level, Message, Args, FormatProvider, 
                                     Category, NativeErrorInfo, Exception);
            }
        }

        /// <summary>
        /// Test logger.
        /// </summary>
        private class TestLogger : ILogger
        {
            public static readonly List<LogEntry> Entries = new List<LogEntry>(5000);

            private readonly ILogger _console = new ConsoleLogger(AllLevels);
            private readonly LogLevel _minLevel;

            public TestLogger(LogLevel minLevel)
            {
                _minLevel = minLevel;
            }

            public void Log(LogLevel level, string message, object[] args, IFormatProvider formatProvider, string category,
                string nativeErrorInfo, Exception ex)
            {
                if (!IsEnabled(level))
                    return;

                lock (Entries)
                {
                    Entries.Add(new LogEntry
                    {
                        Level = level,
                        Message = message,
                        Args = args,
                        FormatProvider = formatProvider,
                        Category = category,
                        NativeErrorInfo = nativeErrorInfo,
                        Exception = ex
                    });
                }

                if (level > LogLevel.Debug)
                    _console.Log(level, message, args, formatProvider, category, nativeErrorInfo, ex);
            }

            public bool IsEnabled(LogLevel level)
            {
                return level >= _minLevel;
            }
        }


        /// <summary>
        /// Failing lifecycle bean.
        /// </summary>
        private class FailBean : ILifecycleBean
        {
            public void OnLifecycleEvent(LifecycleEventType evt)
            {
                throw new ArithmeticException("Failure in bean");
            }
        }

        /// <summary>
        /// Failing computation.
        /// </summary>
        [Serializable]
        private class FailFunc : IComputeFunc<string>
        {
            public string Invoke()
            {
                throw new ArithmeticException("Error in func.");
            }
        }
    }
}
