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

namespace Apache.Ignite.Core.Impl.Events
{
    using System.Collections.Generic;
    using Apache.Ignite.Core.Events;

    /// <summary>
    /// Local event listener holder, see <see cref="IgniteConfiguration.LocalEventListeners"/>.
    /// </summary>
    public class LocalEventListener
    {
        /// <summary>
        /// Gets or sets the listener.
        /// </summary>
        public IEventListener<IEvent> Listener { get; set; }

        /// <summary>
        /// Gets or sets the event types.
        /// </summary>
        public ICollection<int> EventTypes { get; set; }

        /// <summary>
        /// Gets the original user listener object.
        /// </summary>
        internal virtual object GetUserListener()
        {
            return Listener;
        }
    }

    /// <summary>
    /// Generic local event listener holder, see <see cref="IgniteConfiguration.LocalEventListeners"/>.
    /// </summary>
    public class LocalEventListener<T> : LocalEventListener where T : IEvent
    {
        /// <summary>
        /// Gets or sets the listener.
        /// </summary>
        public new IEventListener<T> Listener
        {
            get { return ((ListenerWrapper<T>) base.Listener).Listener; }
            set { base.Listener = new ListenerWrapper<T>(value); }
        }

        /// <summary>
        /// Gets the user listener object.
        /// </summary>
        internal override object GetUserListener()
        {
            return Listener;
        }

        /// <summary>
        /// Generic listener wrapper.
        /// </summary>
        private class ListenerWrapper<TEvent> : IEventListener<IEvent> where TEvent : IEvent
        {
            /** Listener. */
            private readonly IEventListener<TEvent> _listener;

            /// <summary>
            /// Initializes a new instance of the class.
            /// </summary>
            public ListenerWrapper(IEventListener<TEvent> listener)
            {
                _listener = listener;
            }

            /// <summary>
            /// Gets the listener.
            /// </summary>
            public IEventListener<TEvent> Listener
            {
                get { return _listener; }
            }

            /** <inheritdoc /> */
            public bool Invoke(IEvent evt)
            {
                return _listener.Invoke((TEvent) evt);
            }
        }
    }
}
