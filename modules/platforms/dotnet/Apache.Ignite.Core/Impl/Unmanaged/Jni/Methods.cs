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
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// JNI methods accessor.
    /// </summary>
    internal class Methods
    {
        private readonly JNIEnv _env;

        private readonly Delegates.CallStaticVoidMethod _callStaticVoidMethod;

        public Methods(JNIEnv env)
        {
            Debug.Assert(env != null);
            Debug.Assert(env.EnvPtr != IntPtr.Zero);

            _env = env;

            var func = env.Functions;

            _callStaticVoidMethod = GetDelegate<Delegates.CallStaticVoidMethod>(func.CallStaticVoidMethod);
        }

        public void CallStaticVoidMethod(JNIEnv env, JavaClass clazz, IntPtr methodId, params Value[] args)
        {
            _callStaticVoidMethod(env.EnvPtr, clazz.Handle.DangerousGetHandle(), methodId, args);

            // TODO
            // ExceptionTest();
        }

        private static T GetDelegate<T>(IntPtr ptr)
        {
            return TypeCaster<T>.Cast(Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)));
        }
    }
}
