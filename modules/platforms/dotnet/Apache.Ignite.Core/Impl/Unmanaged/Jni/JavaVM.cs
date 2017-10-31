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

    /// <summary>
    /// JNI defines "JavaVM" and "JNIEnv" structures.
    /// They are pointers to function tables.
    /// 
    /// JavaVM allows to get/create/destroy JVM.
    /// API implies that multiple JVMs can exist in one process, but only one is allowed.
    ///
    /// JNIEnv provides all other functions.
    /// JNIEnv is used for thread-local storage.
    /// JNIEnv CAN NOT be shared between threads.
    /// JavaVM should be passed around, use GetEnv to get JNIEnv for a thread.
    /// Not every thread has JNIEnv, AttachCurrentThread should be called to ensure this.
    /// 
    /// TODO: Threads attached through JNI must call DetachCurrentThread before they exit.
    /// However, DLL_THREAD_DETACH does not work currently. Need to investigate.
    /// Always detach if just attached?
    /// </summary>
    internal unsafe class JavaVM
    {
        private readonly IntPtr native;
        private JNIInvokeInterface functions;

        public JavaVM(IntPtr native)
        {
            this.native = native;
            var x = (JNIInvokeInterface**) native;
            functions = **x;
        }
    }
}