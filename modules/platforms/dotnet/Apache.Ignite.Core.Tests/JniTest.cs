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

namespace Apache.Ignite.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Apache.Ignite.Core.Impl;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Common;
    using Apache.Ignite.Core.Impl.Unmanaged;
    using Apache.Ignite.Core.Impl.Unmanaged.Jni;
    using Apache.Ignite.Core.Log;
    using NUnit.Framework;

    public class JniTest
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            IgniteUtils.LoadDlls(null, new NoopLogger());
        }

        [Test]
        public void TestStart()
        {
            Ignition.Start(TestUtils.GetTestConfiguration());
        }

        //[Test]
        public unsafe void TestIgnitionStart()
        {
            var opts = new List<string> {Classpath.CreateClasspath(forceTestClasspath: true)};
            var jvm = Jvm.GetOrCreate(opts);
            Assert.IsNotNull(jvm);

            // Should return existing.
            jvm = Jvm.GetOrCreate(opts);
            Assert.IsNotNull(jvm);

            var env = jvm.AttachCurrentThread();

            var ignition = env.FindClass("org/apache/ignite/internal/processors/platform/PlatformIgnition");
            Assert.AreNotEqual(IntPtr.Zero, ignition);

            var start = env.GetStaticMethodId(ignition, "start", "(Ljava/lang/String;Ljava/lang/String;IJJ)V");
            Assert.AreNotEqual(IntPtr.Zero, start);

            var args = new List<JavaValue>();

            var cbs = new UnmanagedCallbacks(new NoopLogger());
            var igniteId = jvm.RegisterCallbacks(cbs);

            using (var dataMem = IgniteManager.Memory.Allocate().GetStream())
            {
                // Cfg path: zero
                args.Add(new JavaValue {_object = IntPtr.Zero});

                // Name
                var gridNameUtf = IgniteUtils.StringToUtf8Unmanaged("myGrid"); // TODO: FreeHGlobal
                var gridName1 = env.NewStringUtf(new IntPtr(gridNameUtf));
                args.Add(new JavaValue(gridName1));

                // FactoryId
                args.Add(new JavaValue(1));

                // IgniteId
                args.Add(new JavaValue { _long = igniteId });

                // Additional data.
                dataMem.WriteBool(false);
                dataMem.WriteBool(false);
                args.Add(new JavaValue { _long = dataMem.SynchronizeOutput() });

                // Register callbacks.
                //RegisterNatives(env);

                env.CallStaticVoidMethod(ignition, start, args.ToArray());
            }
        }




        private class NoopLogger : ILogger
        {
            /** <inheritdoc /> */
            public void Log(LogLevel level, string message, object[] args, IFormatProvider formatProvider,
                string category, string nativeErrorInfo, Exception ex)
            {
                // No-op.
            }

            /** <inheritdoc /> */
            public bool IsEnabled(LogLevel level)
            {
                return false;
            }
        }

    }
}
