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

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct JavaValue
    {
        public JavaValue(IUnmanagedTarget r) : this()
        {
            if (r != null)
            {
                _object = r.Target;
            }
        }

        public JavaValue(int i) : this()
        {
            _int = i;
        }

        public JavaValue(long l) : this()
        {
            _long = l;
        }

        [FieldOffset(0)] public byte _bool;
        [FieldOffset(0)] public byte _byte;
        [FieldOffset(0)] public short _char;
        [FieldOffset(0)] public short _short;
        [FieldOffset(0)] public int _int;
        [FieldOffset(0)] public long _long;
        [FieldOffset(0)] public float _float;
        [FieldOffset(0)] public double _double;
        [FieldOffset(0)] public IntPtr _object;
    }
}