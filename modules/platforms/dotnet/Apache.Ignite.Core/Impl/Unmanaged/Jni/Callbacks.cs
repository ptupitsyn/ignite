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

using System;

namespace Apache.Ignite.Core.Impl.Unmanaged.Jni
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Apache.Ignite.Core.Impl.Handle;

    /// <summary>
    /// Java -> .NET callback dispatcher.
    /// Instance of this class should only exist once per process, in the default AppDomain.
    /// </summary>
    internal class Callbacks : MarshalByRefObject
    {
        /** Holds delegates so that GC does not collect them. */
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<Delegate> _delegates = new List<Delegate>();

        /** Holds Ignite instance-specific callbacks. */
        private readonly HandleRegistry _callbackRegistry = new HandleRegistry(100);

        /** Console writers. */
        private readonly ConcurrentDictionary<long, ConsoleWriter> _consoleWriters
            = new ConcurrentDictionary<long, ConsoleWriter>();

        /** Console writer id generator. */
        private long _consoleWriterId;

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
            Debug.Assert(cbs != null);

            // TODO: Unregister on stop.
            return _callbackRegistry.AllocateCritical(cbs);
        }

        /// <summary>
        /// Releases callback handlers.
        /// </summary>
        public void ReleaseHandlers(long igniteId)
        {
            _callbackRegistry.Release(igniteId);
        }

        /// <summary>
        /// Registers the console writer.
        /// </summary>
        public long RegisterConsoleWriter(ConsoleWriter writer)
        {
            Debug.Assert(writer != null);

            var id = Interlocked.Increment(ref _consoleWriterId);

            var res = _consoleWriters.TryAdd(id, writer);
            Debug.Assert(res);

            return id;
        }

        /// <summary>
        /// Registers the console writer.
        /// </summary>
        public void ReleaseConsoleWriter(long id)
        {
            ConsoleWriter writer;
            var res = _consoleWriters.TryRemove(id, out writer);
            Debug.Assert(res);
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

        private void LoggerLog(IntPtr envPtr, IntPtr clazz, long igniteId, int level, IntPtr message, IntPtr category,
            IntPtr errorInfo, long memPtr)
        {
            var cbs = _callbackRegistry.Get<UnmanagedCallbacks>(igniteId, true);
            var env = Jvm.Get().AttachCurrentThread();

            var message0 = env.JStringToString(message);
            var category0 = env.JStringToString(category);
            var errorInfo0 = env.JStringToString(errorInfo);

            cbs.LoggerLog(level, message0, category0, errorInfo0, memPtr);
        }

        private bool LoggerIsLevelEnabled(IntPtr env, IntPtr clazz, long igniteId, int level)
        {
            var cbs = _callbackRegistry.Get<UnmanagedCallbacks>(igniteId, true);

            return cbs.LoggerIsLevelEnabled(level);
        }

        private long InLongLongLongObjectOutLong(IntPtr env, IntPtr clazz, long igniteId,
            int op, long arg1, long arg2, long arg3, IntPtr arg)
        {
            var cbs = _callbackRegistry.Get<UnmanagedCallbacks>(igniteId, true);

            return cbs.InLongLongLongObjectOutLong(op, arg1, arg2, arg3, arg);
        }

        private long InLongOutLong(IntPtr env, IntPtr clazz, long igniteId,
            int op, long arg)
        {
            var cbs = _callbackRegistry.Get<UnmanagedCallbacks>(igniteId, true);

            return cbs.InLongOutLong(op, arg);
        }

        private void ConsoleWrite(IntPtr envPtr, IntPtr clazz, IntPtr message, bool isError)
        {
            if (message != IntPtr.Zero)
            {
                // Each domain registers it's own writer.
                var writer = _consoleWriters.Select(x => x.Value).FirstOrDefault();

                if (writer != null)
                {                    
                    var env = Jvm.Get().AttachCurrentThread();
                    var msg = env.JStringToString(message);

                    writer.Write(msg, isError);
                }
            }
        }
    }
}
