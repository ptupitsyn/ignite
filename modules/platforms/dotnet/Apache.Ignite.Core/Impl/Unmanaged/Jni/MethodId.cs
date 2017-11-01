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

    /// <summary>
    /// JNI method ids.
    /// </summary>
    internal class MethodId
    {
        /// <summary>
        /// Class.getName().
        /// </summary>
        public IntPtr ClassGetName { get; private set; }

        /// <summary>
        /// Throwable.getMessage().
        /// </summary>
        public IntPtr ThrowableGetMessage { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodId"/> class.
        /// </summary>
        public MethodId(Env env)
        {
            Debug.Assert(env != null);

            // TODO: Classes should use GlobalRef (if used for a long time), method ids don't
            var classCls = env.FindClass("java/lang/Class");
            ClassGetName = env.GetMethodId(classCls, "getName", "()Ljava/lang/String;");

            var throwableCls = env.FindClass("java/lang/Throwable");
            ThrowableGetMessage = env.GetMethodId(throwableCls, "getMessage", "()Ljava/lang/String;");

            


            // TODO: DeleteLocalRef, IDisposable
        }

    }
}
