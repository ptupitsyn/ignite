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
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Apache.Ignite.Core.Common;
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
                    var args = new List<JavaValue>
                    {
                        new JavaValue(cfgPath1),
                        new JavaValue(gridName1),
                        new JavaValue(InteropFactoryId),
                        new JavaValue {_long = igniteId}
                    };

                    // Additional data.
                    mem.WriteBool(false);
                    mem.WriteBool(false);
                    args.Add(new JavaValue { _long = mem.SynchronizeOutput() });

                    // OnStart receives InteropProcessor referece and stores it.
                    var methodId = env.Jvm.MethodId;
                    env.CallStaticVoidMethod(methodId.PlatformIgnition, methodId.PlatformIgnitionStart, 
                        args.ToArray());
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
                // TODO
                // return JNI.IgnitionStop(ctx, gridName0, cancel);
                return true;
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

            return jvm.AttachCurrentThread().CallLongMethod(
                target, jvm.MethodId.TargetInLongOutLong, new JavaValue(opType), new JavaValue(memPtr));
        }

        internal static long TargetInStreamOutLong(IUnmanagedTarget target, int opType, long memPtr)
        {
            var jvm = Jvm.Get();

            return jvm.AttachCurrentThread().CallLongMethod(
                target, jvm.MethodId.TargetInStreamOutLong, new JavaValue(opType), new JavaValue(memPtr));
        }

        internal static void TargetInStreamOutStream(IUnmanagedTarget target, int opType, long inMemPtr, long outMemPtr)
        {
            var jvm = Jvm.Get();

            jvm.AttachCurrentThread().CallVoidMethod(
                target, jvm.MethodId.TargetInStreamOutStream, new JavaValue(opType), 
                new JavaValue(inMemPtr), new JavaValue(outMemPtr));
        }

        internal static IUnmanagedTarget TargetInStreamOutObject(IUnmanagedTarget target, int opType, long inMemPtr)
        {
            var jvm = Jvm.Get();

            using (var lRef = jvm.AttachCurrentThread().CallObjectMethod(
                target, jvm.MethodId.TargetInStreamOutObject, new JavaValue(opType), new JavaValue(inMemPtr)))
            {
                return lRef.ToGlobal();
            }
        }

        internal static IUnmanagedTarget TargetInObjectStreamOutObjectStream(IUnmanagedTarget target, int opType, 
            IUnmanagedTarget arg, long inMemPtr, long outMemPtr)
        {
            var jvm = Jvm.Get();

            using (var lRef = jvm.AttachCurrentThread().CallObjectMethod(
                target, jvm.MethodId.TargetInObjectStreamOutObjectStream,
                new JavaValue(opType), new JavaValue(arg), new JavaValue(inMemPtr), new JavaValue(outMemPtr)))
            {
                return lRef.ToGlobal();
            }
        }

        internal static void TargetOutStream(IUnmanagedTarget target, int opType, long memPtr)
        {
            var jvm = Jvm.Get();

            jvm.AttachCurrentThread().CallVoidMethod(
                target, jvm.MethodId.TargetOutStream, new JavaValue(opType), new JavaValue(memPtr));
        }

        internal static IUnmanagedTarget TargetOutObject(IUnmanagedTarget target, int opType)
        {
            var jvm = Jvm.Get();

            using (var lRef = jvm.AttachCurrentThread().CallObjectMethod(
                target, jvm.MethodId.TargetOutObject, new JavaValue(opType)))
            {
                return lRef.ToGlobal();
            }
        }

        internal static void TargetInStreamAsync(IUnmanagedTarget target, int opType, long memPtr)
        {
            var jvm = Jvm.Get();

            jvm.AttachCurrentThread().CallVoidMethod(
                target, jvm.MethodId.TargetInStreamAsync, new JavaValue(opType), new JavaValue(memPtr));
        }

        internal static IUnmanagedTarget TargetInStreamOutObjectAsync(IUnmanagedTarget target, int opType, long memPtr)
        {
            var jvm = Jvm.Get();

            using (var lRef = jvm.AttachCurrentThread().CallObjectMethod(
                target, jvm.MethodId.TargetInStreamOutObjectAsync, new JavaValue(opType), new JavaValue(memPtr)))
            {
                return lRef.ToGlobal();
            }
        }

        #endregion

        #region NATIVE METHODS: MISCELANNEOUS

        internal static void Reallocate(long memPtr, int cap)
        {
            // TODO
            //int res = JNI.Reallocate(memPtr, cap);

            //if (res != 0)
            //    throw new IgniteException("Failed to reallocate external memory [ptr=" + memPtr + 
            //        ", capacity=" + cap + ']');
        }

        internal static IUnmanagedTarget Acquire(void* target)
        {
            return Jvm.Get().AttachCurrentThread().NewGlobalRef(new IntPtr(target));
        }

        internal static void ThrowToJava(Exception e)
        {
            char* msgChars = (char*)IgniteUtils.StringToUtf8Unmanaged(e.Message);

            try
            {
                // TODO:
                //JNI.ThrowToJava(ctx, msgChars);
            }
            finally
            {
                Marshal.FreeHGlobal(new IntPtr(msgChars));
            }
        }

        #endregion
    }
}
