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

namespace Apache.Ignite.Core.Impl.Messaging
{
    using System;
    using System.Runtime.Serialization;
    using Apache.Ignite.Core.Impl.Common;
    using Apache.Ignite.Core.Messaging;

    /// <summary>
    /// MessageFilter from a delegate.
    /// </summary>
    [Serializable]
    internal class MessageDelegateFilter<T> : SerializableWrapper<Func<Guid, T, bool>>, IMessageListener<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDelegateFilter{T}"/> class.
        /// </summary>
        /// <param name="obj">The object to wrap.</param>
        public MessageDelegateFilter(Func<Guid, T, bool> obj)
            : base(obj)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDelegateFilter{T}"/> class.
        /// </summary>
        public MessageDelegateFilter(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // No-op.
        }

        /** <inheritdoc /> */
        public bool Invoke(Guid nodeId, T message)
        {
            return WrappedObject(nodeId, message);
        }
    }
}