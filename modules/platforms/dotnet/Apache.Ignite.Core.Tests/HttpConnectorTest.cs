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

namespace Apache.Ignite.Core.Tests
{
    using System;
    using System.Net;
    using NUnit.Framework;

    /// <summary>
    /// Tests HTTP connectivity.
    /// </summary>
    public class HttpConnectorTest
    {
        /// <summary>
        /// Tests connectivity with default configuration.
        /// </summary>
        [Test]
        public void TestDefaultConfig()
        {
            using (Ignition.Start(GetConfigWithRestHttp()))
            {
                CheckHttpApi();
            }
        }

        /// <summary>
        /// Tests the custom connector configuration.
        /// </summary>
        [Test]
        public void TestCustomConfig()
        {
            // TODO: Custom ConnectorConfiguration
        }

        /// <summary>
        /// Sets up the test.
        /// </summary>
        [SetUp]
        public void TestSetUp()
        {
            Assert.Catch<Exception>(() => CheckHttpApi(), "Default HTTP API port must be available.");
        }

        /// <summary>
        /// Tears down the test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Ignition.StopAll(true);
            Assert.Catch<Exception>(() => CheckHttpApi(), "Default HTTP API port must be available.");
        }

        /// <summary>
        /// Gets the configuration with rest HTTP in classpath.
        /// </summary>
        private static IgniteConfiguration GetConfigWithRestHttp()
        {
            return new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                JvmClasspath = TestUtils.CreateTestClasspath(true)
            };
        }

        /// <summary>
        /// Asserts that HTTP API works.
        /// </summary>
        private void CheckHttpApi()
        {
            var res = new WebClient().DownloadString("http://localhost:8080/ignite?cmd=version");

            var expected = string.Format("\"response\":\"{0}", GetType().Assembly.GetName().Version.ToString(3));

            Assert.IsTrue(res.Contains(expected), res);
        }
    }
}
