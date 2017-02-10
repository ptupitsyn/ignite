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
    using System.Collections.Generic;
    using System.Linq;
    using Apache.Ignite.Core.Cache;
    using NUnit.Framework;

    /// <summary>
    /// Tests binary type interoperability between .NET and Java code.
    /// </summary>
    public class JavaBinaryInteropTest
    {
        /// <summary>
        /// Tests that all kinds of values from .NET can be handled properly on Java side.
        /// </summary>
        [Test]
        public void TestValueRoundtrip()
        {
            using (var ignite = Ignition.Start(TestUtils.GetTestConfiguration()))
            {
                var cache = ignite.CreateCache<int, object>((string) null);

                // Basic types.
                CheckValueCaching((byte) 255);
                CheckValueCachingAsObject((byte) 255);

                CheckValueCaching((sbyte) -10);

                CheckValueCaching((short) -32000);
                CheckValueCachingAsObject((short) -32000);

                CheckValueCaching((ushort) 65350);
                CheckValueCaching(int.MinValue);
                CheckValueCaching(uint.MaxValue);
                CheckValueCaching(long.MinValue);
                CheckValueCaching(ulong.MaxValue);

                // Basic type arrays.
                CheckValueCaching(new [] {Guid.Empty, Guid.NewGuid()});
                CheckValueCaching(new Guid?[] {Guid.Empty, Guid.NewGuid()});
                CheckValueCachingAsObject(new Guid?[] {Guid.Empty, Guid.NewGuid()});

                // Custom types.
                CheckValueCaching(new Foo {X = 10});
                CheckValueCachingAsObject(new Foo {X = 10});

                CheckValueCaching(new List<Foo> {new Foo {X = -2}, new Foo {X = 2}});
                CheckValueCachingAsObject(new List<Foo> {new Foo {X = -2}, new Foo {X = 2}});

                CheckValueCaching(new[] {new Foo {X = -1}, new Foo {X = 1}});
                //CheckValueCachingAsObject(new[] {new Foo {X = -1}, new Foo {X = 1}});
            }
        }

        /// <summary>
        /// Checks caching of a value.
        /// </summary>
        private static void CheckValueCaching<T>(T val)
        {
            var cache = Ignition.GetIgnite(null).GetCache<int, T>(null);

            cache[1] = val;
            Assert.AreEqual(val, cache[1]);
        }

        /// <summary>
        /// Checks caching of a value.
        /// </summary>
        private static void CheckValueCachingAsObject<T>(T val)
        {
            var cache = Ignition.GetIgnite(null).GetCache<int, object>(null);

            cache[1] = val;
            Assert.AreEqual(val, (T) cache[1]);
        }

        /// <summary>
        /// Test custom class.
        /// </summary>
        private class Foo
        {
            public int X { get; set; }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return X == ((Foo) obj).X;
            }

            public override int GetHashCode()
            {
                return X;
            }
        }

        /// <summary>
        /// Test custom struct.
        /// </summary>
        private struct Bar
        {
            public int X { get; set; }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Bar && X == ((Bar) obj).X;
            }

            public override int GetHashCode()
            {
                return X;
            }
        }
    }
}
