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

namespace Apache.Ignite.Core.Tests.DataStructures
{
    using System;
    using System.Linq;
    using Apache.Ignite.Core.Portable;
    using NUnit.Framework;

    /// <summary>
    /// Atomic reference test.
    /// </summary>
    public class AtomicReferenceTest : IgniteTestBase
    {
        /** */
        private const string AtomicRefName = "testAtomicRef";

        /// <summary>
        /// Initializes a new instance of the <see cref="AtomicReferenceTest"/> class.
        /// </summary>
        public AtomicReferenceTest() : base("config\\compute\\compute-grid1.xml")
        {
            // No-op.
        }

        /** <inheritdoc /> */
        public override void TestSetUp()
        {
            base.TestSetUp();

            // Close test atomic if there is any
            Grid.GetAtomicReference(AtomicRefName, 0, true).Close();
        }

        /** <inheritdoc /> */
        protected override IgniteConfiguration GetConfiguration(string springConfigUrl)
        {
            var cfg = base.GetConfiguration(springConfigUrl);

            cfg.PortableConfiguration = new PortableConfiguration
            {
                TypeConfigurations = new[] { new PortableTypeConfiguration(typeof(PortableObj)) }
            };

            return cfg;
        }

        /// <summary>
        /// Tests lifecycle of the AtomicReference.
        /// </summary>
        [Test]
        public void TestCreateClose()
        {
            Assert.IsNull(Grid.GetAtomicReference(AtomicRefName, 10, false));

            // Nonexistent atomic returns null
            Assert.IsNull(Grid.GetAtomicReference(AtomicRefName, 10, false));

            // Create new
            var al = Grid.GetAtomicReference(AtomicRefName, 10, true);
            Assert.AreEqual(AtomicRefName, al.Name);
            Assert.AreEqual(10, al.Get());
            Assert.AreEqual(false, al.IsClosed());

            // Get existing with create flag
            var al2 = Grid.GetAtomicReference(AtomicRefName, 5, true);
            Assert.AreEqual(AtomicRefName, al2.Name);
            Assert.AreEqual(10, al2.Get());
            Assert.AreEqual(false, al2.IsClosed());

            // Get existing without create flag
            var al3 = Grid.GetAtomicReference(AtomicRefName, 5, false);
            Assert.AreEqual(AtomicRefName, al3.Name);
            Assert.AreEqual(10, al3.Get());
            Assert.AreEqual(false, al3.IsClosed());

            al.Close();

            Assert.AreEqual(true, al.IsClosed());
            Assert.AreEqual(true, al2.IsClosed());
            Assert.AreEqual(true, al3.IsClosed());

            Assert.IsNull(Grid.GetAtomicReference(AtomicRefName, 10, false));
        }

        /// <summary>
        /// Tests modification methods.
        /// </summary>
        [Test]
        public void TestModify()
        {
            var atomics = Enumerable.Range(1, 10)
                .Select(x => Grid.GetAtomicReference(AtomicRefName, 5, true)).ToList();

            atomics.ForEach(x => Assert.AreEqual(5, x.Get()));

            atomics[0].Set(15);
            atomics.ForEach(x => Assert.AreEqual(15, x.Get()));

            Assert.AreEqual(15, atomics[0].CompareExchange(42, 15));
            atomics.ForEach(x => Assert.AreEqual(42, x.Get()));
        }

        /// <summary>
        /// Tests primitives in the atomic.
        /// </summary>
        [Test]
        public void TestPrimitives()
        {
            TestOperations(1, 2);
            TestOperations("1", "2");
            TestOperations(Guid.NewGuid(), Guid.NewGuid());
            TestOperations(DateTime.Now, DateTime.Now.AddDays(-1));
        }

        /// <summary>
        /// Tests serializable objects in the atomic.
        /// </summary>
        [Test]
        public void TestSerializable()
        {
            TestOperations(new SerializableObj {Foo = 16}, new SerializableObj {Foo = -5});
        }

        /// <summary>
        /// Tests portable objects in the atomic.
        /// </summary>
        [Test]
        public void TestPortable()
        {
            TestOperations(new PortableObj {Foo = 16}, new PortableObj {Foo = -5});
        }

        /// <summary>
        /// Tests operations on specific object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void TestOperations<T>(T x, T y)
        {
            Grid.GetAtomicReference(AtomicRefName, 0, true).Close();

            var atomic = Grid.GetAtomicReference(AtomicRefName, x, true);

            Assert.AreEqual(x, atomic.Get());

            atomic.Set(y);
            Assert.AreEqual(y, atomic.Get());

            var old = atomic.CompareExchange(x, y);
            Assert.AreEqual(y, old);
            Assert.AreEqual(x, atomic.Get());

            old = atomic.CompareExchange(x, y);
            Assert.AreEqual(x, old);
            Assert.AreEqual(x, atomic.Get());

            // Check nulls
            var nul = default(T);

            old = atomic.CompareExchange(nul, x);
            Assert.AreEqual(x, old);
            Assert.AreEqual(nul, atomic.Get());

            old = atomic.CompareExchange(y, nul);
            Assert.AreEqual(nul, old);
            Assert.AreEqual(y, atomic.Get());
        }

        /// <summary>
        /// Serializable.
        /// </summary>
        [Serializable]
        private class SerializableObj
        {
            /** */
            public int Foo { get; set; }

            /** <inheritdoc /> */
            private bool Equals(SerializableObj other)
            {
                return Foo == other.Foo;
            }

            /** <inheritdoc /> */
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((SerializableObj) obj);
            }

            /** <inheritdoc /> */
            public override int GetHashCode()
            {
                return Foo;
            }

            /** <inheritdoc /> */
            public override string ToString()
            {
                return base.ToString() + "[" + Foo + "]";
            }
        }

        /// <summary>
        /// Portable.
        /// </summary>
        private sealed class PortableObj : SerializableObj
        {
            // No-op.
        }
    }
}