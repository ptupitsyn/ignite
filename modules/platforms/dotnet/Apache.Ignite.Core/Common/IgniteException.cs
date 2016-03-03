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

namespace Apache.Ignite.Core.Common
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// General Ignite exception. Indicates any error condition within Ignite.
    /// </summary>
    [Serializable]
    public class IgniteException : Exception
    {
        /** Java stack trace. */
        private readonly string _javaStackTrace;

        /** Java stack trace field name. */
        private const string JavaStackTraceField = "JavaStackTrace";

        /// <summary>
        /// Initializes a new instance of the <see cref="IgniteException"/> class.
        /// </summary>
        public IgniteException()
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IgniteException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IgniteException(string message) : base(message)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IgniteException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="javaStackTrace">The Java stack trace.</param>
        /// <param name="cause">The cause.</param>
        public IgniteException(string message, string javaStackTrace, Exception cause) : base(message, cause)
        {
            _javaStackTrace = javaStackTrace;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IgniteException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cause">The cause.</param>
        public IgniteException(string message, Exception cause) : base(message, cause)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IgniteException"/> class.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="ctx">Streaming context.</param>
        protected IgniteException(SerializationInfo info, StreamingContext ctx) : base(info, ctx)
        {
            _javaStackTrace = info.GetString(JavaStackTraceField);
        }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="SerializationInfo" /> with information 
        /// about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about 
        /// the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about 
        /// the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(JavaStackTraceField, _javaStackTrace);
        }

        /// <summary>
        /// Gets the Java stack trace, if applicable.
        /// </summary>
        /// <value>
        /// The Java stack trace, or null.
        /// </value>
        public string JavaStackTrace
        {
            get { return _javaStackTrace; }
        }

        /** <inheritdoc /> */
        public override string ToString()
        {
            return string.Format("{0}, \nJavaStackTrace: {1}", base.ToString(), JavaStackTrace);
        }
    }
}
