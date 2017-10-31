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
        private readonly Delegates.GetStringChars _getStringChars;
        private readonly Delegates.ReleaseStringChars _releaseStringChars;
        private readonly Delegates.ExceptionClear _exceptionClear;
        private readonly Delegates.CallStaticObjectMethod _callStaticObjectMethod;

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
            _exceptionClear = GetDelegate<Delegates.ExceptionClear>(func.ExceptionClear);
            _getObjectClass = GetDelegate<Delegates.GetObjectClass>(func.GetObjectClass);
            _callObjectMethod = GetDelegate<Delegates.CallObjectMethod>(func.CallObjectMethod);
            _getStringChars = GetDelegate<Delegates.GetStringChars>(func.GetStringChars);
            _releaseStringChars = GetDelegate<Delegates.ReleaseStringChars>(func.ReleaseStringChars);
            _callStaticObjectMethod = GetDelegate<Delegates.CallStaticObjectMethod>(func.CallStaticObjectMethod);
        }

        public void CallStaticVoidMethod(IntPtr cls, IntPtr methodId, params JavaValue[] args)
        {
            _callStaticVoidMethod(_envPtr, cls, methodId, args);

            ExceptionCheck();
        }

        public IntPtr CallObjectMethod(IntPtr obj, IntPtr methodId, params JavaValue[] args)
        {
            var res = _callObjectMethod(_envPtr, obj, methodId, args);

            ExceptionCheck();

            return res;
        }

        public IntPtr CallStaticObjectMethod(IntPtr cls, IntPtr methodId, params JavaValue[] args)
        {
            var res = _callStaticObjectMethod(_envPtr, cls, methodId, args);

            ExceptionCheck();

            return res;
        }

        public IntPtr FindClass(string name)
        {
            var res = _findClass(_envPtr, name);

            ExceptionCheck();

            return res;
        }

        public IntPtr GetObjectClass(IntPtr err)
        {
            var res = _getObjectClass(_envPtr, err);
            
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

        public unsafe IntPtr GetStringChars(IntPtr jstring)
        {
            Debug.Assert(jstring != IntPtr.Zero);

            byte isCopy;
            return _getStringChars(_envPtr, jstring, &isCopy);
        }

        public void ReleaseStringChars(IntPtr jstring, IntPtr chars)
        {
            _releaseStringChars(_envPtr, jstring, chars);
        }

        private void ExceptionCheck()
        {
            var err = _exceptionOccurred(_envPtr);

            if (err != IntPtr.Zero)
            {
                _exceptionClear(_envPtr);

                var classCls = FindClass("java/lang/Class");
                var classGetName = GetMethodId(classCls, "getName", "()Ljava/lang/String;");

                var throwableCls = FindClass("java/lang/Throwable");
                var throwableGetMessage = GetMethodId(throwableCls, "getMessage", "()Ljava/lang/String;");

                var platformUtilsCls = FindClass("org/apache/ignite/internal/processors/platform/utils/PlatformUtils");
                var getStackTrace = GetMethodId(platformUtilsCls, "getFullStackTrace",
                    "(Ljava/lang/Throwable;)Ljava/lang/String;");

                var cls = GetObjectClass(err);
                var clsName = CallObjectMethod(cls, classGetName);
                var msg = CallObjectMethod(err, throwableGetMessage);
                var trace = CallStaticObjectMethod(platformUtilsCls, getStackTrace, new JavaValue {_object = err});

                // Exception is present.
                throw new Exception(string.Format("{0}: {1}\n\n{2}", JStringToString(clsName), JStringToString(msg),
                    JStringToString(trace)));
            }
        }

        private static T GetDelegate<T>(IntPtr ptr)
        {
            return TypeCaster<T>.Cast(Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)));
        }

        private string JStringToString(IntPtr jstring)
        {
            if (jstring != IntPtr.Zero)
            {
                var chars = GetStringChars(jstring);
                var result = Marshal.PtrToStringUni(chars);
                ReleaseStringChars(jstring, chars);
                return result;
            }

            return null;
        }
    }
}
