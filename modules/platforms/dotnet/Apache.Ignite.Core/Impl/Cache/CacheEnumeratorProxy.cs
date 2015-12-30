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
    using System.Collections.Generic;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Impl.Collections;


    /// <summary>
    /// Cache enumerator proxy. Required to support reset and early native iterator cleanup.
    /// </summary>
    internal class CacheEnumeratorProxy<TK, TV> : EnumeratorProxyBase<ICacheEntry<TK, TV>>
    {
        /** Target cache. */
        private readonly CacheImpl<TK, TV> _cache;

        /** Local flag. */
        private readonly bool _loc;

        /** Peek modes. */
        private readonly int _peekModes;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cache">Target cache.</param>
        /// <param name="loc">Local flag.</param>
        /// <param name="peekModes">Peek modes.</param>
        public CacheEnumeratorProxy(CacheImpl<TK, TV> cache, bool loc, int peekModes)
        {
            _cache = cache;
            _loc = loc;
            _peekModes = peekModes;

            CreateTarget();
        }


        /** <inheritdoc /> */
        protected override IEnumerator<ICacheEntry<TK, TV>> GetEnumerator()
        {
            return _cache.CreateEnumerator(_loc, _peekModes);
        }
    }
}
