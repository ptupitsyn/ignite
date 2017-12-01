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

namespace Apache.Ignite.Examples
{
    using System;
    using System.Linq;
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Affinity;
    using Apache.Ignite.Core.Cache.Configuration;
    using Apache.Ignite.Core.Cache.Query;
    using Apache.Ignite.Linq;

    /// <summary>
    /// This example populates cache with sample data and runs several LINQ queries over this data.
    /// <para />
    /// 1) Build the project Apache.Ignite.ExamplesDll (select it -> right-click -> Build).
    ///    Apache.Ignite.ExamplesDll.dll must appear in %IGNITE_HOME%/platforms/dotnet/examples/Apache.Ignite.ExamplesDll/bin/${Platform]/${Configuration} folder.
    /// 2) Set this class as startup object (Apache.Ignite.Examples project -> right-click -> Properties ->
    ///     Application -> Startup object);
    /// 3) Start example (F5 or Ctrl+F5).
    /// <para />
    /// This example can be run with standalone Apache Ignite.NET node:
    /// 1) Run %IGNITE_HOME%/platforms/dotnet/bin/Apache.Ignite.exe:
    /// Apache.Ignite.exe -configFileName=platforms\dotnet\examples\apache.ignite.examples\app.config -assembly=[path_to_Apache.Ignite.ExamplesDll.dll]
    /// 2) Start example.
    /// </summary>
    public class LinqExample
    {
        /// <summary>Organization cache name.</summary>
        private const string OrganizationCacheName = "dotnet_cache_query_organization";

        /// <summary>Employee cache name.</summary>
        private const string EmployeeCacheName = "dotnet_cache_query_employee";

        /// <summary>Colocated employee cache name.</summary>
        private const string EmployeeCacheNameColocated = "dotnet_cache_query_employee_colocated";

        [STAThread]
        public static void Run()
        {
            var ignite = Ignition.GetIgnite() ?? Ignition.StartFromApplicationConfiguration();

            Console.WriteLine();
            Console.WriteLine(">>> Cache LINQ example started.");

            var employeeCache = ignite.GetOrCreateCache<int, Employee>(
                new CacheConfiguration(EmployeeCacheName, typeof(Employee)));

            var employeeCacheColocated = ignite.GetOrCreateCache<AffinityKey, Employee>(
                new CacheConfiguration(EmployeeCacheNameColocated, typeof(Employee)));

            var organizationCache = ignite.GetOrCreateCache<int, Organization>(
                new CacheConfiguration(OrganizationCacheName, new QueryEntity(typeof(int), typeof(Organization))));

            // Populate cache with sample data entries.
            PopulateCache(employeeCache);
            PopulateCache(employeeCacheColocated);
            PopulateCache(organizationCache);

            // Run SQL query example.
            QueryExample(employeeCache);

            // Run compiled SQL query example.
            CompiledQueryExample(employeeCache);

            // Run SQL query with join example.
            JoinQueryExample(employeeCacheColocated, organizationCache);

            // Run SQL query with distributed join example.
            DistributedJoinQueryExample(employeeCache, organizationCache);

            // Run SQL fields query example.
            FieldsQueryExample(employeeCache);

            Console.WriteLine();

            Console.WriteLine();
            Console.WriteLine(">>> Example finished, press any key to exit ...");
            Console.ReadKey();
        }

        /// <summary>
        /// Queries employees that have provided ZIP code in address.
        /// </summary>
        /// <param name="cache">Cache.</param>
        private static void QueryExample(ICache<int, Employee> cache)
        {
            const int minSalary = 10000;

            var qry = cache.AsCacheQueryable().Where(emp => emp.Value.Salary > minSalary);

            Console.WriteLine();
            Console.WriteLine($">>> Employees with salary > {minSalary}:");

            foreach (var entry in qry)
                Console.WriteLine(">>>    " + entry.Value);
        }

        /// <summary>
        /// Queries employees that have provided ZIP code in address with a compiled query.
        /// </summary>
        /// <param name="cache">Cache.</param>
        private static void CompiledQueryExample(ICache<int, Employee> cache)
        {
            const int minSalary = 10000;

            var cache0 = cache.AsCacheQueryable();

            // Compile cache query to eliminate LINQ overhead on multiple runs.
            Func<int, IQueryCursor<ICacheEntry<int, Employee>>> qry =
                CompiledQuery.Compile((int ms) => cache0.Where(emp => emp.Value.Salary > ms));

            Console.WriteLine();
            Console.WriteLine($">>> Employees with salary > {minSalary} using compiled query:");

            foreach (var entry in qry(minSalary))
                Console.WriteLine(">>>    " + entry.Value);
        }

        /// <summary>
        /// Queries employees that work for organization with provided name.
        /// </summary>
        /// <param name="employeeCache">Employee cache.</param>
        /// <param name="organizationCache">Organization cache.</param>
        private static void JoinQueryExample(ICache<AffinityKey, Employee> employeeCache,
            ICache<int, Organization> organizationCache)
        {
            const string orgName = "Apache";

            var employees = employeeCache.AsCacheQueryable();
            var organizations = organizationCache.AsCacheQueryable();

            var qry =
                from employee in employees
                from organization in organizations
                where employee.Value.OrganizationId == organization.Key && organization.Value.Name == orgName
                select employee;


            Console.WriteLine();
            Console.WriteLine(">>> Employees working for " + orgName + ":");

            foreach (var entry in qry)
                Console.WriteLine(">>>     " + entry.Value);
        }

        /// <summary>
        /// Queries employees that work for organization with provided name.
        /// </summary>
        /// <param name="employeeCache">Employee cache.</param>
        /// <param name="organizationCache">Organization cache.</param>
        private static void DistributedJoinQueryExample(ICache<int, Employee> employeeCache,
            ICache<int, Organization> organizationCache)
        {
            const string orgName = "Apache";

            var queryOptions = new QueryOptions {EnableDistributedJoins = true};

            var employees = employeeCache.AsCacheQueryable(queryOptions);
            var organizations = organizationCache.AsCacheQueryable(queryOptions);

            var qry =
                from employee in employees
                from organization in organizations
                where employee.Value.OrganizationId == organization.Key && organization.Value.Name == orgName
                select employee;


            Console.WriteLine();
            Console.WriteLine(">>> Employees working for " + orgName + ":");

            foreach (var entry in qry)
                Console.WriteLine(">>>     " + entry.Value);
        }

        /// <summary>
        /// Queries names and salaries for all employees.
        /// </summary>
        /// <param name="cache">Cache.</param>
        private static void FieldsQueryExample(ICache<int, Employee> cache)
        {
            var qry = cache.AsCacheQueryable().Select(entry => new {entry.Value.Name, entry.Value.Salary});

            Console.WriteLine();
            Console.WriteLine(">>> Employee names and their salaries:");

            foreach (var row in qry)
                Console.WriteLine(">>>     [Name=" + row.Name + ", salary=" + row.Salary + ']');
        }

        /// <summary>
        /// Populate cache with data for this example.
        /// </summary>
        /// <param name="cache">Cache.</param>
        private static void PopulateCache(ICache<int, Organization> cache)
        {
            cache.Put(1, new Organization("Apache"));
            cache.Put(2, new Organization("Microsoft"));
        }

        /// <summary>
        /// Populate cache with data for this example.
        /// </summary>
        /// <param name="cache">Cache.</param>
        private static void PopulateCache(ICache<AffinityKey, Employee> cache)
        {
            cache.Put(new AffinityKey(1, 1), new Employee("James Wilson", 12500, 1));
            cache.Put(new AffinityKey(2, 1), new Employee("Daniel Adams", 11000, 1));
            cache.Put(new AffinityKey(3, 1), new Employee("Cristian Moss", 12500, 1));
            cache.Put(new AffinityKey(4, 2), new Employee("Allison Mathis", 25300, 2));
            cache.Put(new AffinityKey(5, 2), new Employee("Breana Robbin", 6500, 2));
            cache.Put(new AffinityKey(6, 2), new Employee("Philip Horsley", 19800, 2));
            cache.Put(new AffinityKey(7, 2), new Employee("Brian Peters", 10600, 2));
        }

        /// <summary>
        /// Populate cache with data for this example.
        /// </summary>
        /// <param name="cache">Cache.</param>
        private static void PopulateCache(ICache<int, Employee> cache)
        {
            cache.Put(1, new Employee("James Wilson", 12500, 1));
            cache.Put(2, new Employee("Daniel Adams", 11000, 1));
            cache.Put(3, new Employee("Cristian Moss", 12500, 1));
            cache.Put(4, new Employee("Allison Mathis", 25300, 2));
            cache.Put(5, new Employee("Breana Robbin", 6500, 2));
            cache.Put(6, new Employee("Philip Horsley", 19800, 2));
            cache.Put(7, new Employee("Brian Peters", 10600, 2));
        }
    }
}
