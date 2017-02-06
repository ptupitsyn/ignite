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
    /// Tracks object graph and invokes <see cref="IDeserializationCallback" />.
    /// </summary>
    internal static class DeserializationCallbackProcessor
    {
        /// <summary>
        /// Object graph for current thread.
        /// </summary>
        private static readonly ThreadLocal<ObjectGraph> Graph 
            = new ThreadLocal<ObjectGraph>(() => new ObjectGraph());

        /// <summary>
        /// Register an object for deserialization callback.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>Id of the object.</returns>
        public static int Push(object obj)
        {
            var graph = Graph.Value;

            graph.Objects.Add(new KeyValuePair<object, object>(obj, null));

            graph.Depth++;

            return graph.Objects.Count - 1;
        }

        /// <summary>
        /// Sets the reference.
        /// </summary>
        /// <param name="objId">The object identifier.</param>
        /// <param name="referenceObj">The reference object.</param>
        public static void SetReference(int objId, object referenceObj)
        {
            var graph = Graph.Value;

            var obj = graph.Objects[objId].Key;
            graph.Objects[objId] = new KeyValuePair<object, object>(obj, referenceObj);
        }

        /// <summary>
        /// Called when deserialization of an object has completed.
        /// When Pop() has been called for all registered objects, all callbacks are invoked.
        /// </summary>
        public static void Pop()
        {
            var graph = Graph.Value;

            graph.Depth--;

            if (graph.Depth == 0)
            {
                // Entire graph has been deserialized: invoke callbacks in direct order (like BinaryFormatter does).
                foreach (var obj in graph.Objects)
                {
                    if (obj.Value != null)
                    {
                        if (InvokeOnDeserialization(obj.Value))
                        {
                            ReflectionUtils.CopyFields(obj.Value, obj.Key);
                        }
                    }
                    else
                    {
                        InvokeOnDeserialization(obj.Key);
                    }
                }

                graph.Objects.Clear();
            }
        }

        /// <summary>
        /// Invokes the OnDeserialization callback.
        /// </summary>
        /// <param name="obj">The object.</param>
        private static bool InvokeOnDeserialization(object obj)
        {
            var cb = obj as IDeserializationCallback;

            if (cb != null)
            {
                cb.OnDeserialization(null);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Object graph.
        /// </summary>
        private class ObjectGraph
        {
            /** */
            private readonly List<KeyValuePair<object, object>> _objects = new List<KeyValuePair<object, object>>();

            /// <summary>
            /// Gets or sets the depth.
            /// </summary>
            public int Depth { get; set; }

            /// <summary>
            /// Gets the objects.
            /// </summary>
            public List<KeyValuePair<object, object>> Objects
            {
                get { return _objects; }
            }
        }
    }
}
