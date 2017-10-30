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

namespace Apache.Ignite.Core.Impl.Unmanaged.Jni
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// JNI methods accessor.
    /// </summary>
    internal class JniMethods
    {
        private readonly JNIEnv _env;

        private readonly Delegates.CallStaticVoidMethod _callStaticVoidMethod;
        private readonly Delegates.CallStaticVoidMethodV _callStaticVoidMethodV;
        private readonly Delegates.FindClass _findClass;
        private readonly Delegates.GetMethodID _getMethodId;
        private readonly Delegates.GetStaticMethodID _getStaticMethodId;
        private readonly Delegates.NewStringUTF _newStringUTF;

        public JniMethods(JNIEnv env)
        {
            Debug.Assert(env != null);
            Debug.Assert(env.EnvPtr != IntPtr.Zero);

            _env = env;

            var func = env.Functions;

            _callStaticVoidMethod = GetDelegate<Delegates.CallStaticVoidMethod>(func.CallStaticVoidMethod);
            _callStaticVoidMethodV = GetDelegate<Delegates.CallStaticVoidMethodV>(func.__CallStaticVoidMethodV);
            _findClass = GetDelegate<Delegates.FindClass>(func.FindClass);
            _getMethodId = GetDelegate<Delegates.GetMethodID>(func.GetMethodID);
            _getStaticMethodId = GetDelegate<Delegates.GetStaticMethodID>(func.GetStaticMethodID);
            _newStringUTF = GetDelegate<Delegates.NewStringUTF>(func.NewStringUTF);
        }

        public void CallStaticVoidMethod(IntPtr clazz, IntPtr methodId, params JavaValue[] args)
        {
            _callStaticVoidMethod(_env.EnvPtr, clazz, methodId, args);

            ExceptionCheck();
        }

        public void CallStaticVoidMethodV(IntPtr clazz, IntPtr methodId, IntPtr vaList)
        {
            _callStaticVoidMethodV(_env.EnvPtr, clazz, methodId, vaList);

            ExceptionCheck();
        }

        public IntPtr FindClass(string name)
        {
            var res = _findClass(_env.EnvPtr, name);

            ExceptionCheck();

            return res;
        }

        public IntPtr GetStaticMethodId(IntPtr clazz, string name, string signature)
        {
            var res = _getStaticMethodId(_env.EnvPtr, clazz, name, signature);

            ExceptionCheck();

            return res;
        }

        public IntPtr NewStringUTF(IntPtr utf)  // TODO: result must be released with DeleteLocalRef
        {
            var res = _newStringUTF(_env.EnvPtr, utf);

            ExceptionCheck();

            return res;
        }

        private static void ExceptionCheck()
        {
            // TODO
        }

        private static T GetDelegate<T>(IntPtr ptr)
        {
            return TypeCaster<T>.Cast(Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)));
        }
    }
}
