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

// ReSharper disable PossibleNullReferenceException
namespace Apache.Ignite.Core.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using NUnit.Engine;

    public static class TestRunner
    {
        [STAThread]
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Debug.AutoFlush = true;

            if (args.Length == 2)
            {
                //Debugger.Launch();
                var testClass = Type.GetType(args[0]);
                var method = args[1];

                if (testClass == null || testClass.GetMethods().All(x => x.Name != method))
                    throw new InvalidOperationException("Failed to find method: " + testClass + "." + method);

                Environment.ExitCode = TestOne(testClass, method);
                return;
            }

            TestOne(typeof(ConsoleRedirectTest), "TestMultipleDomains");
        }

        private static int TestOne(Type testClass, string method)
        {
            // Create runner.
            var engine = TestEngineActivator.CreateInstance();
            var package = new TestPackage(testClass.Assembly.Location);
            var runner = engine.GetRunner(package);

            // Filter to specified test.
            var filterBuilder = engine.Services.GetService<ITestFilterService>().GetTestFilterBuilder();
            filterBuilder.AddTest(testClass.FullName + "." + method);
            
            // Run.
            var testResult = runner.Run(new TestEventListener(), filterBuilder.GetFilter());

            var total = int.Parse(testResult.Attributes["total"].Value);
            var passed = int.Parse(testResult.Attributes["passed"].Value);

            if (total == 1 && passed == 1)
            {
                // Success.
                return 0;
            }

            // Failure.
            Console.WriteLine("Tests total: {0}, passed: {1}", total, passed);

            return 1;
        }

        private class TestEventListener : ITestEventListener
        {
            public void OnTestEvent(string report)
            {
                // No-op.
            }
        }
    }
}