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

namespace Apache.Ignite.Core.Cluster 
{
    using System;
    using System.Runtime.Serialization;
    using Apache.Ignite.Core.Common;

    /// <summary>
    /// Indicates an illegal call on empty projection. Thrown by projection when operation
    /// that requires at least one node is called on empty projection.
    /// </summary>
    [Serializable]
    public class ClusterGroupEmptyException : IgniteException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterGroupEmptyException"/> class.
        /// </summary>
        public ClusterGroupEmptyException()
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterGroupEmptyException"/> class.
        /// </summary>
        /// <param name="msg">Exception message.</param>
        public ClusterGroupEmptyException(string msg) : base(msg)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterGroupEmptyException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="javaStackTrace">The Java stack trace.</param>
        public ClusterGroupEmptyException(string message, string javaStackTrace) : base(message, javaStackTrace)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterGroupEmptyException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cause">The cause.</param>
        public ClusterGroupEmptyException(string message, Exception cause)
            : base(message, cause)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterGroupEmptyException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="ctx">Streaming context.</param>
        protected ClusterGroupEmptyException(SerializationInfo info, StreamingContext ctx)
            : base(info, ctx)
        {
            // No-op.
        }
    }
}
