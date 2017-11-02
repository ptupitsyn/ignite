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
        private readonly EnvDelegates.CallStaticObjectMethod _callStaticObjectMethod;

        /** */
        private readonly EnvDelegates.RegisterNatives _registerNatives;

        /** */
        private readonly EnvDelegates.DeleteLocalRef _deleteLocalRef;

        /** */
        private readonly EnvDelegates.NewGlobalRef _newGlobalRef;

        /** */
        private readonly EnvDelegates.DeleteGlobalRef _deleteGlobalRef;

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
            GetDelegate(func.FindClass, out _findClass);
            GetDelegate(func.GetMethodID, out _getMethodId);
            GetDelegate(func.GetStaticMethodID, out _getStaticMethodId);
            GetDelegate(func.NewStringUTF, out _newStringUtf);
            GetDelegate(func.ExceptionOccurred, out _exceptionOccurred);
            GetDelegate(func.ExceptionClear, out _exceptionClear);
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
        }

        /// <summary>
        /// Gets the env pointer.
        /// </summary>
        public IntPtr EnvPtr
        {
            get { return _envPtr; }
        }

        /// <summary>
        /// Gets the JVM.
        /// </summary>
        public Jvm Jvm
        {
            get { return _jvm; }
        }

        public void CallStaticVoidMethod(IUnmanagedTarget cls, IntPtr methodId, params JavaValue[] args)
        {
            _callStaticVoidMethod(_envPtr, cls.Target, methodId, args);

            ExceptionCheck();
        }

        public LocalRef CallObjectMethod(IUnmanagedTarget obj, IntPtr methodId, params JavaValue[] args)
        {
            var res = _callObjectMethod(_envPtr, obj.Target, methodId, args);

            ExceptionCheck();

            return new LocalRef(this, res);
        }

        public long CallLongMethod(IUnmanagedTarget obj, IntPtr methodId, params JavaValue[] args)
        {
            var res = _callLongMethod(_envPtr, obj.Target, methodId, args);

            ExceptionCheck();

            return res;
        }

        public void CallVoidMethod(IUnmanagedTarget obj, IntPtr methodId, params JavaValue[] args)
        {
            _callVoidMethod(_envPtr, obj.Target, methodId, args);

            ExceptionCheck();
        }

        public LocalRef CallStaticObjectMethod(LocalRef cls, IntPtr methodId, params JavaValue[] args)
        {
            var res = _callStaticObjectMethod(_envPtr, cls.Target, methodId, args);

            ExceptionCheck();

            return new LocalRef(this, res);
        }

        public LocalRef FindClass(string name)
        {
            var res = _findClass(_envPtr, name);

            ExceptionCheck();

            return new LocalRef(this, res);
        }

        public LocalRef GetObjectClass(LocalRef obj)
        {
            var res = _getObjectClass(_envPtr, obj.Target);

            ExceptionCheck();

            return new LocalRef(this, res);
        }

        public IntPtr GetStaticMethodId(LocalRef clazz, string name, string signature)
        {
            var res = _getStaticMethodId(_envPtr, clazz.Target, name, signature);

            ExceptionCheck();

            return res;
        }

        public IntPtr GetMethodId(LocalRef clazz, string name, string signature)
        {
            var res = _getMethodId(_envPtr, clazz.Target, name, signature);

            ExceptionCheck();

            return res;
        }

        public LocalRef NewStringUtf(sbyte* utf)
        {
            return NewStringUtf(new IntPtr(utf));
        }

        public LocalRef NewStringUtf(IntPtr utf)
        {
            var res = _newStringUtf(_envPtr, utf);

            ExceptionCheck();

            return new LocalRef(this, res);
        }

        private IntPtr GetStringChars(IntPtr jstring)
        {
            Debug.Assert(jstring != IntPtr.Zero);

            byte isCopy;
            return _getStringChars(_envPtr, jstring, &isCopy);
        }

        private void ReleaseStringChars(IntPtr jstring, IntPtr chars)
        {
            _releaseStringChars(_envPtr, jstring, chars);
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

        public void RegisterNatives(LocalRef clazz, NativeMethod[] methods)
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

        public string JStringToString(LocalRef jstring)
        {
            return JStringToString(jstring.Target);
        }

        public string JStringToString(IntPtr jstring)
        {
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
            return new GlobalRef(_newGlobalRef(_envPtr, lref));
        }

        public void DeleteGlobalRef(IntPtr gref)
        {
            _deleteGlobalRef(_envPtr, gref);
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

                using (var errRef = new LocalRef(this, err))
                using (var platformUtilsCls =
                    FindClass("org/apache/ignite/internal/processors/platform/utils/PlatformUtils"))
                {
                    var getStackTrace = GetStaticMethodId(platformUtilsCls, "getFullStackTrace",
                        "(Ljava/lang/Throwable;)Ljava/lang/String;");

                    using (var cls = GetObjectClass(errRef))
                    using (var clsName = CallObjectMethod(cls, _jvm.MethodId.ClassGetName))
                    using (var msg = CallObjectMethod(errRef, _jvm.MethodId.ThrowableGetMessage))
                    using (var trace = CallStaticObjectMethod(platformUtilsCls, getStackTrace,
                        new JavaValue {_object = err}))
                    {
                        // Exception is present.
                        throw new Exception(string.Format("{0}: {1}\n\n{2}",
                            JStringToString(clsName),
                            JStringToString(msg),
                            JStringToString(trace)));
                    }
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