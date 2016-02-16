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

namespace Apache.Ignite.Core.Impl.Cache
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Impl.Collections;
    using Apache.Ignite.Core.Impl.Unmanaged;

    /// <summary>
    /// Real cache enumerator communicating with Java.
    /// </summary>
    internal class CacheEnumerator<TK, TV> : IgniteEnumerator<ICacheEntry<TK, TV>>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="target">Target.</param>
        /// <param name="marsh">Marshaller.</param>
        /// <param name="keepBinary">Keep binary flag.</param>
        public CacheEnumerator(IUnmanagedTarget target, Marshaller marsh, bool keepBinary) : 
            base(target, marsh, keepBinary)
        {
            // No-op.
        }

        /** <inheritdoc /> */
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
        protected override ICacheEntry<TK, TV> ReadItem(BinaryReader reader)
        {
            Debug.Assert(reader != null);

            var key = reader.DetachNext().ReadObject<TK>();
            var val = reader.DetachNext().ReadObject<TV>();

            return new CacheEntry<TK, TV>(key, val);
        }
    }
}
