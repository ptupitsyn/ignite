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
    using System.Linq;
    using Apache.Ignite.Core.Common;
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
            Assert.AreEqual(12, res.AssemblyStart);

            // Real types.
            CheckType(GetType());
            CheckType(typeof(string));
            CheckType(typeof(IDictionary));

            // Nested types.
            CheckType(typeof(Nested));
            CheckType(typeof(Nested.Nested2));
        }

        /// <summary>
        /// Tests generic types.
        /// </summary>
        [Test]
        public void TestGenericTypes()
        {
            // One arg.
            var res = TypeNameParser.Parse(typeof(List<int>).AssemblyQualifiedName);
            Assert.AreEqual("List`1", res.GetName());
            Assert.AreEqual("System.Collections.Generic.List`1", res.GetFullName());
            Assert.IsTrue(res.GetAssemblyName().StartsWith("mscorlib,"));

            Assert.AreEqual(1, res.Generics.Count);
            var gen = res.Generics.Single();
            Assert.AreEqual("Int32", gen.GetName());
            Assert.AreEqual("System.Int32", gen.GetFullName());
            Assert.IsTrue(gen.GetAssemblyName().StartsWith("mscorlib,"));

            // Two args.
            res = TypeNameParser.Parse(typeof(Dictionary<int, string>).AssemblyQualifiedName);
            Assert.AreEqual("Dictionary`2", res.GetName());
            Assert.AreEqual("System.Collections.Generic.Dictionary`2", res.GetFullName());
            Assert.IsTrue(res.GetAssemblyName().StartsWith("mscorlib,"));

            Assert.AreEqual(2, res.Generics.Count);

            gen = res.Generics.First();
            Assert.AreEqual("Int32", gen.GetName());
            Assert.AreEqual("System.Int32", gen.GetFullName());
            Assert.IsTrue(gen.GetAssemblyName().StartsWith("mscorlib,"));

            gen = res.Generics.Last();
            Assert.AreEqual("String", gen.GetName());
            Assert.AreEqual("System.String", gen.GetFullName());
            Assert.IsTrue(gen.GetAssemblyName().StartsWith("mscorlib,"));

            // Nested args.
            res = TypeNameParser.Parse(typeof(Dictionary<int, List<string>>).FullName);

            Assert.AreEqual("Dictionary`2", res.GetName());
            Assert.AreEqual("System.Collections.Generic.Dictionary`2", res.GetFullName());
            Assert.IsNull(res.GetAssemblyName());

            Assert.AreEqual(2, res.Generics.Count);

            gen = res.Generics.Last();
            Assert.AreEqual("List`1", gen.GetName());
            Assert.AreEqual("System.Collections.Generic.List`1", gen.GetFullName());
            Assert.IsTrue(gen.GetAssemblyName().StartsWith("mscorlib,"));
            Assert.AreEqual(1, gen.Generics.Count);

            gen = gen.Generics.Single();
            Assert.AreEqual("String", gen.GetName());
            Assert.AreEqual("System.String", gen.GetFullName());
            Assert.IsTrue(gen.GetAssemblyName().StartsWith("mscorlib,"));

            // Nested class.
            res = TypeNameParser.Parse(typeof(NestedGeneric<int>).FullName);

            Assert.AreEqual("NestedGeneric`1", res.GetName());
            Assert.AreEqual("Apache.Ignite.Core.Tests.Binary.TypeNameParserTest+NestedGeneric`1", res.GetFullName());

            gen = res.Generics.Single();
            Assert.AreEqual("Int32", gen.GetName());
            Assert.AreEqual("System.Int32", gen.GetFullName());

            res = TypeNameParser.Parse(typeof(NestedGeneric<int>.NestedGeneric2<string>).AssemblyQualifiedName);
            
            Assert.AreEqual("NestedGeneric2`1", res.GetName());
            Assert.AreEqual("Apache.Ignite.Core.Tests.Binary.TypeNameParserTest+NestedGeneric`1+NestedGeneric2`1", 
                res.GetFullName());

            Assert.AreEqual(2, res.Generics.Count);
            Assert.AreEqual("Int32", res.Generics.First().GetName());
            Assert.AreEqual("String", res.Generics.Last().GetName());
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
            Assert.Throws<ArgumentException>(() => TypeNameParser.Parse(null));
            Assert.Throws<ArgumentException>(() => TypeNameParser.Parse(""));

            Assert.Throws<IgniteException>(() => TypeNameParser.Parse("x["));
            Assert.Throws<IgniteException>(() => TypeNameParser.Parse("x[[]"));
            Assert.Throws<IgniteException>(() => TypeNameParser.Parse("x`["));
            Assert.Throws<IgniteException>(() => TypeNameParser.Parse("x`]"));
            Assert.Throws<IgniteException>(() => TypeNameParser.Parse("x`[ ]"));
            Assert.Throws<IgniteException>(() => TypeNameParser.Parse("x,"));
            Assert.Throws<IgniteException>(() => TypeNameParser.Parse("x`x"));
            Assert.Throws<IgniteException>(() => TypeNameParser.Parse("x`2[x"));
            Assert.Throws<IgniteException>(() => TypeNameParser.Parse("x`2xx"));
        }

        /// <summary>
        /// Checks the type.
        /// </summary>
        private static void CheckType(Type type)
        {
            var name = type.AssemblyQualifiedName;

            Assert.IsNotNull(name);

            var res = TypeNameParser.Parse(name);

            Assert.AreEqual(type.Name, res.GetName() + res.GetArray());

            if (res.Generics == null)
            {
                Assert.AreEqual(type.FullName, res.GetFullName() + res.GetArray());
            }

            Assert.AreEqual(type.FullName.Length + 2, res.AssemblyStart);
        }

        private class Nested
        {
            public class Nested2
            {
                // No-op.
            }
        }

        private class NestedGeneric<T>
        {
            public class NestedGeneric2<T2>
            {
                // No-op.
            }
        }
    }
}
