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

namespace Apache.Ignite.Core.Impl.Binary
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Threading;

    /// <summary>
    /// Invokes [Serializable] system callbacks, such as
    /// <see cref="IDeserializationCallback" /> and <see cref="OnDeserializedAttribute"/>.
    /// </summary>
    internal static class SerializableCallback
    {
        /// <summary>
        /// Object graph for current thread.
        /// </summary>
        private static readonly ThreadLocal<ObjectGraph> Graph 
            = new ThreadLocal<ObjectGraph>(() => new ObjectGraph());

        /// <summary>
        /// Called when deserialization of an object has started.
        /// </summary>
        /// <param name="obj">The object.</param>
        public static void OnDeserializing(object obj)
        {
            var graph = Graph.Value;

            graph.Objects.Add(obj);
            graph.Depth++;
        }

        /// <summary>
        /// Called when deserialization of an object has completed.
        /// </summary>
        public static void OnDeserialized()
        {
            var graph = Graph.Value;

            graph.Depth--;

            if (graph.Depth == 0)
            {
                // Entire graph has been deserialized: invoke callbacks.
                foreach (var obj in graph.Objects)
                {
                    var cb = obj as IDeserializationCallback;

                    if (cb != null)
                    {
                        cb.OnDeserialization(null);
                    }
                }

                graph.Objects.Clear();
            }
        }

        /// <summary>
        /// Object graph.
        /// </summary>
        private class ObjectGraph
        {
            /** */
            private readonly List<object> _objects = new List<object>();

            /// <summary>
            /// Gets or sets the depth.
            /// </summary>
            public int Depth { get; set; }

            /// <summary>
            /// Gets the objects.
            /// </summary>
            public List<object> Objects
            {
                get { return _objects; }
            }
        }
    }
}
