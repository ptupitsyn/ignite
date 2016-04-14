﻿/*
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

namespace Apache.Ignite.Core.Impl.Cache.Event
{
    using System;
    using System.Collections.Generic;
    using Apache.Ignite.Core.Cache.Event;
    using Apache.Ignite.Core.Common;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Cache entry event filter that delegates to Java.
    /// </summary>
    /// <typeparam name="TK">Key type.</typeparam>
    /// <typeparam name="TV">Value type.</typeparam>
    internal class JavaCacheEntryEventFilterFactory<TK, TV> : PlatformJavaObjectFactoryProxy, 
        IFactory<ICacheEntryEventFilter<TK, TV>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JavaCacheEntryEventFilterFactory{TK, TV}"/> class.
        /// </summary>
        /// <param name="factoryClassName">Name of the factory class.</param>
        /// <param name="properties">The properties.</param>
        public JavaCacheEntryEventFilterFactory(string factoryClassName, IDictionary<string, object> properties) 
            : base(FactoryType.User, factoryClassName, null, properties)
        {
            // No-op.
        }

        /** <inheritdoc /> */
        public ICacheEntryEventFilter<TK, TV> CreateInstance()
        {
            throw new InvalidOperationException(GetType() + " cannot be invoked directly.");
        }
    }
}
