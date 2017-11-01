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

using System;

namespace Apache.Ignite.Core.Impl.Unmanaged.Jni
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Handle;

    /// <summary>
    /// Java -> .NET callback dispatcher.
    /// </summary>
    internal class Callbacks
    {
        /** Holds delegates so that GC does not collect them. */
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<Delegate> _delegates = new List<Delegate>();

        /** Holds Ignite instance-specific callbacks. */
        private readonly HandleRegistry _callbackRegistry = new HandleRegistry(100);

        /// <summary>
        /// Initializes a new instance of the <see cref="Callbacks"/> class.
        /// </summary>
        public Callbacks(Env env)
        {
            Debug.Assert(env != null);

            RegisterNatives(env);
        }

        /// <summary>
        /// Registers callback handlers.
        /// </summary>
        public long RegisterHandlers(UnmanagedCallbacks cbs)
        {
            return _callbackRegistry.AllocateCritical(cbs);
        }

        /// <summary>
        /// Registers native callbacks.
        /// </summary>
        private void RegisterNatives(Env env)
        {
            // Native callbacks are per-jvm.
            // Every callback (except ConsoleWrite) includes envPtr (third arg) to identify Ignite instance.

            using (var callbackUtils = env.FindClass(
                "org/apache/ignite/internal/processors/platform/callback/PlatformCallbackUtils"))
            {
                // Any signature works, but wrong one will cause segfault eventually.
                var methods = new[]
                {
                    GetNativeMethod("loggerLog", "(JILjava/lang/String;Ljava/lang/String;Ljava/lang/String;J)V",
                        (CallbackDelegates.LoggerLog) LoggerLog),

                    GetNativeMethod("loggerIsLevelEnabled", "(JI)Z",
                        (CallbackDelegates.LoggerIsLevelEnabled) LoggerIsLevelEnabled),

                    GetNativeMethod("consoleWrite", "(Ljava/lang/String;Z)V",
                        (CallbackDelegates.ConsoleWrite) ConsoleWrite),

                    GetNativeMethod("inLongOutLong", "(JIJ)J", (CallbackDelegates.InLongOutLong) InLongOutLong),

                    GetNativeMethod("inLongLongLongObjectOutLong", "(JIJJJLjava/lang/Object;)J",
                        (CallbackDelegates.InLongLongLongObjectOutLong) InLongLongLongObjectOutLong)
                };

                try
                {
                    env.RegisterNatives(callbackUtils, methods);
                }
                finally
                {
                    foreach (var nativeMethod in methods)
                    {
                        Marshal.FreeHGlobal(nativeMethod.Name);
                        Marshal.FreeHGlobal(nativeMethod.Signature);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the native method.
        /// </summary>
        private unsafe NativeMethod GetNativeMethod(string name, string sig, Delegate d)
        {
            _delegates.Add(d);

            return new NativeMethod
            {
                Name = (IntPtr)IgniteUtils.StringToUtf8Unmanaged(name),
                Signature = (IntPtr)IgniteUtils.StringToUtf8Unmanaged(sig),
                FuncPtr = Marshal.GetFunctionPointerForDelegate(d)
            };
        }

        private void LoggerLog(IntPtr env, IntPtr clazz, long igniteId, int level, IntPtr category, IntPtr error,
            IntPtr intPtr, long memPtr)
        {

        }

        private bool LoggerIsLevelEnabled(IntPtr env, IntPtr clazz, long ignteId, int level)
        {
            return false;
        }

        private long InLongLongLongObjectOutLong(IntPtr env, IntPtr clazz, long igniteId,
            int op, long arg1, long arg2, long arg3, IntPtr arg)
        {
            if (op == (int)UnmanagedCallbackOp.ExtensionInLongLongOutLong && arg1 == 1)
            {
                Console.WriteLine("OpPrepareDotNet");
                using (var inStream = IgniteManager.Memory.Get(arg2).GetStream())
                using (var outStream = IgniteManager.Memory.Get(arg3).GetStream())
                {
                    var writer = BinaryUtils.Marshaller.StartMarshal(outStream);

                    // Config.
                    // TODO
                    //TestUtils.GetTestConfiguration().Write(writer);

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

        private long InLongOutLong(IntPtr env, IntPtr clazz, long igniteId,
            int op, long arg)
        {
            // TODO
            return 0;
        }

        private static void ConsoleWrite(IntPtr envPtr, IntPtr clazz, IntPtr message, bool isError)
        {
            if (message != IntPtr.Zero)
            {
                var env = Jvm.Get().AttachCurrentThread();
                var msg = env.JStringToString(message);

                var target = isError ? Console.Error : Console.Out;
                target.Write(msg);
            }
        }
    }
}
