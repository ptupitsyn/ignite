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
    using System.Collections.Generic;
    using Core;
    using Core.Binary;

    /// <summary>
    /// This example demonstrates several put-get operations on Ignite cache
    /// with binary values. Note that binary object can be retrieved in
    /// fully-deserialized form or in binary object format using special
    /// cache projection.
    /// <para />
    /// TODO: "dotnet run" - with arg?
    /// TODO: standalone Java node
    /// </summary>
    public class PutGetExample
    {
        /// <summary>Cache name.</summary>
        private const string CacheName = "dotnet_cache_put_get";

        /// <summary>
        /// Runs the example.
        /// </summary>
        public static void Run()
        {
            using (var ignite = Ignition.StartFromApplicationConfiguration())
            {
                Console.WriteLine();
                Console.WriteLine(">>> Cache put-get example started.");

                // Clean up caches on all nodes before run.
                ignite.GetOrCreateCache<object, object>(CacheName).Clear();

                PutGet(ignite);
                PutGetBinary(ignite);
                PutAllGetAll(ignite);
                PutAllGetAllBinary(ignite);

                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine(">>> Example finished, press any key to exit ...");
            Console.ReadKey();
        }

        /// <summary>
        /// Execute individual Put and Get.
        /// </summary>
        /// <param name="ignite">Ignite instance.</param>
        private static void PutGet(IIgnite ignite)
        {
            var cache = ignite.GetCache<int, Organization>(CacheName);

            // Create new Organization to store in cache.
            var org = new Organization("Microsoft");

            // Put created data entry to cache.
            cache.Put(1, org);

            // Get recently created employee as a strongly-typed fully de-serialized instance.
            var orgFromCache = cache.Get(1);

            Console.WriteLine();
            Console.WriteLine(">>> Retrieved organization instance from cache: " + orgFromCache);
        }

        /// <summary>
        /// Execute individual Put and Get, getting value in binary format, without de-serializing it.
        /// </summary>
        /// <param name="ignite">Ignite instance.</param>
        private static void PutGetBinary(IIgnite ignite)
        {
            var cache = ignite.GetCache<int, Organization>(CacheName);

            // Create new Organization to store in cache.
            var org = new Organization("Microsoft");

            // Put created data entry to cache.
            cache.Put(1, org);

            // Create projection that will get values as binary objects.
            var binaryCache = cache.WithKeepBinary<int, IBinaryObject>();

            // Get recently created organization as a binary object.
            var binaryOrg = binaryCache.Get(1);

            // Get organization's name from binary object (note that  object doesn't need to be fully deserialized).
            var name = binaryOrg.GetField<string>("name");

            Console.WriteLine();
            Console.WriteLine(">>> Retrieved organization name from binary object: " + name);
        }

        /// <summary>
        /// Execute bulk Put and Get operations.
        /// </summary>
        /// <param name="ignite">Ignite instance.</param>
        private static void PutAllGetAll(IIgnite ignite)
        {
            var cache = ignite.GetCache<int, Organization>(CacheName);

            // Create new Organizations to store in cache.
            var org1 = new Organization("Microsoft");
            var org2 = new Organization("Red Cross");

            var map = new Dictionary<int, Organization> { { 1, org1 }, { 2, org2 } };

            // Put created data entries to cache.
            cache.PutAll(map);

            // Get recently created organizations as a strongly-typed fully de-serialized instances.
            var mapFromCache = cache.GetAll(new List<int> { 1, 2 });

            Console.WriteLine();
            Console.WriteLine(">>> Retrieved organization instances from cache:");

            foreach (var org in mapFromCache)
                Console.WriteLine(">>>     " + org.Value);
        }

        /// <summary>
        /// Execute bulk Put and Get operations getting values in binary format, without de-serializing it.
        /// </summary>
        /// <param name="ignite">Ignite instance.</param>
        private static void PutAllGetAllBinary(IIgnite ignite)
        {
            var cache = ignite.GetCache<int, Organization>(CacheName);

            // Create new Organizations to store in cache.
            var org1 = new Organization("Microsoft");
            var org2 = new Organization("Red Cross");

            var map = new Dictionary<int, Organization> { { 1, org1 }, { 2, org2 } };

            // Put created data entries to cache.
            cache.PutAll(map);

            // Create projection that will get values as binary objects.
            var binaryCache = cache.WithKeepBinary<int, IBinaryObject>();

            // Get recently created organizations as binary objects.
            var binaryMap = binaryCache.GetAll(new List<int> { 1, 2 });

            Console.WriteLine();
            Console.WriteLine(">>> Retrieved organization names from binary objects:");

            foreach (var pair in binaryMap)
                Console.WriteLine(">>>     " + pair.Value.GetField<string>("name"));
        }
    }
}
