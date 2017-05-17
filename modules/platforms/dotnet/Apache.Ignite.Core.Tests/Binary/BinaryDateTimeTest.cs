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

namespace Apache.Ignite.Core.Tests.Binary
{
    using System;
    using Apache.Ignite.Core.Binary;
    using NUnit.Framework;

    /// <summary>
    /// DateTime binary serialization tests.
    /// </summary>
    public class BinaryDateTimeTest
    {
        /// <summary>
        /// Sets up the test fixture.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            Ignition.Start(new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                BinaryConfiguration = new BinaryConfiguration
                {
                    TypeConfigurations = new[]
                    {
                        new BinaryTypeConfiguration(typeof(DateTimeObj2))
                        {
                            Serializer = new BinaryReflectiveSerializer {ForceTimestamp = true}
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Tears down the test fixture.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            Ignition.StopAll(true);
        }

        /// <summary>
        /// Tests the default behavior: DateTime is written as ISerializable object.
        /// </summary>
        [Test]
        public void TestDefaultBehavior()
        {
            var binary = Ignition.GetIgnite().GetBinary();

            var obj = new DateTimeObj {Value = DateTime.Now};
            var bin = binary.ToBinary<IBinaryObject>(obj);
            var res = bin.Deserialize<DateTimeObj>();

            Assert.AreEqual(obj.Value, res.Value);
            Assert.AreEqual(obj.Value, bin.GetField<IBinaryObject>("Value").Deserialize<DateTime>());
        }

        class DateTimeObj
        {
            public DateTime Value { get; set; }
        }

        class DateTimeObj2
        {
            public DateTime Value { get; set; }
        }

        class DateTimeObjAttribute
        {
            // TODO
            public DateTime Value { get; set; }
        }
    }
}
