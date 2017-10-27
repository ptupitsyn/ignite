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
    using Apache.Ignite.Core.Common;

    /// <summary>
    /// JVM holder.
    /// </summary>
    internal class Jvm
    {
        /** */
        private const int JNI_VERSION_1_6 = 0x00010006;

        /** */
        private readonly JNIEnv _env;

        /** */
        private readonly JavaVM _vm;

        /** */
        private readonly Methods _methods;

        private Jvm(JNIEnv env, JavaVM vm)
        {
            _env = env;
            _vm = vm;

            _methods = new Methods(env);
        }

        public Methods Methods
        {
            get { return _methods; }
        }

        public static unsafe Jvm GetOrCreate(params string[] options)
        {
            var args = new JavaVMInitArgs
            {
                version = JNI_VERSION_1_6
            };

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

            IntPtr njvm;
            IntPtr nenv;

            var result = IgniteJniNativeMethods2.JNI_CreateJavaVM(out njvm, out nenv, &args);
            if (result != JNIResult.Success)
            {
                throw new IgniteException("Can't load JVM: " + result);
            }
            
            var jvm = new JavaVM(njvm);
            var env = new JNIEnv(nenv);

            return new Jvm(env, jvm);
        }
    }
}
