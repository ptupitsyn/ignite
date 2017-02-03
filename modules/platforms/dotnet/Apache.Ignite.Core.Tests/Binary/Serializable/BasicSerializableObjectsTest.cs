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
    using System.Runtime.Serialization;
    using Apache.Ignite.Core.Binary;
    using NUnit.Framework;

    /// <summary>
    /// Tests basic ISerializable scenarios.
    /// </summary>
    public class BasicSerializableObjectsTest
    {
        /// <summary>
        /// Tests the object with no fields.
        /// </summary>
        [Test]
        public void TestEmptyObject()
        {
            var res = TestUtils.SerializeDeserialize(new EmptyObject());

            Assert.IsNotNull(res);
        }

        /// <summary>
        /// Tests ISerializable without serialization ctor.
        /// </summary>
        [Test]
        public void TestMissingCtor()
        {
            var ex = Assert.Throws<SerializationException>(() => TestUtils.SerializeDeserialize(new MissingCtor()));
            Assert.AreEqual(string.Format("The constructor to deserialize an object of type '{0}' was not found.", 
                typeof(MissingCtor)), ex.Message);
        }

        /// <summary>
        /// Missing serialization ctor.
        /// </summary>
        private class MissingCtor : ISerializable
        {
            /** <inheritdoc /> */
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // No-op.
            }
        }

        /// <summary>
        /// Object with no fields.
        /// </summary>
        [Serializable]
        private class EmptyObject : ISerializable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="EmptyObject"/> class.
            /// </summary>
            public EmptyObject()
            {
                // No-op.
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="EmptyObject"/> class.
            /// </summary>
            private EmptyObject(SerializationInfo info, StreamingContext context)
            {
                Assert.IsInstanceOf<IBinaryReader>(context.Context);
            }

            /** <inheritdoc /> */
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                Assert.IsInstanceOf<IBinaryWriter>(context.Context);
            }
        }
    }
}
