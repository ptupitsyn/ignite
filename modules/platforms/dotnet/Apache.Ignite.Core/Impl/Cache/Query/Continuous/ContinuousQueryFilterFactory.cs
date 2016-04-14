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

namespace Apache.Ignite.Core.Impl.Cache.Query.Continuous
{
    using Apache.Ignite.Core.Cache.Event;
    using Apache.Ignite.Core.Common;

    /// <summary>
    /// Continuous query filter interface. Required to hide generic nature of underliyng real filter.
    /// </summary>
    internal interface IContinuousQueryFilterFactory
    {
        /// <summary>
        /// Creates the filter instance.
        /// </summary>
        IContinuousQueryFilter CreateInstance();
    }

    /// <summary>
    /// Continuous query filter generic implementation.
    /// </summary>
    internal class ContinuousQueryFilterFactory<TK, TV> : IContinuousQueryFilterFactory
    {
        /** Actual filter factory. */
        private readonly IFactory<ICacheEntryEventFilter<TK, TV>> _filterFactory;

        /** Keep binary flag. */
        private readonly bool _keepBinary;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filterFactory">Actual filter factory.</param>
        /// <param name="keepBinary">Keep binary flag.</param>
        public ContinuousQueryFilterFactory(IFactory<ICacheEntryEventFilter<TK, TV>> filterFactory, bool keepBinary)
        {
            _filterFactory = filterFactory;
            _keepBinary = keepBinary;
        }

        /** <inheritdoc /> */
        public IContinuousQueryFilter CreateInstance()
        {
            return new ContinuousQueryFilter<TK, TV>(_filterFactory.CreateInstance(), _keepBinary);
        }
    }
}
