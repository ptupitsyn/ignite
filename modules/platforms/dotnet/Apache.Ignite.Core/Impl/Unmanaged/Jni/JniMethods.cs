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
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// JNI methods accessor.
    /// </summary>
    internal class JniMethods
    {
        private readonly JNIEnv _env;

        private readonly JniDelegates.CallStaticVoidMethod _callStaticVoidMethod;
        private readonly JniDelegates.FindClass _findClass;
        private readonly JniDelegates.GetMethodID _getMethodId;
        private readonly JniDelegates.GetStaticMethodID _getStaticMethodId;
        private readonly JniDelegates.NewStringUTF _newStringUtf;
        private readonly JniDelegates.ExceptionOccurred _exceptionOccurred;

        public JniMethods(JNIEnv env)
        {
            Debug.Assert(env != null);
            Debug.Assert(env.EnvPtr != IntPtr.Zero);

            _env = env;

            var func = env.Functions;

            _callStaticVoidMethod = GetDelegate<JniDelegates.CallStaticVoidMethod>(func.CallStaticVoidMethod);
            _findClass = GetDelegate<JniDelegates.FindClass>(func.FindClass);
            _getMethodId = GetDelegate<JniDelegates.GetMethodID>(func.GetMethodID);
            _getStaticMethodId = GetDelegate<JniDelegates.GetStaticMethodID>(func.GetStaticMethodID);
            _newStringUtf = GetDelegate<JniDelegates.NewStringUTF>(func.NewStringUTF);
            _exceptionOccurred = GetDelegate<JniDelegates.ExceptionOccurred>(func.ExceptionOccurred);
        }

        public void CallStaticVoidMethod(IntPtr clazz, IntPtr methodId, params JavaValue[] args)
        {
            _callStaticVoidMethod(_env.EnvPtr, clazz, methodId, args);

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
            var res = _newStringUtf(_env.EnvPtr, utf);

            ExceptionCheck();

            return res;
        }

        private void ExceptionCheck()
        {
            var err = _exceptionOccurred(_env.EnvPtr);

            if (err != IntPtr.Zero)
            {
                //jclass cls = env->GetObjectClass(err);

                //jstring clsName = static_cast<jstring>(env->CallObjectMethod(cls, jvm->GetJavaMembers().m_Class_getName));
                //jstring msg = static_cast<jstring>(env->CallObjectMethod(err, jvm->GetJavaMembers().m_Throwable_getMessage));
                //jstring trace = static_cast<jstring>(env->CallStaticObjectMethod(jvm->GetJavaMembers().c_PlatformUtils, jvm->GetJavaMembers().m_PlatformUtils_getFullStackTrace, err));


                // Exception is present.
                throw new Exception("Fuck");
            }
        }

        private static T GetDelegate<T>(IntPtr ptr)
        {
            return TypeCaster<T>.Cast(Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)));
        }
    }
}
