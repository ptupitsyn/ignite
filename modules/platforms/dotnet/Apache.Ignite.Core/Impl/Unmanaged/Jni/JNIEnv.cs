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

        public IntPtr EnvPtr
        {
            get { return envPtr; }
        }

        public JNINativeInterface Functions
        {
            get { return functions; }
        }

        [StructLayout(LayoutKind.Sequential, Size = 4)]
        internal struct JavaPtr
        {
            public JNINativeInterface* functions;
        }
    }
}