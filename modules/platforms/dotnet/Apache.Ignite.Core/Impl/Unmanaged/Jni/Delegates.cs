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

namespace Apache.Ignite.Core.Impl.Unmanaged.Jni
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;

    [SuppressUnmanagedCodeSecurity]
    internal static class Delegates // TODO: remove prefix from all Jni* classes
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate JNIResult CallStaticVoidMethod(
            IntPtr env, IntPtr clazz, IntPtr methodId, params JavaValue[] args);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate JNIResult CallStaticVoidMethodV(
            IntPtr env, IntPtr clazz, IntPtr methodId, IntPtr vaList);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr NewGlobalRef(IntPtr env, IntPtr lobj);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void DeleteLocalRef(IntPtr env, IntPtr lref);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [SuppressUnmanagedCodeSecurity]
        internal delegate void DeleteGlobalRef(IntPtr env, IntPtr gref);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr FindClass(IntPtr env, [MarshalAs(UnmanagedType.LPStr)] string name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr GetStaticMethodID(IntPtr env, IntPtr clazz,
            [MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string sig);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr GetMethodID(IntPtr env, IntPtr clazz, [MarshalAs(UnmanagedType.LPStr)] string name,
            [MarshalAs(UnmanagedType.LPStr)] string sig);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr NewStringUTF(IntPtr env, IntPtr utf);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr ExceptionOccurred(IntPtr env);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void ExceptionClear(IntPtr env);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr GetObjectClass(IntPtr env, IntPtr obj);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr CallObjectMethod(
            IntPtr env, IntPtr obj, IntPtr methodId, params JavaValue[] args);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr CallStaticObjectMethod(
            IntPtr env, IntPtr clazz, IntPtr methodId, params JavaValue[] args);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal unsafe delegate IntPtr GetStringChars(IntPtr env, IntPtr jstring, byte* isCopy);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void ReleaseStringChars(IntPtr env, IntPtr jstring, IntPtr chars);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal unsafe delegate JNIResult RegisterNatives(IntPtr env, IntPtr clazz,
            JNINativeMethod* methods, int nMethods);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [SuppressUnmanagedCodeSecurity]
        internal delegate JNIResult GetEnv(IntPtr jvm, out IntPtr env, int version);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [SuppressUnmanagedCodeSecurity]
        internal delegate JNIResult AttachCurrentThread(IntPtr jvm, out IntPtr env, IntPtr args);
    }
}