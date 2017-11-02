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
        public unsafe void TestIgnitionStart()
        {
            var jvm = Jvm.GetOrCreate(Classpath.CreateClasspath(forceTestClasspath: true));
            Assert.IsNotNull(jvm);

            // Should return existing.
            jvm = Jvm.GetOrCreate();
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

        private void RegisterNatives(Env env)
        {
            var callbackUtils = env.FindClass(
                    "org/apache/ignite/internal/processors/platform/callback/PlatformCallbackUtils");

            // Turns out any signature works, we can ignore parameters!
            var methods = new[]
            {
                GetNativeMethod("loggerLog", "(JILjava/lang/String;Ljava/lang/String;Ljava/lang/String;J)V",
                    (CallbackDelegates.LoggerLog) LoggerLog),

                GetNativeMethod("loggerIsLevelEnabled", "(JI)Z",
                    (CallbackDelegates.LoggerIsLevelEnabled) LoggerIsLevelEnabled),

                GetNativeMethod("consoleWrite", "(Ljava/lang/String;Z)V",
                    (CallbackDelegates.ConsoleWrite) ConsoleWrite),

                GetNativeMethod("inLongOutLong", "(JIJ)J", (Action) (() => { Console.WriteLine("woot"); })),

                GetNativeMethod("inLongLongLongObjectOutLong", "(JIJJJLjava/lang/Object;)J",
                    (CallbackDelegates.InLongLongLongObjectOutLong) InLongLongLongObjectOutLong)
            };

            env.RegisterNatives(callbackUtils, methods);
        }

        private static unsafe NativeMethod GetNativeMethod(string name, string sig, Delegate d)
        {
            return new NativeMethod
            {
                Name = (IntPtr) IgniteUtils.StringToUtf8Unmanaged(name),
                Signature = (IntPtr) IgniteUtils.StringToUtf8Unmanaged(sig),
                FuncPtr = Marshal.GetFunctionPointerForDelegate(d)
            };
        }

        private void LoggerLog(IntPtr env, IntPtr clazz, long igniteId, int level, IntPtr message, IntPtr category, IntPtr error, long memPtr)
        {
            
        }

        private void ConsoleWrite(IntPtr envPtr, IntPtr clazz, IntPtr message, bool isError)
        {
            // TODO: This causes crash some times (probably unreleased stuff or incorrect env handling)
            var env = Jvm.Get().AttachCurrentThread();
            var msg = env.JStringToString(message);

            Console.Write(msg);
        }

        private bool LoggerIsLevelEnabled(IntPtr env, IntPtr clazz, long igniteId, int level)
        {
            return false;
        }

        private long InLongLongLongObjectOutLong(IntPtr env, IntPtr clazz, long envPtr, 
            int op, long arg1, long arg2, long arg3, IntPtr arg)
        {
            if (op == (int) UnmanagedCallbackOp.ExtensionInLongLongOutLong && arg1 == 1)
            {
                Console.WriteLine("OpPrepareDotNet");
                using (var inStream = IgniteManager.Memory.Get(arg2).GetStream())
                using (var outStream = IgniteManager.Memory.Get(arg3).GetStream())
                {
                    var writer = BinaryUtils.Marshaller.StartMarshal(outStream);

                    // Config.
                    TestUtils.GetTestConfiguration().Write(writer);

                    // Beans.
                    writer.WriteInt(0);

                    outStream.SynchronizeOutput();

                    return 0;
                }

            }
            else
            {
                Console.WriteLine("UNKNOWN CALLBACK: " + op);
            }

            return 0;
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
