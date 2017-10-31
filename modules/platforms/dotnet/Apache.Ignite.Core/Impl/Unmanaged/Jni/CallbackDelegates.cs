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
    using System.Runtime.InteropServices;
    using System.Security;

    [SuppressUnmanagedCodeSecurity]
    internal static class CallbackDelegates
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void LoggerLog(IntPtr env, IntPtr clazz, int level, IntPtr message, IntPtr category,
            IntPtr error, long memPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool LoggerIsLevelEnabled(IntPtr env, IntPtr clazz, int level);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long InLongLongLongObjectOutLong(IntPtr env, IntPtr clazz, 
            long envPtr, int op, long arg1, long arg2, long arg3, IntPtr arg);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void ConsoleWrite(IntPtr env, IntPtr clazz, IntPtr message, bool isError);
    }
}
