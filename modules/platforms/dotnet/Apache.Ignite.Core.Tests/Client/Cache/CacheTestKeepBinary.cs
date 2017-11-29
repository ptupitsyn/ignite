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

namespace Apache.Ignite.Core.Tests.Client.Cache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Client;
    using Apache.Ignite.Core.Client.Cache;
    using NUnit.Framework;

    /// <summary>
    /// Thin client cache test in binary mode.
    /// </summary>
    public sealed class CacheTestKeepBinary : ClientTestBase
    {
        /// <summary>
        /// Tests the cache put / get with primitive data types.
        /// </summary>
        [Test]
        public void TestPutGetPrimitives()
        {
            using (var client = GetClient())
            {
                GetCache<string>().Put(1, "foo");

                var clientCache = client.GetCache<int?, string>(CacheName)
                    .WithKeepBinary<int?, string>();

                clientCache.Put(2, "bar");
                clientCache[3] = "baz";

                // Existing key.
                Assert.AreEqual("foo", clientCache.Get(1));
                Assert.AreEqual("foo", clientCache[1]);
                Assert.AreEqual("bar", clientCache[2]);
                Assert.AreEqual("baz", clientCache[3]);

                // Missing key.
                Assert.Throws<KeyNotFoundException>(() => clientCache.Get(-1));

                // Null key.
                Assert.Throws<ArgumentNullException>(() => clientCache.Get(null));

                // Null vs 0.
                var intCache = client.GetCache<int?, int?>(CacheName);
                intCache.Put(1, 0);
                Assert.AreEqual(0, intCache.Get(1));
            }
        }

        /// <summary>
        /// Tests the cache put / get for Empty object type.
        /// </summary>
        [Test]
        public void TestPutGetEmptyObject()
        {
            using (var client = GetClient())
            {
                var serverCache = GetCache<EmptyObject>();
                var clientCache = client.GetCache<int, EmptyObject>(CacheName)
                    .WithKeepBinary<int, IBinaryObject>();

                serverCache.Put(1, new EmptyObject());
                
                var res = clientCache.Get(1);
                Assert.IsNotNull(res);
                Assert.IsInstanceOf<EmptyObject>(res.Deserialize<object>());
            }
        }

        /// <summary>
        /// Tests the cache put / get with user data types.
        /// </summary>
        [Test]
        public void TestPutGetUserObjects([Values(true, false)] bool compactFooter)
        {
            var cfg = GetClientConfiguration();

            cfg.BinaryConfiguration = new BinaryConfiguration
            {
                CompactFooter = compactFooter
            };

            using (var client = Ignition.StartClient(cfg))
            {
                var person = new Person {Id = 100, Name = "foo"};
                var person2 = new Person2 {Id = 200, Name = "bar"};

                var serverCache = GetCache<Person>();
                var clientCache = client.GetCache<int?, Person>(CacheName)
                    .WithKeepBinary<int?, IBinaryObject>();

                Assert.AreEqual(CacheName, clientCache.Name);

                // Put through server cache.
                serverCache.Put(1, person);

                // Put through client cache.
                var binPerson = client.GetBinary().ToBinary<IBinaryObject>(person2);
                clientCache.Put(2, binPerson);

                // Read from client cache.
                Assert.AreEqual("foo", clientCache.Get(1).GetField<string>("Name"));
                Assert.AreEqual(100, clientCache[1].GetField<int>("Id"));
                Assert.AreEqual(200, clientCache[2].GetField<int>("Id"));

                // Read from server cache.
                Assert.AreEqual("foo", serverCache.Get(1).Name);
                Assert.AreEqual(100, serverCache[1].Id);
                Assert.AreEqual(200, serverCache[2].Id);

                // Null key or value.
                Assert.Throws<ArgumentNullException>(() => clientCache.Put(10, null));
                Assert.Throws<ArgumentNullException>(() => clientCache.Put(null, binPerson));
            }
        }

        /// <summary>
        /// Tests the GetAll method.
        /// </summary>
        [Test]
        public void TestGetAll()
        {
            var cache = GetBinaryCache();
            cache[1] = GetBinaryPerson(1);
            cache[2] = GetBinaryPerson(2);

            var res = cache.GetAll(new [] {1}).Single();
            Assert.AreEqual(1, res.Key);
            Assert.AreEqual(1, res.Value.GetField<int>("Id"));

            res = cache.GetAll(new [] {1, -1}).Single();
            Assert.AreEqual(1, res.Key);
            Assert.AreEqual(1, res.Value.GetField<int>("Id"));

            CollectionAssert.AreEquivalent(new[] {1, 2}, 
                cache.GetAll(new [] {1, 2, 3}).Select(x => x.Value.GetField<int>("Id")));
        }

        /// <summary>
        /// Tests the GetAndPut method.
        /// </summary>
        [Test]
        public void TestGetAndPut()
        {
            var cache = GetBinaryCache();

            Assert.IsFalse(cache.ContainsKey(1));

            var res = cache.GetAndPut(1, GetBinaryPerson(1));
            Assert.IsFalse(res.Success);
            Assert.IsNull(res.Value);

            Assert.IsTrue(cache.ContainsKey(1));

            res = cache.GetAndPut(1, GetBinaryPerson(2));
            Assert.IsTrue(res.Success);
            Assert.AreEqual(GetBinaryPerson(1), res.Value);

            Assert.AreEqual(GetBinaryPerson(2), cache[1]);
        }

        /// <summary>
        /// Tests the GetAndReplace method.
        /// </summary>
        [Test]
        public void TestGetAndReplace()
        {
            var cache = GetBinaryCache();

            Assert.IsFalse(cache.ContainsKey(1));

            var res = cache.GetAndReplace(1, GetBinaryPerson(1));
            Assert.IsFalse(res.Success);
            Assert.IsNull(res.Value);

            Assert.IsFalse(cache.ContainsKey(1));
            cache[1] = GetBinaryPerson(1);

            res = cache.GetAndReplace(1, GetBinaryPerson(2));
            Assert.IsTrue(res.Success);
            Assert.AreEqual(GetBinaryPerson(1), res.Value);

            Assert.AreEqual(GetBinaryPerson(2), cache[1]);
        }

        /// <summary>
        /// Tests the GetAndRemove method.
        /// </summary>
        [Test]
        public void TestGetAndRemove()
        {
            var cache = GetBinaryCache();

            Assert.IsFalse(cache.ContainsKey(1));

            var res = cache.GetAndRemove(1);
            Assert.IsFalse(res.Success);
            Assert.IsNull(res.Value);

            Assert.IsFalse(cache.ContainsKey(1));
            cache[1] = GetBinaryPerson(1);

            res = cache.GetAndRemove(1);
            Assert.IsTrue(res.Success);
            Assert.AreEqual(GetBinaryPerson(1), res.Value);

            Assert.IsFalse(cache.ContainsKey(1));
        }

        /// <summary>
        /// Tests the ContainsKey method.
        /// </summary>
        [Test]
        public void TestContainsKey()
        {
            var cache = Client.GetCache<int, Person>(CacheName).WithKeepBinary<IBinaryObject, int>();

            cache[GetBinaryPerson(25)] = 1;

            Assert.IsTrue(cache.ContainsKey(GetBinaryPerson(25)));
            Assert.IsFalse(cache.ContainsKey(GetBinaryPerson(26)));

            Assert.Throws<ArgumentNullException>(() => cache.ContainsKey(null));
        }

        /// <summary>
        /// Tests the ContainsKeys method.
        /// </summary>
        [Test]
        public void TestContainsKeys()
        {
            using (var client = GetClient())
            {
                var cache = client.GetCache<int, int>(CacheName);

                cache[1] = 1;
                cache[2] = 2;
                cache[3] = 3;

                Assert.IsTrue(cache.ContainsKeys(new[] {1}));
                Assert.IsTrue(cache.ContainsKeys(new[] {1, 2}));
                Assert.IsTrue(cache.ContainsKeys(new[] {2, 1}));
                Assert.IsTrue(cache.ContainsKeys(new[] {1, 2, 3}));
                Assert.IsTrue(cache.ContainsKeys(new[] {1, 3, 2}));

                Assert.IsFalse(cache.ContainsKeys(new[] {0}));
                Assert.IsFalse(cache.ContainsKeys(new[] {0, 1}));
                Assert.IsFalse(cache.ContainsKeys(new[] {1, 0}));
                Assert.IsFalse(cache.ContainsKeys(new[] {1, 2, 3, 0}));

                Assert.Throws<ArgumentNullException>(() => cache.ContainsKeys(null));
            }
        }

        /// <summary>
        /// Tests the PutIfAbsent method.
        /// </summary>
        [Test]
        public void TestPutIfAbsent()
        {
            using (var client = GetClient())
            {
                var cache = client.GetCache<int?, int?>(CacheName);

                Assert.IsFalse(cache.ContainsKey(1));

                var res = cache.PutIfAbsent(1, 1);
                Assert.IsTrue(res);
                Assert.AreEqual(1, cache[1]);

                res = cache.PutIfAbsent(1, 2);
                Assert.IsFalse(res);
                Assert.AreEqual(1, cache[1]);

                Assert.Throws<ArgumentNullException>(() => cache.PutIfAbsent(null, 1));
                Assert.Throws<ArgumentNullException>(() => cache.PutIfAbsent(1, null));
            }
        }

        /// <summary>
        /// Tests the GetAndPutIfAbsent method.
        /// </summary>
        [Test]
        public void TestGetAndPutIfAbsent()
        {
            using (var client = GetClient())
            {
                var cache = client.GetCache<int?, int?>(CacheName);

                Assert.IsFalse(cache.ContainsKey(1));

                var res = cache.GetAndPutIfAbsent(1, 1);
                Assert.IsFalse(res.Success);
                Assert.IsNull(res.Value);
                Assert.AreEqual(1, cache[1]);

                res = cache.GetAndPutIfAbsent(1, 2);
                Assert.IsTrue(res.Success);
                Assert.AreEqual(1, res.Value);
                Assert.AreEqual(1, cache[1]);

                Assert.Throws<ArgumentNullException>(() => cache.GetAndPutIfAbsent(null, 1));
                Assert.Throws<ArgumentNullException>(() => cache.GetAndPutIfAbsent(1, null));
            }
        }

        /// <summary>
        /// Tests the Replace method.
        /// </summary>
        [Test]
        public void TestReplace()
        {
            using (var client = GetClient())
            {
                var cache = client.GetCache<int?, int?>(CacheName);

                Assert.IsFalse(cache.ContainsKey(1));

                var res = cache.Replace(1, 1);
                Assert.IsFalse(res);
                Assert.IsFalse(cache.ContainsKey(1));

                cache[1] = 1;

                res = cache.Replace(1, 2);
                Assert.IsTrue(res);
                Assert.AreEqual(2, cache[1]);

                Assert.Throws<ArgumentNullException>(() => cache.Replace(null, 1));
                Assert.Throws<ArgumentNullException>(() => cache.Replace(1, null));
            }
        }

        /// <summary>
        /// Tests the Replace overload with additional argument.
        /// </summary>
        [Test]
        public void TestReplace2()
        {
            using (var client = GetClient())
            {
                var cache = client.GetCache<int?, int?>(CacheName);

                Assert.IsFalse(cache.ContainsKey(1));

                var res = cache.Replace(1, 1, 2);
                Assert.IsFalse(res);
                Assert.IsFalse(cache.ContainsKey(1));

                cache[1] = 1;

                res = cache.Replace(1, -1, 2);
                Assert.IsFalse(res);
                Assert.AreEqual(1, cache[1]);

                res = cache.Replace(1, 1, 2);
                Assert.IsTrue(res);
                Assert.AreEqual(2, cache[1]);

                Assert.Throws<ArgumentNullException>(() => cache.Replace(null, 1, 1));
                Assert.Throws<ArgumentNullException>(() => cache.Replace(1, null, 1));
                Assert.Throws<ArgumentNullException>(() => cache.Replace(1, 1, null));
            }
        }

        /// <summary>
        /// Tests the PutAll method.
        /// </summary>
        [Test]
        public void TestPutAll()
        {
            using (var client = GetClient())
            {
                // Primitives.
                var cache = client.GetCache<int?, int?>(CacheName);

                cache.PutAll(Enumerable.Range(1, 3).ToDictionary(x => (int?) x, x => (int?) x + 1));

                Assert.AreEqual(2, cache[1]);
                Assert.AreEqual(3, cache[2]);
                Assert.AreEqual(4, cache[3]);

                // Objects.
                var cache2 = client.GetCache<int, Container>(CacheName);

                var obj1 = new Container();
                var obj2 = new Container();
                var obj3 = new Container();

                obj1.Inner = obj2;
                obj2.Inner = obj1;
                obj3.Inner = obj2;

                cache2.PutAll(new Dictionary<int, Container>
                {
                    {1, obj1},
                    {2, obj2},
                    {3, obj3}
                });

                var res1 = cache2[1];
                var res2 = cache2[2];
                var res3 = cache2[3];

                Assert.AreEqual(res1, res1.Inner.Inner);
                Assert.AreEqual(res2, res2.Inner.Inner);
                Assert.IsNotNull(res3.Inner.Inner.Inner);

                // Nulls.
                Assert.Throws<ArgumentNullException>(() => cache.PutAll(null));

                Assert.Throws<IgniteClientException>(() => cache.PutAll(new[]
                {
                    new KeyValuePair<int?, int?>(null, 1)
                }));

                Assert.Throws<IgniteClientException>(() => cache.PutAll(new[]
                {
                    new KeyValuePair<int?, int?>(1, null)
                }));
            }
        }

        /// <summary>
        /// Tests the Clear method with a key argument.
        /// </summary>
        [Test]
        public void TestClearKey()
        {
            using (var client = GetClient())
            {
                var cache = client.GetCache<int, int>(CacheName)
                    .WithKeepBinary<IBinaryObject, int>();

                cache[GetBinaryPerson(1)] = 1;
                cache[GetBinaryPerson(2)] = 2;

                cache.Clear(GetBinaryPerson(1));
                Assert.IsFalse(cache.ContainsKey(GetBinaryPerson(1)));
                Assert.IsTrue(cache.ContainsKey(GetBinaryPerson(2)));

                cache.Clear(GetBinaryPerson(2));
                Assert.IsFalse(cache.ContainsKey(GetBinaryPerson(1)));
                Assert.IsFalse(cache.ContainsKey(GetBinaryPerson(2)));
            }
        }

        /// <summary>
        /// Tests the Remove method.
        /// </summary>
        [Test]
        public void TestRemove()
        {
            using (var client = GetClient())
            {
                var cache = client.GetCache<int?, int?>(CacheName);

                cache[1] = 1;
                cache[2] = 2;

                var res = cache.Remove(1);
                Assert.IsTrue(res);
                Assert.IsFalse(cache.ContainsKey(1));
                Assert.IsTrue(cache.ContainsKey(2));

                res = cache.Remove(2);
                Assert.IsTrue(res);
                Assert.IsFalse(cache.ContainsKey(1));
                Assert.IsFalse(cache.ContainsKey(2));

                res = cache.Remove(-1);
                Assert.IsFalse(res);

                Assert.Throws<ArgumentNullException>(() => cache.Remove(null));
            }
        }

        /// <summary>
        /// Tests the Remove method with value argument.
        /// </summary>
        [Test]
        public void TestRemoveKeyVal()
        {
            using (var client = GetClient())
            {
                var cache = client.GetCache<int?, int?>(CacheName);

                cache[1] = 1;
                cache[2] = 2;

                var res = cache.Remove(1, 0);
                Assert.IsFalse(res);

                res = cache.Remove(0, 0);
                Assert.IsFalse(res);

                res = cache.Remove(1, 1);
                Assert.IsTrue(res);
                Assert.IsFalse(cache.ContainsKey(1));
                Assert.IsTrue(cache.ContainsKey(2));

                res = cache.Remove(2, 2);
                Assert.IsTrue(res);
                Assert.IsFalse(cache.ContainsKey(1));
                Assert.IsFalse(cache.ContainsKey(2));

                res = cache.Remove(2, 2);
                Assert.IsFalse(res);

                Assert.Throws<ArgumentNullException>(() => cache.Remove(1, null));
                Assert.Throws<ArgumentNullException>(() => cache.Remove(null, 1));
            }
        }

        /// <summary>
        /// Tests the RemoveAll with a set of keys.
        /// </summary>
        [Test]
        public void TestRemoveReys()
        {
            using (var client = GetClient())
            {
                var cache = client.GetCache<int?, int?>(CacheName);
                var keys = Enumerable.Range(1, 10).Cast<int?>().ToArray();

                cache.PutAll(keys.ToDictionary(x => x, x => x));

                cache.RemoveAll(keys.Skip(2));
                CollectionAssert.AreEquivalent(keys.Take(2), cache.GetAll(keys).Select(x => x.Key));

                cache.RemoveAll(new int?[] {1});
                Assert.AreEqual(2, cache.GetAll(keys).Single().Value);

                cache.RemoveAll(keys);
                cache.RemoveAll(keys);

                Assert.AreEqual(0, cache.GetSize());

                Assert.Throws<ArgumentNullException>(() => cache.RemoveAll(null));
                Assert.Throws<IgniteClientException>(() => cache.RemoveAll(new int?[] {1, null}));
            }
        }

        /// <summary>
        /// Converts object to binary form.
        /// </summary>
        private IBinaryObject ToBinary(object o)
        {
            return Client.GetBinary().ToBinary<IBinaryObject>(o);
        }

        /// <summary>
        /// Gets the binary cache.
        /// </summary>
        private ICacheClient<int, IBinaryObject> GetBinaryCache()
        {
            return Client.GetCache<int, Person>(CacheName).WithKeepBinary<int, IBinaryObject>();
        }

        /// <summary>
        /// Gets the binary person.
        /// </summary>
        private IBinaryObject GetBinaryPerson(int id)
        {
            return ToBinary(new Person(id) {DateTime = DateTime.MinValue.ToUniversalTime()});
        }

        private class Container
        {
            public Container Inner;
        }

        public enum ByteEnum : byte
        {
            One = 1,
            Two = 2,
        }
    }
}