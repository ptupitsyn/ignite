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
    using Apache.Ignite.Core.Configuration;
    using NUnit.Framework;

    /// <summary>
    /// Tests code-based configuration.
    /// </summary>
    public class IgniteConfigurationTest
    {
        [Test]
        public void TestDefaultSpi()
        {
            var cfg = new IgniteConfiguration
            {
                DiscoveryConfiguration =
                    new DiscoveryConfiguration
                    {
                        AckTimeout = TimeSpan.MaxValue,
                        JoinTimeout = TimeSpan.MaxValue,
                        NetworkTimeout = TimeSpan.MaxValue,
                        SocketTimeout = TimeSpan.MaxValue
                    },
                JvmClasspath = TestUtils.CreateTestClasspath(),
                JvmOptions = TestUtils.TestJavaOptions()
            };

            using (var ignite = Ignition.Start(cfg))
            { 
                Assert.AreEqual(1, ignite.GetCluster().GetNodes().Count);
            }
        }

        //[Test]
        public void TestInvalidTimeouts()
        {
            var cfg = new IgniteConfiguration
            {
                DiscoveryConfiguration =
                    new DiscoveryConfiguration
                    {
                        AckTimeout = TimeSpan.FromMilliseconds(-5),
                        JoinTimeout = TimeSpan.MinValue,
                    },
                JvmClasspath = TestUtils.CreateTestClasspath(),
                JvmOptions = TestUtils.TestJavaOptions()
            };

            using (var ignite = Ignition.Start(cfg))
            {
                Assert.IsNotNull(ignite);
            }
        }

        [Test]
        public void TestStaticSpi()
        {
            //var cfg1 = new IgniteConfiguration {DiscoveryConfiguration = new DiscoveryConfiguration {} };
        }

        [Test]
        public void TestMulticastSpi()
        {
            
        }
    }
}
