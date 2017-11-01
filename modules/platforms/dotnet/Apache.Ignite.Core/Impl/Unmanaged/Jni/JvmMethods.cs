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
    using Apache.Ignite.Core.Common;

    /// <summary>
    /// JNIJvm methods.
    /// </summary>
    internal class JvmMethods
    {
        /** */
        private readonly JvmDelegates.AttachCurrentThread _attachCurrentThread;

        /** */
        private readonly IntPtr _jvmPtr;

        /// <summary>
        /// Initializes a new instance of the <see cref="JvmMethods"/> class.
        /// </summary>
        public unsafe JvmMethods(IntPtr jvmPtr)
        {
            Debug.Assert(jvmPtr != IntPtr.Zero);
            _jvmPtr = jvmPtr;

            var funcPtr = (JvmInterface**)jvmPtr;
            var func = **funcPtr;

            GetDelegate(func.AttachCurrentThread, out _attachCurrentThread);
        }

        /// <summary>
        /// Attaches the current thread.
        /// </summary>
        public IntPtr AttachCurrentThread()
        {
            IntPtr envPtr;
            var res = _attachCurrentThread(_jvmPtr, out envPtr, IntPtr.Zero);

            if (res != JNIResult.Success)
            {
                throw new IgniteException("Failed to attach to JVM.");
            }

            Debug.Assert(envPtr != IntPtr.Zero);

            return envPtr;
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
