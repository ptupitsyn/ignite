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

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable ClassWithVirtualMembersNeverInherited.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Apache.Ignite.EntityFramework.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Entity;
    using System.Data.Entity.Core.EntityClient;
    using System.Data.Entity.Infrastructure;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Transactions;
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Impl.EntityFramework;
    using Apache.Ignite.Core.Tests;
    using Apache.Ignite.EntityFramework;
    using NUnit.Framework;

    /// <summary>
    /// Integration test with temporary SQL CE database.
    /// </summary>
    public class EntityFrameworkCacheTest
    {
        /** */
        private static readonly string TempFile = Path.GetTempFileName();

        /** */
        private static readonly string ConnectionString = "Datasource = " + TempFile;

        /** */
        private static readonly DelegateCachingPolicy Policy = new DelegateCachingPolicy();

        /** */
        private ICache<object, object> _cache;

        /// <summary>
        /// Fixture set up.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            // Prepare SQL CE support.
            // Database.DefaultConnectionFactory = new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0");
            // context.Database.Log = s => Debug.WriteLine(s);

            // Start Ignite.
            var ignite = Ignition.Start(TestUtils.GetTestConfiguration());

            // Create SQL CE database in a temp file.
            using (var ctx = GetDbContext())
            {
                File.Delete(TempFile);
                ctx.Database.Create();
            }

            // Get the cache.
            _cache = ignite.GetCache<object, object>(null);
        }

        /// <summary>
        /// Fixture tear down.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            using (var ctx = GetDbContext())
            {
                ctx.Database.Delete();
            }

            Ignition.StopAll(true);
            File.Delete(TempFile);
        }

        /// <summary>
        /// Sets up the test.
        /// </summary>
        [SetUp]
        public void TestSetUp()
        {
            // Reset the policy.
            Policy.CanBeCachedFunc = null;
            Policy.CanBeCachedRowsFunc = null;
            Policy.GetExpirationTimeoutFunc = null;
            Policy.GetCachingStrategyFunc = null;

            // Clean up the db.
            using (var ctx = GetDbContext())
            {
                ctx.Blogs.RemoveRange(ctx.Blogs);
                ctx.Posts.RemoveRange(ctx.Posts);
                ctx.Tests.RemoveRange(ctx.Tests);

                ctx.SaveChanges();
            }

            using (var ctx = GetDbContext())
            {
                Assert.IsEmpty(ctx.Blogs);
                Assert.IsEmpty(ctx.Posts);
            }

            // Clear the cache.
            _cache.Clear();
        }

        /// <summary>
        /// Tests that caching actually happens.
        /// </summary>
        [Test]
        public void TestResultFromCache()
        {
            using (var ctx = GetDbContext())
            {
                // Add data.
                ctx.Posts.Add(new Post {Title = "Foo", Blog = new Blog(), PostId = 1});
                ctx.Posts.Add(new Post {Title = "Bar", Blog = new Blog(), PostId = 2});
                ctx.SaveChanges();

                Assert.AreEqual(new[] {"Foo"}, ctx.Posts.Where(x => x.Title == "Foo").Select(x => x.Title).ToArray());
                Assert.AreEqual(new[] {"Bar"}, ctx.Posts.Where(x => x.Title == "Bar").Select(x => x.Title).ToArray());

                // Alter cached data: swap cached values.
                var cachedData = _cache.Where(x => x.Value is EntityFrameworkCacheEntry).ToArray();

                Assert.AreEqual(2, cachedData.Length);

                _cache[cachedData[0].Key] = cachedData[1].Value;
                _cache[cachedData[1].Key] = cachedData[0].Value;

                // Verify.
                Assert.AreEqual(new[] {"Bar"}, ctx.Posts.Where(x => x.Title == "Foo").Select(x => x.Title).ToArray());
                Assert.AreEqual(new[] {"Foo"}, ctx.Posts.Where(x => x.Title == "Bar").Select(x => x.Title).ToArray());
            }
        }
    
        /// <summary>
        /// Tests the read-write strategy (default).
        /// </summary>
        [Test]
        public void TestReadWriteStrategy()
        {
            using (var ctx = GetDbContext())
            {
                var blog = new Blog
                {
                    Name = "Foo",
                    Posts = new List<Post>
                    {
                        new Post {Title = "My First Post", Content = "Hello World!"}
                    }
                };
                ctx.Blogs.Add(blog);

                Assert.AreEqual(2, ctx.SaveChanges());

                // Check that query works.
                Assert.AreEqual(1, ctx.Posts.Where(x => x.Title.StartsWith("My")).ToArray().Length);

                // Add new post to check invalidation.
                ctx.Posts.Add(new Post {BlogId = blog.BlogId, Title = "My Second Post", Content = "Foo bar."});
                Assert.AreEqual(1, ctx.SaveChanges());
                
                Assert.AreEqual(2, _cache.GetSize()); // Only entity set versions are in cache.

                Assert.AreEqual(2, ctx.Posts.Where(x => x.Title.StartsWith("My")).ToArray().Length);

                Assert.AreEqual(3, _cache.GetSize()); // Cached query added.

                // Delete post.
                ctx.Posts.Remove(ctx.Posts.First());
                Assert.AreEqual(1, ctx.SaveChanges());

                Assert.AreEqual(2, _cache.GetSize()); // Only entity set versions are in cache.
                Assert.AreEqual(1, ctx.Posts.Where(x => x.Title.StartsWith("My")).ToArray().Length);

                Assert.AreEqual(3, _cache.GetSize()); // Cached query added.

                // Modify post.
                Assert.AreEqual(0, ctx.Posts.Count(x => x.Title.EndsWith("updated")));

                ctx.Posts.Single().Title += " - updated";
                Assert.AreEqual(1, ctx.SaveChanges());

                Assert.AreEqual(2, _cache.GetSize()); // Only entity set versions are in cache.
                Assert.AreEqual(1, ctx.Posts.Count(x => x.Title.EndsWith("updated")));

                Assert.AreEqual(3, _cache.GetSize()); // Cached query added.
            }
        }

        /// <summary>
        /// Tests the read only strategy.
        /// </summary>
        [Test]
        public void TestReadOnlyStrategy()
        {
            // Set up a policy to cache Blogs as read-only and Posts as read-write.
            Policy.GetCachingStrategyFunc = q =>
                q.AffectedEntitySets.Count == 1 && q.AffectedEntitySets.Single().Name == "Blog"
                    ? DbCachingStrategy.ReadOnly
                    : DbCachingStrategy.ReadWrite;

            using (var ctx = GetDbContext())
            {
                ctx.Blogs.Add(new Blog
                {
                    Name = "Foo",
                    Posts = new List<Post>
                    {
                        new Post {Title = "Post"}
                    }
                });

                ctx.SaveChanges();

                // Update entities.
                Assert.AreEqual("Foo", ctx.Blogs.Single().Name);
                Assert.AreEqual("Post", ctx.Posts.Single().Title);

                ctx.Blogs.Single().Name += " - updated";
                ctx.Posts.Single().Title += " - updated";

                ctx.SaveChanges();
            }

            // Verify that cached result is not changed for blogs, but changed for posts.
            using (var ctx = GetDbContext())
            {
                // Raw SQL queries do not hit cache - verify that actual data is updated.
                Assert.AreEqual("Foo - updated", ctx.Database.SqlQuery<string>("select name from blogs").Single());
                Assert.AreEqual("Post - updated", ctx.Database.SqlQuery<string>("select title from posts").Single());

                // Check EF queries that hit cache.
                Assert.AreEqual("Foo", ctx.Blogs.Single().Name);
                Assert.AreEqual("Post - updated", ctx.Posts.Single().Title);

            }

            // Clear the cache and verify that actual value in DB is changed.
            _cache.Clear();

            using (var ctx = GetDbContext())
            {
                Assert.AreEqual("Foo - updated", ctx.Blogs.Single().Name);
                Assert.AreEqual("Post - updated", ctx.Posts.Single().Title);
            }
        }

        /// <summary>
        /// Tests the scalar queries.
        /// </summary>
        [Test]
        public void TestScalars()
        {
            using (var ctx = GetDbContext())
            {
                var blog = new Blog
                {
                    Name = "Foo",
                    Posts = new List<Post>
                    {
                        new Post {Title = "1"},
                        new Post {Title = "2"},
                        new Post {Title = "3"},
                        new Post {Title = "4"}
                    }
                };
                ctx.Blogs.Add(blog);

                Assert.AreEqual(5, ctx.SaveChanges());

                // Test sum and count.
                const string esql = "SELECT COUNT(1) FROM [BloggingContext].Posts";

                Assert.AreEqual(4, ctx.Posts.Count());
                Assert.AreEqual(4, ctx.Posts.Count(x => x.Content == null));
                Assert.AreEqual(4, GetEntityCommand(ctx, esql).ExecuteScalar());
                Assert.AreEqual(blog.BlogId * 4, ctx.Posts.Sum(x => x.BlogId));

                ctx.Posts.Remove(ctx.Posts.First());
                ctx.SaveChanges();

                Assert.AreEqual(3, ctx.Posts.Count());
                Assert.AreEqual(3, ctx.Posts.Count(x => x.Content == null));
                Assert.AreEqual(3, GetEntityCommand(ctx, esql).ExecuteScalar());
                Assert.AreEqual(blog.BlogId * 3, ctx.Posts.Sum(x => x.BlogId));
            }
        }

        /// <summary>
        /// Tests transactions created with BeginTransaction.
        /// </summary>
        [Test]
        public void TestTx()
        {
            // Check TX without commit.
            using (var ctx = GetDbContext())
            {
                using (ctx.Database.BeginTransaction())
                {
                    ctx.Posts.Add(new Post { Title = "Foo", Blog = new Blog() });
                    ctx.SaveChanges();

                    Assert.AreEqual(1, ctx.Posts.ToArray().Length);
                }
            }

            using (var ctx = GetDbContext())
            {
                Assert.AreEqual(0, ctx.Posts.ToArray().Length);
            }

            // Check TX with commit.
            using (var ctx = GetDbContext())
            {
                using (var tx = ctx.Database.BeginTransaction())
                {
                    ctx.Posts.Add(new Post { Title = "Foo", Blog = new Blog() });
                    ctx.SaveChanges();

                    Assert.AreEqual(1, ctx.Posts.ToArray().Length);

                    tx.Commit();

                    Assert.AreEqual(1, ctx.Posts.ToArray().Length);
                }
            }

            using (var ctx = GetDbContext())
            {
                Assert.AreEqual(1, ctx.Posts.ToArray().Length);
            }
        }

        /// <summary>
        /// Tests transactions created with TransactionScope.
        /// </summary>
        [Test]
        public void TestTxScope()
        {
            // Check TX without commit.
            using (new TransactionScope())
            {
                using (var ctx = GetDbContext())
                {
                    ctx.Posts.Add(new Post {Title = "Foo", Blog = new Blog()});
                    ctx.SaveChanges();
                }
            }

            using (var ctx = GetDbContext())
            {
                Assert.AreEqual(0, ctx.Posts.ToArray().Length);
            }

            // Check TX with commit.
            using (var tx = new TransactionScope())
            {
                using (var ctx = GetDbContext())
                {
                    ctx.Posts.Add(new Post {Title = "Foo", Blog = new Blog()});
                    ctx.SaveChanges();
                }

                tx.Complete();
            }

            using (var ctx = GetDbContext())
            {
                Assert.AreEqual(1, ctx.Posts.ToArray().Length);
            }
        }

        /// <summary>
        /// Tests the expiration.
        /// </summary>
        [Test]
        public void TestExpiration()
        {
            Policy.GetExpirationTimeoutFunc = qry => TimeSpan.FromSeconds(0.2);

            using (var ctx = GetDbContext())
            {
                ctx.Posts.Add(new Post {Title = "Foo", Blog = new Blog()});
                ctx.SaveChanges();

                Assert.AreEqual(1, ctx.Posts.ToArray().Length);
                Assert.AreEqual(3, _cache.GetSize());

                Thread.Sleep(250);
                Assert.AreEqual(2, _cache.GetSize());
            }
        }

        /// <summary>
        /// Tests the caching policy.
        /// </summary>
        [Test]
        public void TestCachingPolicy()
        {
            var funcs = new List<string>();

            var checkQry = (Action<DbQueryInfo>) (qry =>
                {
                    var set = qry.AffectedEntitySets.Single();

                    Assert.AreEqual("Post", set.Name);

                    Assert.AreEqual(1, qry.Parameters.Count);
                    Assert.AreEqual(-5, qry.Parameters[0].Value);
                    Assert.AreEqual(DbType.Int32, qry.Parameters[0].DbType);

                    Assert.IsTrue(qry.CommandText.EndsWith("WHERE [Extent1].[BlogId] > @p__linq__0"));
                }
            );

            Policy.CanBeCachedFunc = qry => {
                funcs.Add("CanBeCached");
                checkQry(qry);
                return true;
            };

            Policy.CanBeCachedRowsFunc = (qry, rows) => {
                funcs.Add("CanBeCachedRows");
                Assert.AreEqual(3, rows);
                checkQry(qry);
                return true;
            };

            Policy.GetCachingStrategyFunc = qry =>
            {
                funcs.Add("GetCachingStrategy");
                checkQry(qry);
                return DbCachingStrategy.ReadWrite;
            };

            Policy.GetExpirationTimeoutFunc = qry =>
            {
                funcs.Add("GetExpirationTimeout");
                checkQry(qry);
                return TimeSpan.MaxValue;
            };

            using (var ctx = GetDbContext())
            {
                var blog = new Blog();

                ctx.Posts.Add(new Post { Title = "Foo", Blog = blog });
                ctx.Posts.Add(new Post { Title = "Bar", Blog = blog });
                ctx.Posts.Add(new Post { Title = "Baz", Blog = blog });

                ctx.SaveChanges();

                int minId = -5;
                Assert.AreEqual(3, ctx.Posts.Where(x => x.BlogId > minId).ToArray().Length);

                // Check that policy methods are called in correct order with correct params.
                Assert.AreEqual(
                    new[] {"GetCachingStrategy", "CanBeCached", "CanBeCachedRows", "GetExpirationTimeout"},
                    funcs.ToArray());
            }
        }

        /// <summary>
        /// Tests the cache reader indirectly with an entity that has various field types.
        /// </summary>
        [Test]
        public void TestCacheReader()
        {
            // Tests all kinds of entity field types to cover ArrayDbDataReader.
            var test = GetTestEntity();

            using (var ctx = new BloggingContext(ConnectionString))
            {
                ctx.Tests.Add(test);
                ctx.SaveChanges();
            }

            // Use new context to ensure no first-level caching.
            using (var ctx = new BloggingContext(ConnectionString))
            {
                // Check default deserialization.
                var test0 = ctx.Tests.Single(x => x.Bool);
                Assert.AreEqual(test, test0);
            }
        }

        /// <summary>
        /// Tests the cache reader by calling it directly.
        /// </summary>
        [Test]
        public void TestCacheReaderRaw()
        {
            var test = GetTestEntity();

            using (var ctx = new BloggingContext(ConnectionString))
            {
                ctx.Tests.Add(test);
                ctx.SaveChanges();

                test = ctx.Tests.Single();
            }

            using (var ctx = new BloggingContext(ConnectionString))
            {
                var cmd = GetEntityCommand(ctx, "SELECT VALUE Test FROM BloggingContext.Tests AS Test");

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    // Check schema.
                    Assert.Throws<NotSupportedException>(() => reader.GetSchemaTable());
                    Assert.AreEqual(0, reader.Depth);
                    Assert.AreEqual(0, reader.RecordsAffected);
                    Assert.IsTrue(reader.HasRows);
                    Assert.IsFalse(reader.IsClosed);
                    Assert.AreEqual(11, reader.FieldCount);
                    Assert.AreEqual(11, reader.VisibleFieldCount);

                    // Read.
                    Assert.IsTrue(reader.Read());

                    // Test values array.
                    var vals = new object[reader.FieldCount];
                    reader.GetValues(vals);

                    Assert.AreEqual(test.Byte, vals[reader.GetOrdinal("Byte")]);
                    Assert.AreEqual(test.Short, vals[reader.GetOrdinal("Short")]);
                    Assert.AreEqual(test.ArrayReaderTestId, vals[reader.GetOrdinal("ArrayReaderTestId")]);
                    Assert.AreEqual(test.Long, vals[reader.GetOrdinal("Long")]);
                    Assert.AreEqual(test.Float, vals[reader.GetOrdinal("Float")]);
                    Assert.AreEqual(test.Double, vals[reader.GetOrdinal("Double")]);
                    Assert.AreEqual(test.Decimal, vals[reader.GetOrdinal("Decimal")]);
                    Assert.AreEqual(test.Bool, vals[reader.GetOrdinal("Bool")]);
                    Assert.AreEqual(test.String, vals[reader.GetOrdinal("String")]);
                    Assert.AreEqual(test.Guid, vals[reader.GetOrdinal("Guid")]);
                    Assert.AreEqual(test.DateTime, vals[reader.GetOrdinal("DateTime")]);
                }

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    // Read.
                    Assert.IsTrue(reader.Read());

                    // Test separate values.
                    Assert.AreEqual(test.ArrayReaderTestId, reader.GetInt32(0));
                    Assert.AreEqual(test.Byte, reader.GetByte(1));
                    Assert.AreEqual(test.Short, reader.GetInt16(2));
                    Assert.AreEqual(test.Long, reader.GetInt64(3));
                    Assert.AreEqual(test.Float, reader.GetFloat(4));
                    Assert.AreEqual(test.Double, reader.GetDouble(5));
                    Assert.AreEqual(test.Decimal, reader.GetDecimal(6));
                    Assert.AreEqual(test.Bool, reader.GetBoolean(7));
                    Assert.AreEqual(test.String, reader.GetString(8));
                    Assert.AreEqual(test.Guid, reader.GetGuid(9));
                    Assert.AreEqual(test.DateTime, reader.GetDateTime(10));
                }
            }
        }

        /// <summary>
        /// Tests the database context.
        /// </summary>
        [Test]
        public void TestDbContext()
        {
            using (var ctx = GetDbContext())
            {
                var objCtx = ((IObjectContextAdapter) ctx).ObjectContext;

                var script = objCtx.CreateDatabaseScript();
                Assert.IsTrue(script.StartsWith("CREATE TABLE \"Blogs\""));
            }
        }

        /// <summary>
        /// Executes the entity SQL.
        /// </summary>
        private static EntityCommand GetEntityCommand(BloggingContext ctx, string esql)
        {
            var objCtx = ((IObjectContextAdapter) ctx).ObjectContext;

            var conn = objCtx.Connection;
            conn.Open();

            var cmd = (EntityCommand) conn.CreateCommand();
            cmd.CommandText = esql;

            return cmd;
        }

        /// <summary>
        /// Gets the test entity.
        /// </summary>
        private static ArrayReaderTest GetTestEntity()
        {
            return new ArrayReaderTest
            {
                DateTime = DateTime.Today,
                Bool = true,
                Byte = 56,
                String = "z",
                Decimal = (decimal)5.6,
                Double = 7.8d,
                Float = -4.5f,
                Guid = Guid.NewGuid(),
                ArrayReaderTestId = -8,
                Long = 3,
                Short = 5
            };
        }

        /// <summary>
        /// Gets the database context.
        /// </summary>
        private static BloggingContext GetDbContext()
        {
            return new BloggingContext(ConnectionString);
        }

        private class MyDbConfiguration : IgniteDbConfiguration
        {
            public MyDbConfiguration() : base(Ignition.GetIgnite(), null, Policy)
            {
                // No-op.
            }
        }

        [DbConfigurationType(typeof(MyDbConfiguration))]
        private class BloggingContext : DbContext
        {
            public BloggingContext(string nameOrConnectionString) : base(nameOrConnectionString)
            {
                // No-op.
            }

            public virtual DbSet<Blog> Blogs { get; set; }
            public virtual DbSet<Post> Posts { get; set; }
            public virtual DbSet<ArrayReaderTest> Tests { get; set; }
        }

        private class Blog
        {
            public int BlogId { get; set; }
            public string Name { get; set; }

            public virtual List<Post> Posts { get; set; }
        }

        private class Post
        {
            public int PostId { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }

            public int BlogId { get; set; }
            public virtual Blog Blog { get; set; }
        }

        private class ArrayReaderTest
        {
            public byte Byte { get; set; }
            public short Short { get; set; }
            public int ArrayReaderTestId { get; set; }
            public long Long { get; set; }
            public float Float { get; set; }
            public double Double { get; set; }
            public decimal Decimal { get; set; }
            public bool Bool { get; set; }
            public string String { get; set; }
            public Guid Guid { get; set; }
            public DateTime DateTime { get; set; }

            private bool Equals(ArrayReaderTest other)
            {
                return Byte == other.Byte && Short == other.Short &&
                       ArrayReaderTestId == other.ArrayReaderTestId && Long == other.Long && 
                       Float.Equals(other.Float) && Double.Equals(other.Double) && 
                       Decimal == other.Decimal && Bool == other.Bool && String == other.String && 
                       Guid.Equals(other.Guid) && DateTime.Equals(other.DateTime);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((ArrayReaderTest) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Byte.GetHashCode();
                    hashCode = (hashCode*397) ^ Short.GetHashCode();
                    hashCode = (hashCode*397) ^ ArrayReaderTestId;
                    hashCode = (hashCode*397) ^ Long.GetHashCode();
                    hashCode = (hashCode*397) ^ Float.GetHashCode();
                    hashCode = (hashCode*397) ^ Double.GetHashCode();
                    hashCode = (hashCode*397) ^ Decimal.GetHashCode();
                    hashCode = (hashCode*397) ^ Bool.GetHashCode();
                    hashCode = (hashCode*397) ^ String.GetHashCode();
                    hashCode = (hashCode*397) ^ Guid.GetHashCode();
                    hashCode = (hashCode*397) ^ DateTime.GetHashCode();
                    return hashCode;
                }
            }
        }

        private class DelegateCachingPolicy : IDbCachingPolicy
        {
            public Func<DbQueryInfo, bool> CanBeCachedFunc { get; set; }

            public Func<DbQueryInfo, int, bool> CanBeCachedRowsFunc { get; set; }

            public Func<DbQueryInfo, TimeSpan> GetExpirationTimeoutFunc { get; set; }

            public Func<DbQueryInfo, DbCachingStrategy> GetCachingStrategyFunc { get; set; }

            public bool CanBeCached(DbQueryInfo queryInfo)
            {
                return CanBeCachedFunc == null || CanBeCachedFunc(queryInfo);
            }

            public bool CanBeCached(DbQueryInfo queryInfo, int rowCount)
            {
                return CanBeCachedRowsFunc == null || CanBeCachedRowsFunc(queryInfo, rowCount);
            }

            public TimeSpan GetExpirationTimeout(DbQueryInfo queryInfo)
            {
                return GetExpirationTimeoutFunc == null ? TimeSpan.MaxValue : GetExpirationTimeoutFunc(queryInfo);
            }

            public DbCachingStrategy GetCachingStrategy(DbQueryInfo queryInfo)
            {
                return GetCachingStrategyFunc == null ? DbCachingStrategy.ReadWrite : GetCachingStrategyFunc(queryInfo);
            }
        }
    }
}
