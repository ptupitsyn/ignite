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
    using Apache.Ignite.Core.Impl;
    using Apache.Ignite.Core.Impl.Common;
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
        public void TestCreateJvm()
        {
            var jvm = Jvm.GetOrCreate();

            Assert.IsNotNull(jvm);
        }

        [Test]
        public void TestIgnitionStart()
        {
            /*
                jstring cfgPath0 = env->NewStringUTF(cfgPath);
                jstring name0 = env->NewStringUTF(name);

                env->CallStaticVoidMethod(
                    jvm->GetMembers().c_PlatformIgnition,
                    jvm->GetMembers().m_PlatformIgnition_start,
                    cfgPath0,
                    name0,
                    factoryId,
                    reinterpret_cast<long long>(&hnds),
                    dataPtr
                );
             */

            var jvm = Jvm.GetOrCreate(Classpath.CreateClasspath(forceTestClasspath: true));
            Assert.IsNotNull(jvm);

            var ignition = jvm.Methods.FindClass("org/apache/ignite/internal/processors/platform/PlatformIgnition");
            Assert.AreNotEqual(IntPtr.Zero, ignition);

            var start = jvm.Methods.GetStaticMethodId(ignition, "start", "(Ljava/lang/String;Ljava/lang/String;IJJ)V");
            Assert.AreNotEqual(IntPtr.Zero, start);

            // TODO: How to pass strings?
            jvm.Methods.CallStaticVoidMethod(ignition, start, new JavaValue());
        }

        private class NoopLogger : ILogger
        {
            /** <inheritdoc /> */
            public void Log(LogLevel level, string message, object[] args, IFormatProvider formatProvider, string category,
                string nativeErrorInfo, Exception ex)
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
