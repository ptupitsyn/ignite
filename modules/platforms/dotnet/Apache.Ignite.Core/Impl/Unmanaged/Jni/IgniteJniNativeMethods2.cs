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
    using System.Runtime.InteropServices;
    using System.Security;
    using Apache.Ignite.Core.Common;

    [SuppressUnmanagedCodeSecurity]
    internal static unsafe class IgniteJniNativeMethods2
    {
        // See https://github.com/srisatish/openjdk/blob/master/jdk/src/share/sample/vm/clr-jvm/invoker.cs
        // See https://github.com/jni4net/jni4net

        [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern JNIResult JNI_CreateJavaVM(out IntPtr pvm, out IntPtr penv,
            JavaVMInitArgs* args);

        [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern JNIResult JNI_GetCreatedJavaVMs(out IntPtr pvm, int size,
            [Out] out int size2);

        [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern JNIResult JNI_GetDefaultJavaVMInitArgs(JavaVMInitArgs* args);

        public static void CreateJavaVM(out JavaVM jvm, out JNIEnv env, params string[] options)
        {
            IntPtr njvm;
            IntPtr nenv;
            var args = new JavaVMInitArgs();

            if (options.Length > 0)
            {
                args.nOptions = options.Length;
                var opt = new JavaVMOption[options.Length];
                for (int i = 0; i < options.Length; i++)
                {
                    opt[i].optionString = Marshal.StringToHGlobalAnsi(options[i]);
                }
                fixed (JavaVMOption* a = &opt[0])
                {
                    args.options = a;
                }
            }
            var result = JNI_CreateJavaVM(out njvm, out nenv, &args);
            if (result != JNIResult.Success)
            {
                Console.Error.WriteLine("Can't load JVM (already have one ?)");
                throw new IgniteException("Can't load JVM (already have one ?) " + result);
            }
            jvm = new JavaVM(njvm);
            env = new JNIEnv(nenv);
        }



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

    [StructLayout(LayoutKind.Sequential)]
    internal struct JNIInvokeInterface
    {
        public IntPtr reserved0;
        public IntPtr reserved1;
        public IntPtr reserved2;

        public IntPtr DestroyJavaVM;
        public IntPtr AttachCurrentThread;
        public IntPtr DetachCurrentThread;
        public IntPtr GetEnv;
        public IntPtr AttachCurrentThreadAsDaemon;
    }



    internal unsafe class JavaVM
    {
        private readonly IntPtr native;
        private JNIInvokeInterface functions;

        public JavaVM(IntPtr native)
        {
            this.native = native;
            functions = *(*(JavaPtr*) native.ToPointer()).functions;
        }

        [StructLayout(LayoutKind.Sequential, Size = 4)]
        internal struct JavaPtr
        {
            public JNIInvokeInterface* functions;
        }


    }

    internal unsafe class JNIEnv
    {
        //private static JavaVM defaultVM;
        //[ThreadStatic] private static JNIEnv threadJNIEnv;

        private readonly IntPtr envPtr;
        private JNINativeInterface functions;
        //private JavaVM javaVM;

        internal JNIEnv(IntPtr native)
        {
            this.envPtr = native;
            functions = *(*(JavaPtr*) native.ToPointer()).functions;
            // TODO
            // InitMethods();
            //if (defaultVM == null)
            //{
            //    defaultVM = GetJavaVM();
            //}
        }

        [StructLayout(LayoutKind.Sequential, Size = 4)]
        internal struct JavaPtr
        {
            public JNINativeInterface* functions;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JNINativeInterface
    {
        public IntPtr reserved0;
        public IntPtr reserved1;
        public IntPtr reserved2;
        public IntPtr reserved3;
        public IntPtr GetVersion;
        public IntPtr DefineClass;
        public IntPtr FindClass;
        public IntPtr FromReflectedMethod;
        public IntPtr FromReflectedField;
        public IntPtr ToReflectedMethod;
        public IntPtr GetSuperclass;
        public IntPtr IsAssignableFrom;
        public IntPtr ToReflectedField;
        public IntPtr Throw;
        public IntPtr ThrowNew;
        public IntPtr ExceptionOccurred;
        public IntPtr ExceptionDescribe;
        public IntPtr ExceptionClear;
        public IntPtr FatalError;
        public IntPtr PushLocalFrame;
        public IntPtr PopLocalFrame;
        public IntPtr NewGlobalRef;
        public IntPtr DeleteGlobalRef;
        public IntPtr DeleteLocalRef;
        public IntPtr IsSameObject;
        public IntPtr NewLocalRef;
        public IntPtr EnsureLocalCapacity;
        public IntPtr AllocObject;
        public IntPtr __NewObject;
        public IntPtr __NewObjectV;
        public IntPtr NewObject;
        public IntPtr GetObjectClass;
        public IntPtr IsInstanceOf;
        public IntPtr GetMethodID;
        public IntPtr __CallObjectMethod;
        public IntPtr __CallObjectMethodV;
        public IntPtr CallObjectMethod;
        public IntPtr __CallBooleanMethod;
        public IntPtr __CallBooleanMethodV;
        public IntPtr CallBooleanMethod;
        public IntPtr __CallByteMethod;
        public IntPtr __CallByteMethodV;
        public IntPtr CallByteMethod;
        public IntPtr __CallCharMethod;
        public IntPtr __CallCharMethodV;
        public IntPtr CallCharMethod;
        public IntPtr __CallShortMethod;
        public IntPtr __CallShortMethodV;
        public IntPtr CallShortMethod;
        public IntPtr __CallIntMethod;
        public IntPtr __CallIntMethodV;
        public IntPtr CallIntMethod;
        public IntPtr __CallLongMethod;
        public IntPtr __CallLongMethodV;
        public IntPtr CallLongMethod;
        public IntPtr __CallFloatMethod;
        public IntPtr __CallFloatMethodV;
        public IntPtr CallFloatMethod;
        public IntPtr __CallDoubleMethod;
        public IntPtr __CallDoubleMethodV;
        public IntPtr CallDoubleMethod;
        public IntPtr __CallVoidMethod;
        public IntPtr __CallVoidMethodV;
        public IntPtr CallVoidMethod;
        public IntPtr __CallNonvirtualObjectMethod;
        public IntPtr __CallNonvirtualObjectMethodV;
        public IntPtr CallNonvirtualObjectMethod;
        public IntPtr __CallNonvirtualBooleanMethod;
        public IntPtr __CallNonvirtualBooleanMethodV;
        public IntPtr CallNonvirtualBooleanMethod;
        public IntPtr __CallNonvirtualByteMethod;
        public IntPtr __CallNonvirtualByteMethodV;
        public IntPtr CallNonvirtualByteMethod;
        public IntPtr __CallNonvirtualCharMethod;
        public IntPtr __CallNonvirtualCharMethodV;
        public IntPtr CallNonvirtualCharMethod;
        public IntPtr __CallNonvirtualShortMethod;
        public IntPtr __CallNonvirtualShortMethodV;
        public IntPtr CallNonvirtualShortMethod;
        public IntPtr __CallNonvirtualIntMethod;
        public IntPtr __CallNonvirtualIntMethodV;
        public IntPtr CallNonvirtualIntMethod;
        public IntPtr __CallNonvirtualLongMethod;
        public IntPtr __CallNonvirtualLongMethodV;
        public IntPtr CallNonvirtualLongMethod;
        public IntPtr __CallNonvirtualFloatMethod;
        public IntPtr __CallNonvirtualFloatMethodV;
        public IntPtr CallNonvirtualFloatMethod;
        public IntPtr __CallNonvirtualDoubleMethod;
        public IntPtr __CallNonvirtualDoubleMethodV;
        public IntPtr CallNonvirtualDoubleMethod;
        public IntPtr __CallNonvirtualVoidMethod;
        public IntPtr __CallNonvirtualVoidMethodV;
        public IntPtr CallNonvirtualVoidMethod;
        public IntPtr GetFieldID;
        public IntPtr GetObjectField;
        public IntPtr GetBooleanField;
        public IntPtr GetByteField;
        public IntPtr GetCharField;
        public IntPtr GetShortField;
        public IntPtr GetIntField;
        public IntPtr GetLongField;
        public IntPtr GetFloatField;
        public IntPtr GetDoubleField;
        public IntPtr SetObjectField;
        public IntPtr SetBooleanField;
        public IntPtr SetByteField;
        public IntPtr SetCharField;
        public IntPtr SetShortField;
        public IntPtr SetIntField;
        public IntPtr SetLongField;
        public IntPtr SetFloatField;
        public IntPtr SetDoubleField;
        public IntPtr GetStaticMethodID;
        public IntPtr __CallStaticObjectMethod;
        public IntPtr __CallStaticObjectMethodV;
        public IntPtr CallStaticObjectMethod;
        public IntPtr __CallStaticBooleanMethod;
        public IntPtr __CallStaticBooleanMethodV;
        public IntPtr CallStaticBooleanMethod;
        public IntPtr __CallStaticByteMethod;
        public IntPtr __CallStaticByteMethodV;
        public IntPtr CallStaticByteMethod;
        public IntPtr __CallStaticCharMethod;
        public IntPtr __CallStaticCharMethodV;
        public IntPtr CallStaticCharMethod;
        public IntPtr __CallStaticShortMethod;
        public IntPtr __CallStaticShortMethodV;
        public IntPtr CallStaticShortMethod;
        public IntPtr __CallStaticIntMethod;
        public IntPtr __CallStaticIntMethodV;
        public IntPtr CallStaticIntMethod;
        public IntPtr __CallStaticLongMethod;
        public IntPtr __CallStaticLongMethodV;
        public IntPtr CallStaticLongMethod;
        public IntPtr __CallStaticFloatMethod;
        public IntPtr __CallStaticFloatMethodV;
        public IntPtr CallStaticFloatMethod;
        public IntPtr __CallStaticDoubleMethod;
        public IntPtr __CallStaticDoubleMethodV;
        public IntPtr CallStaticDoubleMethod;
        public IntPtr __CallStaticVoidMethod;
        public IntPtr __CallStaticVoidMethodV;
        public IntPtr CallStaticVoidMethod;
        public IntPtr GetStaticFieldID;
        public IntPtr GetStaticObjectField;
        public IntPtr GetStaticBooleanField;
        public IntPtr GetStaticByteField;
        public IntPtr GetStaticCharField;
        public IntPtr GetStaticShortField;
        public IntPtr GetStaticIntField;
        public IntPtr GetStaticLongField;
        public IntPtr GetStaticFloatField;
        public IntPtr GetStaticDoubleField;
        public IntPtr SetStaticObjectField;
        public IntPtr SetStaticBooleanField;
        public IntPtr SetStaticByteField;
        public IntPtr SetStaticCharField;
        public IntPtr SetStaticShortField;
        public IntPtr SetStaticIntField;
        public IntPtr SetStaticLongField;
        public IntPtr SetStaticFloatField;
        public IntPtr SetStaticDoubleField;
        public IntPtr NewString;
        public IntPtr GetStringLength;
        public IntPtr GetStringChars;
        public IntPtr ReleaseStringChars;
        public IntPtr NewStringUTF;
        public IntPtr GetStringUTFLength;
        public IntPtr GetStringUTFChars;
        public IntPtr ReleaseStringUTFChars;
        public IntPtr GetArrayLength;
        public IntPtr NewObjectArray;
        public IntPtr GetObjectArrayElement;
        public IntPtr SetObjectArrayElement;
        public IntPtr NewBooleanArray;
        public IntPtr NewByteArray;
        public IntPtr NewCharArray;
        public IntPtr NewShortArray;
        public IntPtr NewIntArray;
        public IntPtr NewLongArray;
        public IntPtr NewFloatArray;
        public IntPtr NewDoubleArray;
        public IntPtr GetBooleanArrayElements;
        public IntPtr GetByteArrayElements;
        public IntPtr GetCharArrayElements;
        public IntPtr GetShortArrayElements;
        public IntPtr GetIntArrayElements;
        public IntPtr GetLongArrayElements;
        public IntPtr GetFloatArrayElements;
        public IntPtr GetDoubleArrayElements;
        public IntPtr ReleaseBooleanArrayElements;
        public IntPtr ReleaseByteArrayElements;
        public IntPtr ReleaseCharArrayElements;
        public IntPtr ReleaseShortArrayElements;
        public IntPtr ReleaseIntArrayElements;
        public IntPtr ReleaseLongArrayElements;
        public IntPtr ReleaseFloatArrayElements;
        public IntPtr ReleaseDoubleArrayElements;
        public IntPtr GetBooleanArrayRegion;
        public IntPtr GetByteArrayRegion;
        public IntPtr GetCharArrayRegion;
        public IntPtr GetShortArrayRegion;
        public IntPtr GetIntArrayRegion;
        public IntPtr GetLongArrayRegion;
        public IntPtr GetFloatArrayRegion;
        public IntPtr GetDoubleArrayRegion;
        public IntPtr SetBooleanArrayRegion;
        public IntPtr SetByteArrayRegion;
        public IntPtr SetCharArrayRegion;
        public IntPtr SetShortArrayRegion;
        public IntPtr SetIntArrayRegion;
        public IntPtr SetLongArrayRegion;
        public IntPtr SetFloatArrayRegion;
        public IntPtr SetDoubleArrayRegion;
        public IntPtr RegisterNatives;
        public IntPtr UnregisterNatives;
        public IntPtr MonitorEnter;
        public IntPtr MonitorExit;
        public IntPtr GetJavaVM;
        public IntPtr GetStringRegion;
        public IntPtr GetStringUTFRegion;
        public IntPtr GetPrimitiveArrayCritical;
        public IntPtr ReleasePrimitiveArrayCritical;
        public IntPtr GetStringCritical;
        public IntPtr ReleaseStringCritical;
        public IntPtr NewWeakGlobalRef;
        public IntPtr DeleteWeakGlobalRef;
        public IntPtr ExceptionCheck;
        public IntPtr NewDirectByteBuffer;
        public IntPtr GetDirectBufferAddress;
        public IntPtr GetDirectBufferCapacity;
    }




}
