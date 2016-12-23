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

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable ClassWithVirtualMembersNeverInherited.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable VirtualMemberNeverOverridden.Global

namespace Apache.Ignite.EntityFramework.Tests
{
    using System.Data.Entity;
    using System.IO;
    using System.Linq;
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Tests;
    using NUnit.Framework;

    /// <summary>
    /// Tests with real world big model.
    /// </summary>
    public class EntityFrameworkCacheTestBigModel
    {
        /** */
        private static readonly string TempFile = Path.GetTempFileName();

        /** */
        private static readonly string ConnectionString = "Datasource = " + TempFile;

        /// <summary>
        /// Fixture set up.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            // Start 2 nodes.
            var cfg = TestUtils.GetTestConfiguration();
            var ignite = Ignition.Start(cfg);

            // Create SQL CE database in a temp file.
            using (var ctx = GetDbContext())
            {
                File.Delete(TempFile);
                ctx.Database.Create();
            }
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

        [Test]
        public void TestSimple()
        {
            Assert.AreEqual(0, GetDbContext().Roles.Count());
        }

        /// <summary>
        /// Gets the database context.
        /// </summary>
        private static BudgetContext GetDbContext()
        {
            return new BudgetContext(ConnectionString);
        }

        private class BudgetContext : DbContext
        {
            public BudgetContext(string nameOrConnectionString) : base(nameOrConnectionString)
            {
                // No-op.
            }

            public virtual DbSet<Region> Regions { get; set; }
            public virtual DbSet<Role> Roles { get; set; }
        }

        private class Region
        {
            public int Id { get; set; }
            public int RoleId { get; set; }
        }

        private class Role
        {
            public int Id { get; set; }
        }
    }
}
