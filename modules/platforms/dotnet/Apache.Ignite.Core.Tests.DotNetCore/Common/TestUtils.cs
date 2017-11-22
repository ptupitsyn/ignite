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

// ReSharper disable once CheckNamespace - use same TestUtils namespace to reuse tests from main project.
namespace Apache.Ignite.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using Apache.Ignite.Core.Discovery.Tcp;
    using Apache.Ignite.Core.Discovery.Tcp.Static;

    /// <summary>
    /// Test utils.
    /// </summary>
    internal static class TestUtils
    {
        private static readonly IList<string> JvmDebugOpts =
            new List<string>
            {
                "-Xdebug",
                "-Xnoagent",
                "-Djava.compiler=NONE",
                "-agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=5005"
            };

        /// <summary>
        /// Gets the static discovery.
        /// </summary>
        public static TcpDiscoverySpi GetStaticDiscovery()
        {
            return new TcpDiscoverySpi
            {
                IpFinder = new TcpDiscoveryStaticIpFinder
                {
                    Endpoints = new[] {"127.0.0.1:47500"}
                },
                SocketTimeout = TimeSpan.FromSeconds(0.3)
            };
        }

        /// <summary>
        /// Gets the default code-based test configuration.
        /// </summary>
        public static IgniteConfiguration GetTestConfiguration(string name = null)
        {
            return new IgniteConfiguration
            {
                DiscoverySpi = GetStaticDiscovery(),
                Localhost = "127.0.0.1",
                JvmOptions = JvmDebugOpts,
                IgniteInstanceName = name
            };
        }
    }
}