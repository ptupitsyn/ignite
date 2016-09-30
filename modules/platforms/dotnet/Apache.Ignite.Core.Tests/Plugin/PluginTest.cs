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

namespace Apache.Ignite.Core.Tests.Plugin
{
    using NUnit.Framework;

    /// <summary>
    /// Tests the plugin system.
    /// </summary>
    public class PluginTest
    {
        /** */
        private const int OpReadWrite = 1;

        /** */
        private const int OpError = 2;

        /** */
        private IIgnite _ignite;

        /// <summary>
        /// Fixture set up.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _ignite = Ignition.Start(TestUtils.GetTestConfiguration());
        }

        /// <summary>
        /// Fixture tear down.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            Ignition.StopAll(true);
        }

        /// <summary>
        /// Tests the initialization.
        /// </summary>
        [Test]
        public void TestInitialization()
        {
            var plugin = _ignite.GetTestPlugin();

            Assert.IsNotNull(plugin);

            var plugin2 = _ignite.GetTestPlugin();

            Assert.AreSame(plugin.Target, plugin2.Target);
        }

        /// <summary>
        /// Tests the invoke operation.
        /// </summary>
        [Test]
        public void TestInvokeOperation()
        {
            var plugin = _ignite.GetTestPlugin();

            // Test round trip.
            Assert.AreEqual(1, plugin.Target.InvokeOperation(OpReadWrite,
                w => w.WriteObject(1), r => r.ReadObject<int>()));

            Assert.AreEqual("hello", plugin.Target.InvokeOperation(OpReadWrite,
                w => w.WriteObject("hello"), r => r.ReadObject<string>()));

            // Test exception.
        }

        /// <summary>
        /// Tests the callback.
        /// </summary>
        [Test]
        public void TestCallback()
        {
            // TODO: Test custom callbacks from Java.
        }
    }
}
