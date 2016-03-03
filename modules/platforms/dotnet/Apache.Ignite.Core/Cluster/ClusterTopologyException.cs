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
    /// Indicates an error with grid topology (e.g., crashed node, etc.)
    /// </summary>
    [Serializable]
    public class ClusterTopologyException : IgniteException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterTopologyException"/> class.
        /// </summary>
        public ClusterTopologyException()
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterTopologyException"/> class.
        /// </summary>
        /// <param name="msg">Exception message.</param>
        public ClusterTopologyException(string msg) : base(msg)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterTopologyException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cause">The cause.</param>
        public ClusterTopologyException(string message, Exception cause) : base(message, cause)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterTopologyException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="javaStackTrace">The Java stack trace.</param>
        /// <param name="cause">The cause.</param>
        public ClusterTopologyException(string message, string javaStackTrace, Exception cause) 
            : base(message, cause, javaStackTrace)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterTopologyException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="ctx">Streaming context.</param>
        protected ClusterTopologyException(SerializationInfo info, StreamingContext ctx)
            : base(info, ctx)
        {
            // No-op.
        }
    }
}