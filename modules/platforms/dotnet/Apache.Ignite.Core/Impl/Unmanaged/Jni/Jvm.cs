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
    using Apache.Ignite.Core.Common;

    /// <summary>
    /// JVM holder.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal unsafe class Jvm
    {
        /** */
        // ReSharper disable once InconsistentNaming
        private const int JNI_VERSION_1_6 = 0x00010006;

        /** */
        private readonly IntPtr _jvmPtr;

        /** */
        private readonly JvmDelegates.AttachCurrentThread _attachCurrentThread;

        /** Static instamce */
        private static Jvm _instance;

        /** Sync. */
        private static readonly object SyncRoot = new object();

        /** Env for current thread. */
        [ThreadStatic] private static Env _env;

        /// <summary>
        /// Initializes a new instance of the <see cref="_instance"/> class.
        /// </summary>
        private Jvm(IntPtr jvmPtr)
        {
            Debug.Assert(jvmPtr != IntPtr.Zero);

            _jvmPtr = jvmPtr;

            var funcPtr = (JvmInterface**)jvmPtr;
            var func = **funcPtr;

            GetDelegate(func.AttachCurrentThread, out _attachCurrentThread);
        }

        /// <summary>
        /// Gets or creates the JVM.
        /// </summary>
        /// <param name="options">JVM options.</param>
        public static Jvm GetOrCreate(params string[] options)
        {
            if (_instance != null)
            {
                return _instance;
            }

            lock (SyncRoot)
            {
                return _instance ?? (_instance = new Jvm(GetJvmPtr(options)));
            }
        }

        /// <summary>
        /// Gets the JVM.
        /// </summary>
        public static Jvm Get()
        {
            if (_instance == null)
            {
                throw new IgniteException("JVM has not been created.");
            }

            return _instance;
        }

        /// <summary>
        /// Gets the JVM pointer.
        /// </summary>
        private static IntPtr GetJvmPtr(string[] options)
        {
            IntPtr jvm;
            int existingJvmCount;

            // Use existing JVM if present.
            var res = JniNativeMethods.JNI_GetCreatedJavaVMs(out jvm, 1, out existingJvmCount);
            if (res != JniResult.Success)
            {
                throw new IgniteException("JNI_GetCreatedJavaVMs failed: " + res);
            }

            if (existingJvmCount > 0)
            {
                return jvm;
            }

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

            IntPtr env;
            res = JniNativeMethods.JNI_CreateJavaVM(out jvm, out env, &args);
            if (res != JniResult.Success)
            {
                throw new IgniteException("JNI_CreateJavaVM failed: " + res);
            }

            return jvm;
        }

        /// <summary>
        /// Attaches current thread to the JVM and returns JNIEnv.
        /// </summary>
        public Env AttachCurrentThread()
        {
            if (_env == null)
            {
                IntPtr envPtr;
                var res = _attachCurrentThread(_jvmPtr, out envPtr, IntPtr.Zero);

                if (res != JniResult.Success)
                {
                    throw new IgniteException("AttachCurrentThread failed: " + res);
                }

                _env = new Env(envPtr);
            }

            return _env;
        }

        /// <summary>
        /// Gets the delegate.
        /// </summary>
        private static void GetDelegate<T>(IntPtr ptr, out T del)
        {
            del = (T) (object) Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        }

        /// <summary>
        /// JavaVMOption.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct JvmOption
        {
            public IntPtr optionString;
            private readonly IntPtr extraInfo;
        }

        /// <summary>
        /// JavaVMInitArgs.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct JvmInitArgs
        {
            public int version;
            public int nOptions;
            public JvmOption* options;
            private readonly byte ignoreUnrecognized;
        }

        /// <summary>
        /// DLL imports.
        /// </summary>
        private static class JniNativeMethods
        {
            [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
            internal static extern JniResult JNI_CreateJavaVM(out IntPtr pvm, out IntPtr penv,
                JvmInitArgs* args);

            [DllImport("jvm.dll", CallingConvention = CallingConvention.StdCall)]
            internal static extern JniResult JNI_GetCreatedJavaVMs(out IntPtr pvm, int size,
                [Out] out int size2);
        }
    }
}
