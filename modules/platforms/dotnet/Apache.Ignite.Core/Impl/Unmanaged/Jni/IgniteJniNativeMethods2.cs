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
    internal static unsafe class IgniteJniNativeMethods2
    {
        // See https://github.com/srisatish/openjdk/blob/master/jdk/src/share/sample/vm/clr-jvm/invoker.cs
        // See https://github.com/jni4net/jni4net

        [DllImport(IgniteUtils.FileJvmDll, EntryPoint = "JNI_CreateJavaVM")]
        public unsafe static extern long JNI_CreateJavaVM(void** ppVm, void** ppEnv, void* pArgs);

        [DllImport(IgniteUtils.FileJvmDll, EntryPoint = "IgniteTargetInLongOutLong")]
        public static extern long TargetInLongOutLong(void* ctx, void* target, int opType, long val);

        [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern JNIResult JNI_CreateJavaVM(out IntPtr pvm, out IntPtr penv,
            JavaVMInitArgs* args);

        [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern JNIResult JNI_GetCreatedJavaVMs(out IntPtr pvm, int size,
            [Out] out int size2);

        [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern JNIResult JNI_GetDefaultJavaVMInitArgs(JavaVMInitArgs* args);



    }

    internal enum JNIResult
    {
        Success = 0,
        Error = -1,
        ThreadDetached = -2,
        VersionError = -3,
        NotEnoughMemory = -4,
        AlreadyExists = -5,
        InvalidArguments = -6
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal unsafe struct JavaVMInitArgs
    {
        public int version;
        public int nOptions;
        public JavaVMOption* options;
        public byte ignoreUnrecognized;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct JavaVMOption
    {
        public IntPtr optionString;
        public IntPtr extraInfo;
    }



}
