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

namespace Apache.Ignite.Core.Impl.Unmanaged
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Apache.Ignite.Core.Impl.Unmanaged.Jni;

    /// <summary>
    /// Unmanaged utility classes.
    /// </summary>
    internal static unsafe class UnmanagedUtils
    {
        /** Interop factory ID for .Net. */
        private const int InteropFactoryId = 1;

        #region NATIVE METHODS: PROCESSOR

        internal static void IgnitionStart(Env env, string cfgPath, string gridName,
            bool clientMode, bool userLogger, long igniteId)
        {
            using (var mem = IgniteManager.Memory.Allocate().GetStream())
            {
                mem.WriteBool(clientMode);
                mem.WriteBool(userLogger);

                var cfgPath0 = IgniteUtils.StringToUtf8Unmanaged(cfgPath);
                var gridName0 = IgniteUtils.StringToUtf8Unmanaged(gridName);

                var cfgPath1 = env.NewStringUtf(cfgPath0);
                var gridName1 = env.NewStringUtf(gridName0);

                try
                {
                    // Additional data.
                    mem.WriteBool(false);
                    mem.WriteBool(false);

                    long* args = stackalloc long[5];
                    args[0] = cfgPath != null ? cfgPath1.TargetAddr : 0;
                    args[1] = gridName != null ?  gridName1.TargetAddr : 0;
                    args[2] = InteropFactoryId;
                    args[3] = igniteId;
                    args[4] = mem.SynchronizeOutput();

                    // OnStart receives InteropProcessor referece and stores it.
                    var methodId = env.Jvm.MethodId;
                    env.CallStaticVoidMethod(methodId.PlatformIgnition, methodId.PlatformIgnitionStart, args);
                }
                finally
                {
                    if (cfgPath != null)
                    {
                        cfgPath1.Dispose();
                        Marshal.FreeHGlobal(new IntPtr(cfgPath0));
                    }

                    if (gridName != null)
                    {
                        gridName1.Dispose();
                        Marshal.FreeHGlobal(new IntPtr(gridName0));
                    }
                }
            }
        }

        internal static bool IgnitionStop(string gridName, bool cancel)
        {
            sbyte* gridName0 = IgniteUtils.StringToUtf8Unmanaged(gridName);

            try
            {
                var env = Jvm.Get().AttachCurrentThread();
                var methodId = env.Jvm.MethodId;

                using (var gridName1 = env.NewStringUtf(gridName0))
                {
                    long* args = stackalloc long[2];
                    args[0] = gridName1.TargetAddr;
                    args[1] = cancel ? 1 : 0;

                    return env.CallStaticBoolMethod(methodId.PlatformIgnition, methodId.PlatformIgnitionStop, args);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(new IntPtr(gridName0));
            }
        }

        #endregion

        #region NATIVE METHODS: TARGET

        internal static long TargetInLongOutLong(IUnmanagedTarget target, int opType, long memPtr)
        {
            var jvm = Jvm.Get();

            long* args = stackalloc long[2];
            args[0] = opType;
            args[1] = memPtr;

            return jvm.AttachCurrentThread().CallLongMethod(target, jvm.MethodId.TargetInLongOutLong, args);
        }

        internal static long TargetInStreamOutLong(IUnmanagedTarget target, int opType, long memPtr)
        {
            var jvm = Jvm.Get();

            long* args = stackalloc long[2];
            args[0] = opType;
            args[1] = memPtr;

            return jvm.AttachCurrentThread().CallLongMethod(target, jvm.MethodId.TargetInStreamOutLong, args);
        }

        internal static void TargetInStreamOutStream(IUnmanagedTarget target, int opType, long inMemPtr,
            long outMemPtr)
        {
            var jvm = Jvm.Get();

            long* args = stackalloc long[3];
            args[0] = opType;
            args[1] = inMemPtr;
            args[2] = outMemPtr;

            jvm.AttachCurrentThread().CallVoidMethod(target, jvm.MethodId.TargetInStreamOutStream, args);
        }

        internal static IUnmanagedTarget TargetInStreamOutObject(IUnmanagedTarget target, int opType, long inMemPtr)
        {
            var jvm = Jvm.Get();

            long* args = stackalloc long[2];
            args[0] = opType;
            args[1] = inMemPtr;

            return jvm.AttachCurrentThread().CallObjectMethod(
                target, jvm.MethodId.TargetInStreamOutObject, args).ToGlobal();
        }

        internal static IUnmanagedTarget TargetInObjectStreamOutObjectStream(IUnmanagedTarget target, int opType, 
            IUnmanagedTarget arg, long inMemPtr, long outMemPtr)
        {
            var jvm = Jvm.Get();

            long* args = stackalloc long[4];
            args[0] = opType;
            args[1] = (long) arg.Target;
            args[2] = inMemPtr;
            args[3] = outMemPtr;

            return jvm.AttachCurrentThread().CallObjectMethod(
                target, jvm.MethodId.TargetInObjectStreamOutObjectStream, args).ToGlobal();
        }

        internal static void TargetOutStream(IUnmanagedTarget target, int opType, long memPtr)
        {
            var jvm = Jvm.Get();

            long* args = stackalloc long[4];
            args[0] = opType;
            args[1] = memPtr;

            jvm.AttachCurrentThread().CallVoidMethod(target, jvm.MethodId.TargetOutStream, args);
        }

        internal static IUnmanagedTarget TargetOutObject(IUnmanagedTarget target, int opType)
        {
            var jvm = Jvm.Get();

            long opType0 = opType;

            return jvm.AttachCurrentThread().CallObjectMethod(
                target, jvm.MethodId.TargetOutObject, &opType0).ToGlobal();
        }

        internal static void TargetInStreamAsync(IUnmanagedTarget target, int opType, long memPtr)
        {
            var jvm = Jvm.Get();

            long* args = stackalloc long[4];
            args[0] = opType;
            args[1] = memPtr;

            jvm.AttachCurrentThread().CallVoidMethod(target, jvm.MethodId.TargetInStreamAsync, args);
        }

        internal static IUnmanagedTarget TargetInStreamOutObjectAsync(IUnmanagedTarget target, int opType, long memPtr)
        {
            var jvm = Jvm.Get();

            long* args = stackalloc long[4];
            args[0] = opType;
            args[1] = memPtr;

            return jvm.AttachCurrentThread().CallObjectMethod(
                target, jvm.MethodId.TargetInStreamOutObjectAsync, args).ToGlobal();
        }

        #endregion

        #region NATIVE METHODS: MISCELANNEOUS

        internal static void Reallocate(long memPtr, int cap)
        {
            var jvm = Jvm.Get();
            var methodId = jvm.MethodId;

            long* args = stackalloc long[4];
            args[0] = memPtr;
            args[1] = cap;

            jvm.AttachCurrentThread().CallStaticVoidMethod(methodId.PlatformUtils, methodId.PlatformUtilsReallocate,
                args);
        }

        internal static IUnmanagedTarget Acquire(void* target)
        {
            return Jvm.Get().AttachCurrentThread().NewGlobalRef(new IntPtr(target));
        }

        internal static void ThrowToJava(Exception e)
        {
            Debug.Assert(e != null);

            Jvm.Get().AttachCurrentThread().ThrowToJava(e.Message);
        }

        #endregion
    }
}
