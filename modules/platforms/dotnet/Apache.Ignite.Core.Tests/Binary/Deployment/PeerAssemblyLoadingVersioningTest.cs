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

namespace Apache.Ignite.Core.Tests.Binary.Deployment
{
    using System;
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using System.IO;
    using Apache.Ignite.Core.Discovery.Tcp;
    using Apache.Ignite.Core.Discovery.Tcp.Static;
    using Apache.Ignite.Core.Impl;
    using Apache.Ignite.Core.Tests.Process;
    using NUnit.Framework;

    /// <summary>
    /// Tests assembly versioning: multiple assemblies with same name but different version should be supported.
    /// </summary>
    public class PeerAssemblyLoadingVersioningTest
    {
        private static readonly string TempDir = IgniteUtils.GetTempDirectoryName();

        /// <summary>
        /// Tears down the test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Directory.Delete(TempDir, true);
        }

        /// <summary>
        /// Tests that multiple versions of same assembly can be used on remote nodes.
        /// </summary>
        [Test]
        public void TestMultipleVersionsOfSameAssembly()
        {
            // Copy required assemblies.
            foreach (var type in new[] { typeof(Ignition), GetType() })
            {
                var loc = type.Assembly.Location;
                Assert.IsNotNull(loc);
                File.Copy(loc, Path.Combine(TempDir, type.Assembly.GetName().Name + ".dll"));
            }

            var exePath = Path.Combine(TempDir, "PeerTest1.exe");
            CompileClientNode(exePath);

            using (Ignition.Start(new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                IsPeerAssemblyLoadingEnabled = true,
                IgniteInstanceName = "peerDeployTest",
                DiscoverySpi = new TcpDiscoverySpi
                {
                    IpFinder = new TcpDiscoveryStaticIpFinder {Endpoints = new[] {"127.0.0.1:47500..47502"}},
                    SocketTimeout = TimeSpan.FromSeconds(0.3)
                }
            }))
            {
                var procStart = new ProcessStartInfo
                {
                    FileName = exePath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                var proc = Process.Start(procStart);
                Assert.IsNotNull(proc);

                IgniteProcess.AttachProcessConsoleReader(proc);

                Assert.IsTrue(proc.WaitForExit(30000));
                Assert.AreEqual(0, proc.ExitCode);
            }
        }

        /// <summary>
        /// Compiles the client node.
        /// </summary>
        private void CompileClientNode(string exePath)
        {
            var parameters = new CompilerParameters
            {
                GenerateExecutable = true,
                OutputAssembly = exePath,
                ReferencedAssemblies =
                {
                    typeof(Ignition).Assembly.Location,
                    GetType().Assembly.Location,
                    "System.dll"
                }
            };

            var src = @"
using System;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Tests;
using Apache.Ignite.Core.Discovery;
using Apache.Ignite.Core.Discovery.Tcp;
using Apache.Ignite.Core.Discovery.Tcp.Static;

class Program
{
    static void Main(string[] args)
    {
        using (var ignite = Ignition.Start(new IgniteConfiguration(TestUtils.GetTestConfiguration(false)) {ClientMode = true, IsPeerAssemblyLoadingEnabled = true,
                DiscoverySpi = new TcpDiscoverySpi { IpFinder = new TcpDiscoveryStaticIpFinder { Endpoints = new[] { ""127.0.0.1:47500..47502"" } }, SocketTimeout = TimeSpan.FromSeconds(0.3) }
}))
        {
            if (ignite.GetCompute().Call(new GridNameFunc()) != ""peerDeployTest"") throw new Exception(""fail"");
        }
    }
}

public class GridNameFunc : IComputeFunc<string> { public string Invoke() { return Ignition.GetIgnite().Name; } }
";

            var results = CodeDomProvider.CreateProvider("CSharp").CompileAssemblyFromSource(parameters, src);

            Assert.IsEmpty(results.Errors);
        }
    }
}
