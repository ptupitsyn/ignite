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
    internal class Methods
    {
        private readonly JNIEnv _env;
        private readonly IntPtr _envPtr;

        private readonly Delegates.CallStaticVoidMethod _callStaticVoidMethod;
        private readonly Delegates.FindClass _findClass;
        private readonly Delegates.GetMethodID _getMethodId;
        private readonly Delegates.GetStaticMethodID _getStaticMethodId;
        private readonly Delegates.NewStringUTF _newStringUtf;
        private readonly Delegates.ExceptionOccurred _exceptionOccurred;
        private readonly Delegates.GetObjectClass _getObjectClass;
        private readonly Delegates.CallObjectMethod _callObjectMethod;

        public Methods(JNIEnv env)
        {
            Debug.Assert(env != null);
            Debug.Assert(env.EnvPtr != IntPtr.Zero);

            _env = env;
            _envPtr = env.EnvPtr;

            var func = env.Functions;

            _callStaticVoidMethod = GetDelegate<Delegates.CallStaticVoidMethod>(func.CallStaticVoidMethod);
            _findClass = GetDelegate<Delegates.FindClass>(func.FindClass);
            _getMethodId = GetDelegate<Delegates.GetMethodID>(func.GetMethodID);
            _getStaticMethodId = GetDelegate<Delegates.GetStaticMethodID>(func.GetStaticMethodID);
            _newStringUtf = GetDelegate<Delegates.NewStringUTF>(func.NewStringUTF);
            _exceptionOccurred = GetDelegate<Delegates.ExceptionOccurred>(func.ExceptionOccurred);
            _getObjectClass = GetDelegate<Delegates.GetObjectClass>(func.GetObjectClass);
            _callObjectMethod = GetDelegate<Delegates.CallObjectMethod>(func.CallObjectMethod);
        }

        public void CallStaticVoidMethod(IntPtr clazz, IntPtr methodId, params JavaValue[] args)
        {
            _callStaticVoidMethod(_envPtr, clazz, methodId, args);

            ExceptionCheck();
        }

        public IntPtr FindClass(string name)
        {
            var res = _findClass(_envPtr, name);

            ExceptionCheck();

            return res;
        }

        public IntPtr GetStaticMethodId(IntPtr clazz, string name, string signature)
        {
            var res = _getStaticMethodId(_envPtr, clazz, name, signature);

            ExceptionCheck();

            return res;
        }

        public IntPtr GetMethodId(IntPtr clazz, string name, string signature)
        {
            var res = _getMethodId(_envPtr, clazz, name, signature);

            ExceptionCheck();

            return res;
        }

        public IntPtr NewStringUTF(IntPtr utf)  // TODO: result must be released with DeleteLocalRef
        {
            var res = _newStringUtf(_envPtr, utf);

            ExceptionCheck();

            return res;
        }

        private void ExceptionCheck()
        {
            var err = _exceptionOccurred(_envPtr);

            if (err != IntPtr.Zero)
            {
                var cls = _getObjectClass(_envPtr, err);

                var classCls = FindClass("java/lang/Class");
                var classGetName = GetMethodId(classCls, "getName", "()Ljava/lang/String;");
                
                var clsName = _callObjectMethod(_envPtr, cls, classGetName);

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
