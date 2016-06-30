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

namespace Apache.Ignite.Core.Cache.Affinity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache.Affinity.Fair;
    using Apache.Ignite.Core.Cache.Affinity.Rendezvous;
    using Apache.Ignite.Core.Cluster;
    using Apache.Ignite.Core.Common;

    /// <summary>
    /// Base class for predefined affinity functions.
    /// </summary>
    [Serializable]
    public abstract class AffinityFunctionBase : IAffinityFunction
    {
        /** */
        private const byte TypeCodeNull = 0;

        /** */
        private const byte TypeCodeFair = 1;

        /** */
        private const byte TypeCodeRendezvous = 2;

        /** */
        private const byte TypeCodeUser = 3;

        /// <summary> The default value for <see cref="Partitions"/> property. </summary>
        public const int DefaultPartitions = 1024;

        /** */
        private int _partitions = DefaultPartitions;

        /** */
        private IAffinityFunction _baseFunction;


        /// <summary>
        /// Gets or sets the total number of partitions.
        /// </summary>
        [DefaultValue(DefaultPartitions)]
        public virtual int Partitions
        {
            get { return _partitions; }
            set { _partitions = value; }
        }

        /// <summary>
        /// Gets partition number for a given key starting from 0. Partitioned caches
        /// should make sure that keys are about evenly distributed across all partitions
        /// from 0 to <see cref="Partitions" /> for best performance.
        /// <para />
        /// Note that for fully replicated caches it is possible to segment key sets among different
        /// grid node groups. In that case each node group should return a unique partition
        /// number. However, unlike partitioned cache, mappings of keys to nodes in
        /// replicated caches are constant and a node cannot migrate from one partition
        /// to another.
        /// </summary>
        /// <param name="key">Key to get partition for.</param>
        /// <returns>
        /// Partition number for a given key.
        /// </returns>
        public virtual int GetPartition(object key)
        {
            ThrowIfUninitialized();

            return _baseFunction.GetPartition(key);
        }

        /// <summary>
        /// Removes node from affinity. This method is called when it is safe to remove
        /// disconnected node from affinity mapping.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        public virtual void RemoveNode(Guid nodeId)
        {
            ThrowIfUninitialized();

            _baseFunction.RemoveNode(nodeId);
        }

        /// <summary>
        /// Gets affinity nodes for a partition. In case of replicated cache, all returned
        /// nodes are updated in the same manner. In case of partitioned cache, the returned
        /// list should contain only the primary and back up nodes with primary node being
        /// always first.
        /// <pare />
        /// Note that partitioned affinity must obey the following contract: given that node
        /// <code>N</code> is primary for some key <code>K</code>, if any other node(s) leave
        /// grid and no node joins grid, node <code>N</code> will remain primary for key <code>K</code>.
        /// </summary>
        /// <param name="context">The affinity function context.</param>
        /// <returns>
        /// A collection of partitions, where each partition is a collection of nodes,
        /// where first node is a primary node, and other nodes are backup nodes.
        /// </returns>
        public virtual IEnumerable<IEnumerable<IClusterNode>> AssignPartitions(AffinityFunctionContext context)
        {
            ThrowIfUninitialized();

            return _baseFunction.AssignPartitions(context);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to exclude same-host-neighbors from being backups of each other.
        /// </summary>
        public virtual bool ExcludeNeighbors { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AffinityFunctionBase"/> class.
        /// </summary>
        internal AffinityFunctionBase()
        {
            // No-op.
        }

        /// <summary>
        /// Reads the instance.
        /// </summary>
        internal static IAffinityFunction Read(IBinaryRawReader reader)
        {
            // TODO
            AffinityFunctionBase fun;

            var typeCode = reader.ReadByte();
            switch (typeCode)
            {
                case TypeCodeNull:
                    return null;
                case TypeCodeFair:
                    fun = new FairAffinityFunction();
                    break;
                case TypeCodeRendezvous:
                    fun = new RendezvousAffinityFunction();
                    break;
                case TypeCodeUser:
                    var f = reader.ReadObject<IAffinityFunction>();
                    reader.ReadInt(); // skip partition count

                    return f;
                default:
                    throw new InvalidOperationException("Invalid AffinityFunction type code: " + typeCode);
            }

            fun.Partitions = reader.ReadInt();
            fun.ExcludeNeighbors = reader.ReadBoolean();

            return fun;
        }

        /// <summary>
        /// Writes the instance.
        /// </summary>
        internal static void Write(IBinaryRawWriter writer, IAffinityFunction fun)
        {
            if (fun == null)
            {
                writer.WriteByte(TypeCodeNull);
                return;
            }

            // 1) Type code
            // 2) Partitions
            // 3) ExcludeNeighbors
            // 4) Override flags
            // 5) User object

            var p = fun as AffinityFunctionBase;

            if (p != null)
            {
                writer.WriteByte(p is FairAffinityFunction ? TypeCodeFair : TypeCodeRendezvous);
                writer.WriteInt(p.Partitions);
                writer.WriteBoolean(p.ExcludeNeighbors);
                writer.WriteByte((byte) GetOverrideFlags(p.GetType())); // Override flags
                // TODO: User func only if there are override flags
                WriteUserFunc(writer, fun); // User func
            }
            else
            {
                writer.WriteByte(TypeCodeUser);
                writer.WriteInt(fun.Partitions); // partition count is written once and can not be changed.
                writer.WriteBoolean(false); // Exclude neighbors
                writer.WriteByte((byte) UserOverrides.None); // Override flags
                WriteUserFunc(writer, fun); // User func
            }
        }

        internal unsafe void SetBaseFunction(IAffinityFunction baseFunc)
        {
            _baseFunction = baseFunc;
        }

        /// <summary>
        /// Gets the override flags.
        /// </summary>
        private static UserOverrides GetOverrideFlags(Type funcType)
        {
            // TODO
            return UserOverrides.None;
        }

        /// <summary>
        /// Writes the user function.
        /// </summary>
        private static void WriteUserFunc(IBinaryRawWriter writer, IAffinityFunction fun)
        {
            if (!fun.GetType().IsSerializable)
                throw new IgniteException("AffinityFunction should be serializable.");

            writer.WriteObject(fun);
        }

        /// <summary>
        /// Gets the direct usage error.
        /// </summary>
        private void ThrowIfUninitialized()
        {
            if (_baseFunction == null)
                throw new IgniteException(GetType() + " has not yet been initialized.");
        }

        [Flags]
        private enum UserOverrides : byte
        {
            None = 0,
            GetPartition = 1,
            RemoveNode = 1 << 1,
            AssignPartitions = 1 << 2
        }
    }
}
