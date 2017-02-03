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

namespace Apache.Ignite.Core.Tests.Binary.Serializable
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using NUnit.Framework;

    /// <summary>
    /// Tests that deserialization callbacks are invoked correctly.
    /// </summary>
    public class CallbacksTest
    {
        /** Deserialization messages. */
        private static readonly List<string> Messages = new List<string>();

        /// <summary>
        /// Tests that callbacks are invoked in correct order on class with ISerializable interface.
        /// </summary>
        [Test]
        public void TestSerializable()
        {
            var obj = new SerCallbacks
            {
                Name = "Foo",
                Inner = new SerCallbacks
                {
                    Name = "Bar",
                    Inner = new SerCallbacks
                    {
                        Name = "Baz"
                    }
                }
            };

            Messages.Clear();
            var res = TestUtils.SerializeDeserialize(obj);

            Assert.AreEqual("Foo", res.Name);
            Assert.AreEqual("Bar", res.Inner.Name);
            Assert.AreEqual("Baz", res.Inner.Inner.Name);

            // Callbacks should be called AFTER entire tree is deserialized.
            Assert.AreEqual(new[]
            {
                "Baz.ctor",
                "Bar.ctor",
                "Foo.ctor",
                "Foo.OnDeserialization",
                "Bar.OnDeserialization",
                "Baz.OnDeserialization"
            }, Messages);

            // TODO: Check attributes
        }

        /// <summary>
        /// Tests that callbacks are invoked in correct order on class without ISerializable interface.
        /// </summary>
        [Test]
        public void TestNonSerializable()
        {
            // TODO: Class without ISerializable
        }

        /// <summary>
        /// Tests that incorrect method signature causes a descriptive exception.
        /// </summary>
        [Test]
        public void TestIncorrectMethodSignature()
        {
            // TODO
        }

        [Serializable]
        private class SerCallbacks : IDeserializationCallback, ISerializable
        {
            public string Name { get; set; }

            public SerCallbacks Inner { get; set; }

            public SerCallbacks()
            {
            }

            protected SerCallbacks(SerializationInfo info, StreamingContext context)
            {
                Name = info.GetString("name");
                Inner = (SerCallbacks) info.GetValue("inner", typeof(SerCallbacks));

                Messages.Add(string.Format("{0}.ctor", Name));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("name", Name);
                info.AddValue("inner", Inner);
            }

            public void OnDeserialization(object sender)
            {
                Messages.Add(string.Format("{0}.OnDeserialization", Name));
            }

            [OnSerializing]
            public void OnSerializing(StreamingContext context)
            {
                Messages.Add(string.Format("{0}.OnSerializing", Name));
            }

            [OnSerialized]
            public void OnSerialized(StreamingContext context)
            {
                Messages.Add(string.Format("{0}.OnSerialized", Name));
            }

            [OnDeserializing]
            public void OnDeserializing(StreamingContext context)
            {
                Messages.Add(string.Format("{0}.OnDeserializing", Name));
            }

            [OnDeserialized]
            public void OnDeserialized(StreamingContext context)
            {
                Messages.Add(string.Format("{0}.OnDeserialized", Name));
            }
        }
    }
}
