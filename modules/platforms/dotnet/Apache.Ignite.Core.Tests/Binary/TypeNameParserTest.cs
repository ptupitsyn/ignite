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

namespace Apache.Ignite.Core.Tests.Binary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Apache.Ignite.Core.Impl.Binary;
    using NUnit.Framework;

    /// <summary>
    /// Tests the type name parser.
    /// </summary>
    public class TypeNameParserTest
    {
        /// <summary>
        /// Tests simple types.
        /// </summary>
        [Test]
        public void TestSimpleTypes()
        {
            // One letter.
            var res = TypeNameParser.Parse("x");
            Assert.AreEqual("x", res.GetFullName());
            Assert.AreEqual("x", res.GetName());
            Assert.AreEqual(0, res.NameStart);
            Assert.AreEqual(0, res.NameEnd);
            Assert.AreEqual(-1, res.AssemblyStart);
            Assert.AreEqual(-1, res.AssemblyEnd);
            Assert.IsNull(res.Generics);

            // Without assembly.
            res = TypeNameParser.Parse("System.Int");

            Assert.AreEqual(7, res.NameStart);
            Assert.AreEqual(9, res.NameEnd);
            Assert.IsNull(res.Generics);
            Assert.AreEqual(-1, res.AssemblyStart);

            // With assembly.
            res = TypeNameParser.Parse("System.Int, myasm, Ver=1");

            Assert.AreEqual(7, res.NameStart);
            Assert.AreEqual(9, res.NameEnd);
            Assert.IsNull(res.Generics);
            Assert.AreEqual(11, res.AssemblyStart);

            // Real types.
            CheckType(GetType());
            CheckType(typeof(string));
            CheckType(typeof(IDictionary));
        }

        /// <summary>
        /// Tests generic types.
        /// </summary>
        [Test]
        public void TestGenericTypes()
        {
            // One arg.

            // Two args.

            // Nested args.
        }

        /// <summary>
        /// Tests arrays.
        /// </summary>
        [Test]
        public void TestArrays()
        {
            CheckType(typeof(int[]));
            CheckType(typeof(int[,]));
            CheckType(typeof(int[][]));
            
            CheckType(typeof(List<int>[]));
            CheckType(typeof(List<int>[,]));
            CheckType(typeof(List<int>[][]));
        }

        /// <summary>
        /// Tests invalid type names.
        /// </summary>
        [Test]
        public void TestInvalidTypes()
        {
            // TODO
            Assert.Throws<ArgumentException>(() => TypeNameParser.Parse(null));
            Assert.Throws<ArgumentException>(() => TypeNameParser.Parse(""));
            Assert.Throws<ArgumentException>(() => TypeNameParser.Parse(""));
        }

        /// <summary>
        /// Checks the type.
        /// </summary>
        private static void CheckType(Type type)
        {
            var name = type.AssemblyQualifiedName;

            Assert.IsNotNull(name);

            var res = TypeNameParser.Parse(name);

            Assert.AreEqual(type.Name, res.GetName() + res.GetGenericHeader() + res.GetArray());

            if (res.Generics == null)
            {
                Assert.AreEqual(type.FullName, res.GetFullName() + res.GetArray());
            }

            Assert.AreEqual(type.FullName.Length + 1, res.AssemblyStart);
        }
    }
}
