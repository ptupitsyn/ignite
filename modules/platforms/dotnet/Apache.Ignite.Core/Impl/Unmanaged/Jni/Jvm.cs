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
    using Apache.Ignite.Core.Common;

    /// <summary>
    /// JVM holder.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal class Jvm
    {
        /** */
        private const int JNI_VERSION_1_6 = 0x00010006;

        /** */
        private readonly JvmMethods _methods;

        /// <summary>
        /// Initializes a new instance of the <see cref="Jvm"/> class.
        /// </summary>
        private Jvm(IntPtr jvmPtr)
        {
            _methods = new JvmMethods(jvmPtr);
        }

        /// <summary>
        /// Gets or creates the JVM.
        /// </summary>
        /// <param name="options">JVM options.</param>
        public static unsafe Jvm GetOrCreate(params string[] options)
        {
            var args = new JvmInitArgs
            {
                version = JNI_VERSION_1_6
            };

            if (options.Length > 0)
            {
                args.nOptions = options.Length;
                var opt = new JvmOption[options.Length];

                for (int i = 0; i < options.Length; i++)
                {
                    opt[i].optionString = Marshal.StringToHGlobalAnsi(options[i]);
                }

                fixed (JvmOption* a = &opt[0])
                {
                    args.options = a;
                }
            }

            IntPtr jvm;
            IntPtr env;

            // TODO: Get if exists.
            var result = JniNativeMethods.JNI_CreateJavaVM(out jvm, out env, &args);
            if (result != JNIResult.Success)
            {
                throw new IgniteException("Can't load JVM: " + result);
            }

            return new Jvm(jvm);
        }

        /// <summary>
        /// Attaches current thread to the JVM and returns JNIEnv.
        /// </summary>
        public Env AttachCurrentThread()
        {
            // TODO: Cache in a ThreadLocal.
            return new Env(_methods.AttachCurrentThread());
        }

        /// <summary>
        /// JavaVMOption.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct JvmOption
        {
            public IntPtr optionString;
            public IntPtr extraInfo;
        }

        /// <summary>
        /// JavaVMInitArgs.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private unsafe struct JvmInitArgs
        {
            public int version;
            public int nOptions;
            public JvmOption* options;
            public byte ignoreUnrecognized;
        }

        /// <summary>
        /// DLL imports.
        /// </summary>
        private static unsafe class JniNativeMethods
        {
            // See https://github.com/srisatish/openjdk/blob/master/jdk/src/share/sample/vm/clr-jvm/invoker.cs
            // See https://github.com/jni4net/jni4net

            [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
            internal static extern JNIResult JNI_CreateJavaVM(out IntPtr pvm, out IntPtr penv,
                JvmInitArgs* args);

            [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
            internal static extern JNIResult JNI_GetCreatedJavaVMs(out IntPtr pvm, int size,
                [Out] out int size2);

            [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
            internal static extern JNIResult JNI_GetDefaultJavaVMInitArgs(JvmInitArgs* args);
        }
    }
}
