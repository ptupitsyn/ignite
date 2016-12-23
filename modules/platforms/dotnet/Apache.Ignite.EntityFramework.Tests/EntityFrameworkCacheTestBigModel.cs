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
    using System.Collections.Generic;
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
            // Start Ignite.
            Ignition.Start(TestUtils.GetTestConfiguration());

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

        [Test]
        public void TestBigQuery()
        {
            using (var ctx = GetDbContext())
            {
                var regionsAsm = ctx.Regions.Where(w => w.Roles.Any(a => a.Id == 1));

                var distributorToRegionAsmBindings = regionsAsm.SelectMany(s => s.Distributors);

                var allInvestTitle = new[] {"abc", "foo"};

                var resultAllBudgets = distributorToRegionAsmBindings
                    .Select(d => new {Distributor = d.DistributorName, RegionAsm = d.Region, d})
                    .SelectMany(dr => allInvestTitle,
                        (dr, t) => new {dr.Distributor, dr.RegionAsm, InvestTitle = t, dr.d});

                var allBudgets = resultAllBudgets
                    .Select(b => new BudgetResult
                    {
                        AsmRegionId = b.RegionAsm.Id,
                        AsmRegionName = b.RegionAsm.Name,
                        DistributorId = b.Distributor.Length,
                        DistributorName = b.Distributor,
                        RsmRegionId = b.RegionAsm.RegionExpands
                            .Where(w => w.Region.Roles.Any(a => a.Id == 5))
                            .Select(ss => ss.ParentRegionId)
                            .FirstOrDefault(),
                        RsmRegionName = b.RegionAsm.RegionExpands
                            .Where(w => w.Region.Roles.Any(a => a.Id == 6))
                            .Select(ss => ss.Region.Name)
                            .FirstOrDefault(),
                        InvestTitleId = b.InvestTitle.Length,
                        InvestTitleName = b.InvestTitle,
                    });

                var result = allBudgets.ToList();

                Assert.AreEqual(0, result.Count);
            }
        }

        /// <summary>
        /// Gets the database context.
        /// </summary>
        private static BudgetContext GetDbContext()
        {
            return new BudgetContext(ConnectionString);
        }

        [DbConfigurationType(typeof(MyDbConfiguration))]
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
            public string Name { get; set; }

            public virtual ICollection<Role> Roles { get; set; }
            public virtual ICollection<Distributor> Distributors { get; set; }
            public virtual ICollection<RegionExpand> RegionExpands { get; set; }
        }

        private class RegionExpand
        {
            public int Id { get; set; }
            public int RoleId { get; set; }
            public int RegionId { get; set; }
            public string Name { get; set; }
            public int ParentRegionId { get; set; }

            public virtual Role Role { get; set; }
            public virtual Region Region { get; set; }
        }

        private class Role
        {
            public int Id { get; set; }
            public int RegionId { get; set; }

            public virtual Region Region { get; set; }
        }

        private class Distributor
        {
            public int Id { get; set; }
            public int RegionId { get; set; }
            public string DistributorName { get; set; }

            public virtual Region Region { get; set; }
        }

        private class BudgetResult
        {
            public int AsmRegionId { get; set; }
            public string AsmRegionName { get; set; }
            public int DistributorId { get; set; }
            public string DistributorName { get; set; }
            public int RsmRegionId { get; set; }
            public string RsmRegionName { get; set; }
            public int InvestTitleId { get; set; }
            public string InvestTitleName { get; set; }
        }

        private class MyDbConfiguration : IgniteDbConfiguration
        {
            public MyDbConfiguration() : base(Ignition.GetIgnite(), null, null, null)
            {
                // No-op.
            }
        }
    }
}
