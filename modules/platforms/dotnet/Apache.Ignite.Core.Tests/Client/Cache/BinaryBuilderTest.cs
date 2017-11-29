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

namespace Apache.Ignite.Core.Tests.Client.Cache
{
    using Apache.Ignite.Core.Binary;
    using NUnit.Framework;

    /// <summary>
    /// Tests binary object builder in thin client.
    /// </summary>
    public class BinaryBuilderTest : ClientTestBase
    {
        /// <summary>
        /// Tests the classless object builder.
        /// </summary>
        [Test]
        public void TestClasslessBuilder()
        {
            var bin = Client.GetBinary();

            var obj = bin.GetBuilder("FooBarBaz")
                .SetByteField("code", 99)
                .SetStringField("name", "abc")
                .Build();

            var cache = GetBinaryCache();
            cache[1] = obj;
            var res = cache.Get(1);

            Assert.AreEqual("abc", res.GetField<string>("name"));
            Assert.AreEqual(99, res.GetField<byte>("code"));
            Assert.IsNull(res.GetField<object>("field"));

            var type = res.GetBinaryType();
            Assert.AreEqual("FooBarBaz", type.TypeName);
            Assert.IsFalse(type.IsEnum);

            CollectionAssert.AreEquivalent(new[] { "code", "name" }, type.Fields);
            Assert.AreEqual("byte", type.GetFieldTypeName("code"));
            Assert.AreEqual("String", type.GetFieldTypeName("name"));
        }

        /// <summary>
        /// Tests the builder with existing class.
        /// </summary>
        [Test]
        public void TestPersonBuilder()
        {
            var cache = GetBinaryCache();
            cache[1] = GetBinaryPerson(1);

            cache[1] = cache[1].ToBuilder().SetField("Name", "Baz").Build();

            Assert.AreEqual("Baz", GetCache<Person>()[1].Name);
        }
    }
}
