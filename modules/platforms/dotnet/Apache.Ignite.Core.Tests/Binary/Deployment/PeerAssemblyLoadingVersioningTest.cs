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
    using System.CodeDom.Compiler;
    using System.IO;
    using Apache.Ignite.Core.Impl;
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
            var exePath = Path.Combine(TempDir, "PeerTest1.exe");

            // Copy required assemblies.
            foreach (var type in new[] { typeof(Ignition), GetType() })
            {
                var loc = type.Assembly.Location;
                Assert.IsNotNull(loc);
                File.Copy(loc, Path.Combine(TempDir, type.Assembly.GetName().Name + ".dll"));
            }

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

class Program
{
    static void Main(string[] args)
    {
        using (var ignite = Ignition.Start(new IgniteConfiguration(TestUtils.GetTestConfiguration()) {ClientMode = true, IsPeerAssemblyLoadingEnabled = true}))
        {
            if (ignite.GetCompute().Call(new GridNameFunc()) != ""peerDeployTest"") throw new Exception(""fail"");
        }
    }
}

public class GridNameFunc : IComputeFunc<string> { public string Invoke() { return Ignition.GetIgnite().Name; } }
";

            var results = CodeDomProvider.CreateProvider("CSharp").CompileAssemblyFromSource(parameters, src);

            Assert.IsEmpty(results.Errors);

            using (Ignition.Start(new IgniteConfiguration(TestUtils.GetTestConfiguration())
            {
                IsPeerAssemblyLoadingEnabled = true,
                IgniteInstanceName = "peerDeployTest"
            }))
            {
                var proc = System.Diagnostics.Process.Start(exePath);

                Assert.IsNotNull(proc);

                proc.WaitForExit();

                Assert.AreEqual(0, proc.ExitCode);
            }
        }
    }
}
