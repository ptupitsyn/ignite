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
    using System.Security;

    /// <summary>
    /// JNIEnv.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal unsafe class Env
    {
        /** JNIEnv pointer. */
        private readonly IntPtr _envPtr;

        /** */
        private readonly EnvDelegates.CallStaticVoidMethod _callStaticVoidMethod;

        /** */
        private readonly EnvDelegates.FindClass _findClass;

        /** */
        private readonly EnvDelegates.GetMethodID _getMethodId;

        /** */
        private readonly EnvDelegates.GetStaticMethodID _getStaticMethodId;

        /** */
        private readonly EnvDelegates.NewStringUTF _newStringUtf;

        /** */
        private readonly EnvDelegates.ExceptionOccurred _exceptionOccurred;

        /** */
        private readonly EnvDelegates.GetObjectClass _getObjectClass;

        /** */
        private readonly EnvDelegates.CallObjectMethod _callObjectMethod;

        /** */
        private readonly EnvDelegates.GetStringChars _getStringChars;

        /** */
        private readonly EnvDelegates.ReleaseStringChars _releaseStringChars;

        /** */
        private readonly EnvDelegates.ExceptionClear _exceptionClear;

        /** */
        private readonly EnvDelegates.CallStaticObjectMethod _callStaticObjectMethod;

        /** */
        private readonly EnvDelegates.RegisterNatives _registerNatives;

        /// <summary>
        /// Initializes a new instance of the <see cref="Env" /> class.
        /// </summary>
        internal Env(IntPtr envPtr)
        {
            _envPtr = envPtr;

            var funcPtr = (EnvInterface**)envPtr;
            var func = **funcPtr;

            GetDelegate(func.CallStaticVoidMethod, out _callStaticVoidMethod);
            GetDelegate(func.FindClass, out _findClass);
            GetDelegate(func.GetMethodID, out _getMethodId);
            GetDelegate(func.GetStaticMethodID, out _getStaticMethodId);
            GetDelegate(func.NewStringUTF, out _newStringUtf);
            GetDelegate(func.ExceptionOccurred, out _exceptionOccurred);
            GetDelegate(func.ExceptionClear, out _exceptionClear);
            GetDelegate(func.GetObjectClass, out _getObjectClass);
            GetDelegate(func.CallObjectMethod, out _callObjectMethod);
            GetDelegate(func.GetStringChars, out _getStringChars);
            GetDelegate(func.ReleaseStringChars, out _releaseStringChars);
            GetDelegate(func.CallStaticObjectMethod, out _callStaticObjectMethod);
            GetDelegate(func.RegisterNatives, out _registerNatives);
        }

        /// <summary>
        /// Gets the env pointer.
        /// </summary>
        public IntPtr EnvPtr
        {
            get { return _envPtr; }
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

        public IntPtr NewStringUtf(IntPtr utf)  // TODO: result must be released with DeleteLocalRef
        {
            var res = _newStringUtf(_envPtr, utf);

            ExceptionCheck();

            return res;
        }

        public IntPtr GetStringChars(IntPtr jstring)
        {
            Debug.Assert(jstring != IntPtr.Zero);

            byte isCopy;
            return _getStringChars(_envPtr, jstring, &isCopy);
        }

        public void ReleaseStringChars(IntPtr jstring, IntPtr chars)
        {
            _releaseStringChars(_envPtr, jstring, chars);
        }

        public void RegisterNatives(IntPtr clazz, NativeMethod[] methods)
        {
            Debug.Assert(methods != null);

            fixed (NativeMethod* m = &methods[0])
            {
                var res = _registerNatives(_envPtr, clazz, m, methods.Length);

                if (res != JniResult.Success)
                {
                    throw new Exception("Failed to register natives.");
                }
            }
        }

        public string JStringToString(IntPtr jstring)
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

        /// <summary>
        /// Checks for the JNI exception and throws.
        /// </summary>
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
                var getStackTrace = GetStaticMethodId(platformUtilsCls, "getFullStackTrace",
                    "(Ljava/lang/Throwable;)Ljava/lang/String;");

                var cls = GetObjectClass(err);
                var clsName = CallObjectMethod(cls, classGetName);
                var msg = CallObjectMethod(err, throwableGetMessage);
                var trace = CallStaticObjectMethod(platformUtilsCls, getStackTrace, new JavaValue { _object = err });

                // Exception is present.
                throw new Exception(string.Format("{0}: {1}\n\n{2}", JStringToString(clsName), JStringToString(msg),
                    JStringToString(trace)));
            }
        }

        /// <summary>
        /// Gets the delegate.
        /// </summary>
        private static void GetDelegate<T>(IntPtr ptr, out T del)
        {
            del = (T)(object)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        }
    }
}