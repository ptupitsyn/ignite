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
    using System.Security;
    using Apache.Ignite.Core.Common;

    /// <summary>
    /// JNIEnv.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal unsafe class Env
    {
        /** JNIEnv pointer. */
        private readonly IntPtr _envPtr;

        /** JVM. */
        private readonly Jvm _jvm;

        /** */
        private readonly EnvDelegates.CallStaticVoidMethod _callStaticVoidMethod;

        /** */
        private readonly EnvDelegates.CallStaticBooleanMethod _callStaticBoolMethod;

        /** */
        private readonly EnvDelegates.FindClass _findClass;

        /** */
        private readonly EnvDelegates.GetMethodId _getMethodId;

        /** */
        private readonly EnvDelegates.GetStaticMethodId _getStaticMethodId;

        /** */
        private readonly EnvDelegates.NewStringUtf _newStringUtf;

        /** */
        private readonly EnvDelegates.ExceptionOccurred _exceptionOccurred;

        /** */
        private readonly EnvDelegates.GetObjectClass _getObjectClass;

        /** */
        private readonly EnvDelegates.CallObjectMethod _callObjectMethod;

        /** */
        private readonly EnvDelegates.CallLongMethod _callLongMethod;

        /** */
        private readonly EnvDelegates.CallVoidMethod _callVoidMethod;

        /** */
        private readonly EnvDelegates.GetStringChars _getStringChars;

        /** */
        private readonly EnvDelegates.GetStringUtfChars _getStringUtfChars;

        /** */
        private readonly EnvDelegates.GetStringUtfLength _getStringUtfLength;

        /** */
        private readonly EnvDelegates.ReleaseStringUtfChars _releaseStringUtfChars;

        /** */
        private readonly EnvDelegates.ReleaseStringChars _releaseStringChars;

        /** */
        private readonly EnvDelegates.ExceptionClear _exceptionClear;

        /** */
        private readonly EnvDelegates.ExceptionCheck _exceptionCheck;

        /** */
        private readonly EnvDelegates.CallStaticObjectMethod _callStaticObjectMethod;

        /** */
        private readonly EnvDelegates.RegisterNatives _registerNatives;

        /** */
        private readonly EnvDelegates.DeleteLocalRef _deleteLocalRef;

        /** */
        private readonly EnvDelegates.NewGlobalRef _newGlobalRef;

        /** */
        private readonly EnvDelegates.DeleteGlobalRef _deleteGlobalRef;

        /** */
        private readonly EnvDelegates.ThrowNew _throwNew;

        /// <summary>
        /// Initializes a new instance of the <see cref="Env" /> class.
        /// </summary>
        internal Env(IntPtr envPtr, Jvm jvm)
        {
            Debug.Assert(envPtr != IntPtr.Zero);
            Debug.Assert(jvm != null);

            _envPtr = envPtr;
            _jvm = jvm;

            var funcPtr = (EnvInterface**)envPtr;
            var func = **funcPtr;

            GetDelegate(func.CallStaticVoidMethod, out _callStaticVoidMethod);
            GetDelegate(func.CallStaticBooleanMethod, out _callStaticBoolMethod);
            GetDelegate(func.FindClass, out _findClass);
            GetDelegate(func.GetMethodID, out _getMethodId);
            GetDelegate(func.GetStaticMethodID, out _getStaticMethodId);
            GetDelegate(func.NewStringUTF, out _newStringUtf);
            GetDelegate(func.ExceptionOccurred, out _exceptionOccurred);
            GetDelegate(func.ExceptionClear, out _exceptionClear);
            GetDelegate(func.ExceptionCheck, out _exceptionCheck);
            GetDelegate(func.GetObjectClass, out _getObjectClass);
            GetDelegate(func.CallObjectMethod, out _callObjectMethod);
            GetDelegate(func.CallStaticObjectMethod, out _callStaticObjectMethod);
            GetDelegate(func.CallLongMethod, out _callLongMethod);
            GetDelegate(func.CallVoidMethod, out _callVoidMethod);

            GetDelegate(func.GetStringChars, out _getStringChars);
            GetDelegate(func.ReleaseStringChars, out _releaseStringChars);

            GetDelegate(func.GetStringUTFChars, out _getStringUtfChars);
            GetDelegate(func.ReleaseStringUTFChars, out _releaseStringUtfChars);

            GetDelegate(func.GetStringUTFLength, out _getStringUtfLength);

            GetDelegate(func.RegisterNatives, out _registerNatives);
            GetDelegate(func.DeleteLocalRef, out _deleteLocalRef);
            GetDelegate(func.NewGlobalRef, out _newGlobalRef);
            GetDelegate(func.DeleteGlobalRef, out _deleteGlobalRef);
            GetDelegate(func.ThrowNew, out _throwNew);
        }

        /// <summary>
        /// Gets the JVM.
        /// </summary>
        public Jvm Jvm
        {
            get { return _jvm; }
        }

        public void CallStaticVoidMethod(IUnmanagedTarget cls, IntPtr methodId, long* argsPtr = null)
        {
            _callStaticVoidMethod(_envPtr, cls.Target, methodId, argsPtr);

            ExceptionCheck();
        }

        public bool CallStaticBoolMethod(IUnmanagedTarget cls, IntPtr methodId, long* argsPtr = null)
        {
            var res = _callStaticBoolMethod(_envPtr, cls.Target, methodId, argsPtr);

            ExceptionCheck();

            return res > 0;
        }

        public GlobalRef CallObjectMethod(IUnmanagedTarget obj, IntPtr methodId, long* argsPtr = null)
        {
            // TODO: Change all methods to this approach.
            // LocalRef should be immediately converted to global and then deleted.
            var lref = _callObjectMethod(_envPtr, obj.Target, methodId, argsPtr);

            ExceptionCheck();

            var gref = _newGlobalRef(_envPtr, lref);

            _deleteLocalRef(_envPtr, lref);

            return new GlobalRef(gref, _jvm);
        }

        public long CallLongMethod(IUnmanagedTarget obj, IntPtr methodId, long* argsPtr = null)
        {
            var res = _callLongMethod(_envPtr, obj.Target, methodId, argsPtr);

            ExceptionCheck();

            return res;
        }

        public void CallVoidMethod(IUnmanagedTarget obj, IntPtr methodId, long* argsPtr = null)
        {
            _callVoidMethod(_envPtr, obj.Target, methodId, argsPtr);

            ExceptionCheck();
        }

        public GlobalRef CallStaticObjectMethod(GlobalRef cls, IntPtr methodId, long* argsPtr = null)
        {
            var res = _callStaticObjectMethod(_envPtr, cls.Target, methodId, argsPtr);

            ExceptionCheck();

            return NewGlobalRef(res);
        }

        public GlobalRef FindClass(string name)
        {
            var res = _findClass(_envPtr, name);

            ExceptionCheck();

            return NewGlobalRef(res);
        }

        public GlobalRef GetObjectClass(IUnmanagedTarget obj)
        {
            var res = _getObjectClass(_envPtr, obj.Target);

            ExceptionCheck();

            return NewGlobalRef(res);
        }

        public IntPtr GetStaticMethodId(IUnmanagedTarget clazz, string name, string signature)
        {
            var res = _getStaticMethodId(_envPtr, clazz.Target, name, signature);

            ExceptionCheck();

            return res;
        }

        public IntPtr GetMethodId(IUnmanagedTarget clazz, string name, string signature)
        {
            var res = _getMethodId(_envPtr, clazz.Target, name, signature);

            ExceptionCheck();

            return res;
        }

        public GlobalRef NewStringUtf(sbyte* utf)
        {
            if (utf == null)
            {
                return null;
            }

            var res = _newStringUtf(_envPtr, new IntPtr(utf));

            ExceptionCheck();

            return NewGlobalRef(res);
        }

        public GlobalRef NewStringUtf(string str)
        {
            if (str == null)
            {
                return null;
            }

            var chars = IgniteUtils.StringToUtf8Unmanaged(str);

            try
            {
                return NewStringUtf(chars);
            }
            finally 
            {
                Marshal.FreeHGlobal(new IntPtr(chars));
            }
        }

        private IntPtr GetStringUtfChars(IntPtr jstring)
        {
            Debug.Assert(jstring != IntPtr.Zero);

            byte isCopy;
            return _getStringUtfChars(_envPtr, jstring, &isCopy);
        }

        private void ReleaseStringUtfChars(IntPtr jstring, IntPtr chars)
        {
            _releaseStringUtfChars(_envPtr, jstring, chars);
        }

        private int GetStringUtfLength(IntPtr jstring)
        {
            Debug.Assert(jstring != IntPtr.Zero);

            return _getStringUtfLength(_envPtr, jstring);
        }

        public void RegisterNatives(IUnmanagedTarget clazz, NativeMethod[] methods)
        {
            Debug.Assert(methods != null);

            fixed (NativeMethod* m = &methods[0])
            {
                var res = _registerNatives(_envPtr, clazz.Target, m, methods.Length);

                if (res != JniResult.Success)
                {
                    throw new Exception("Failed to register natives: " + res);
                }
            }
        }

        public string JStringToString(IUnmanagedTarget jstring)
        {
            return JStringToString(jstring.Target);
        }

        public string JStringToString(IntPtr jstring)
        {
            if (jstring == IntPtr.Zero)
            {
                return null;
            }

            var chars = GetStringUtfChars(jstring);
            var len = GetStringUtfLength(jstring);

            try
            {
                return IgniteUtils.Utf8UnmanagedToString((sbyte*) chars, len);
            }
            finally 
            {
                ReleaseStringUtfChars(jstring, chars);
            }
        }

        public void DeleteLocalRef(IntPtr lref)
        {
            _deleteLocalRef(_envPtr, lref);
        }

        public GlobalRef NewGlobalRef(IntPtr lref)
        {
            return new GlobalRef(_newGlobalRef(_envPtr, lref), _jvm);
        }

        public void DeleteGlobalRef(IntPtr gref)
        {
            _deleteGlobalRef(_envPtr, gref);
        }

        public void ThrowToJava(string message)
        {
            Debug.Assert(message != null);
            var msgChars = IgniteUtils.StringToUtf8Unmanaged(message);

            try
            {
                using (var cls = FindClass("org/apache/ignite/IgniteException"))
                {
                    _throwNew(_envPtr, cls.Target, new IntPtr(msgChars));
                }
            }
            finally 
            {
                Marshal.FreeHGlobal(new IntPtr(msgChars));
            }
        }

        /// <summary>
        /// Checks for the JNI exception and throws.
        /// </summary>
        private void ExceptionCheck()
        {
            if (!_exceptionCheck(_envPtr))
            {
                return;
            }

            var err = _exceptionOccurred(_envPtr);
            Debug.Assert(err != IntPtr.Zero);

            _exceptionClear(_envPtr);

            using (var errRef = NewGlobalRef(err))
            {
                var errRef0 = (long) errRef.Target;
                var methodId = _jvm.MethodId;

                using (var cls = GetObjectClass(errRef))
                using (var clsName = CallObjectMethod(cls, methodId.ClassGetName))
                using (var msg = CallObjectMethod(errRef, methodId.ThrowableGetMessage))
                using (var trace = CallStaticObjectMethod(methodId.PlatformUtils,
                    methodId.PlatformUtilsGetStackTrace, &errRef0))
                {
                    throw new JavaException(
                        JStringToString(clsName),
                        JStringToString(msg),
                        JStringToString(trace));
                }
            }
        }

        /// <summary>
        /// Gets the delegate.
        /// </summary>
        private static void GetDelegate<T>(IntPtr ptr, out T del)
        {
            del = (T) (object) Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        }
    }
}