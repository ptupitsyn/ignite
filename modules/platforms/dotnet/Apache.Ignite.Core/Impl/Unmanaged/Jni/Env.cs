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

    /// <summary>
    /// JNIEnv.
    /// </summary>
    internal unsafe class Env
    {
        /** JNIEnv pointer. */
        private readonly IntPtr _envPtr;

        /** Functions. */
        private readonly EnvInterface _functions;

        /** Methods. */
        private readonly EnvMethods _methods;

        /// <summary>
        /// Initializes a new instance of the <see cref="Env" /> class.
        /// </summary>
        internal Env(IntPtr envPtr)
        {
            _envPtr = envPtr;
            var funcPtr = (EnvInterface**)envPtr;
            _functions = **funcPtr;

            _methods = new EnvMethods(this);
        }

        /// <summary>
        /// Gets the methods.
        /// </summary>
        public EnvMethods Methods
        {
            get { return _methods; }
        }

        /// <summary>
        /// Gets the env pointer.
        /// </summary>
        public IntPtr EnvPtr
        {
            get { return _envPtr; }
        }

        /// <summary>
        /// Gets the functions.
        /// </summary>
        public EnvInterface Functions
        {
            get { return _functions; }
        }
    }
}