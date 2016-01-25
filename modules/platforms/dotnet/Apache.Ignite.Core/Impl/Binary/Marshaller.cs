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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary.IO;
    using Apache.Ignite.Core.Impl.Binary.Metadata;
    using Apache.Ignite.Core.Impl.Cache;
    using Apache.Ignite.Core.Impl.Cache.Query.Continuous;
    using Apache.Ignite.Core.Impl.Compute;
    using Apache.Ignite.Core.Impl.Compute.Closure;
    using Apache.Ignite.Core.Impl.Datastream;
    using Apache.Ignite.Core.Impl.Messaging;

    /// <summary>
    /// Marshaller implementation.
    /// </summary>
    internal class Marshaller
    {
        /** Binary configuration. */
        private readonly BinaryConfiguration _cfg;

        /** Type to descriptor map. */
        private readonly IDictionary<Type, IBinaryTypeDescriptor> _typeToDesc =
            new Dictionary<Type, IBinaryTypeDescriptor>();

        /** Type name to descriptor map. */
        private readonly IDictionary<string, IBinaryTypeDescriptor> _typeNameToDesc =
            new Dictionary<string, IBinaryTypeDescriptor>();

        /** ID to descriptor map. */
        private readonly IDictionary<long, IBinaryTypeDescriptor> _idToDesc =
            new Dictionary<long, IBinaryTypeDescriptor>();

        /** Cached metadatas. */
        private volatile IDictionary<int, BinaryTypeHolder> _metas =
            new Dictionary<int, BinaryTypeHolder>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cfg">Configurtaion.</param>
        public Marshaller(BinaryConfiguration cfg)
        {
            // Validation.
            if (cfg == null)
                cfg = new BinaryConfiguration();

            if (cfg.TypeConfigurations == null)
                cfg.TypeConfigurations = new List<BinaryTypeConfiguration>();

            foreach (BinaryTypeConfiguration typeCfg in cfg.TypeConfigurations)
            {
                if (string.IsNullOrEmpty(typeCfg.TypeName))
                    throw new BinaryObjectException("Type name cannot be null or empty: " + typeCfg);
            }

            // Define system types. They use internal reflective stuff, so configuration doesn't affect them.
            AddSystemTypes();

            // 2. Define user types.
            var dfltSerializer = cfg.DefaultSerializer == null ? new BinaryReflectiveSerializer() : null;

            var typeResolver = new TypeResolver();

            ICollection<BinaryTypeConfiguration> typeCfgs = cfg.TypeConfigurations;

            if (typeCfgs != null)
                foreach (BinaryTypeConfiguration typeCfg in typeCfgs)
                    AddUserType(cfg, typeCfg, typeResolver, dfltSerializer);

            var typeNames = cfg.TypeNames;

            if (typeNames != null)
                foreach (string typeName in typeNames)
                    AddUserType(cfg, new BinaryTypeConfiguration(typeName), typeResolver, dfltSerializer);

            var types = cfg.Types;

            if (types != null)
                foreach (var type in cfg.Types)
                    AddUserType(cfg, new BinaryTypeConfiguration(type), typeResolver, dfltSerializer);

            if (cfg.DefaultSerializer == null)
                cfg.DefaultSerializer = dfltSerializer;

            _cfg = cfg;
        }

        /// <summary>
        /// Gets or sets the backing grid.
        /// </summary>
        public Ignite Ignite { get; set; }

        /// <summary>
        /// Gets the binary configuration.
        /// </summary>
        public BinaryConfiguration BinaryConfiguration
        {
            get { return _cfg; }
        }

        /// <summary>
        /// Marshal object.
        /// </summary>
        /// <param name="val">Value.</param>
        /// <returns>Serialized data as byte array.</returns>
        public byte[] Marshal<T>(T val)
        {
            BinaryHeapStream stream = new BinaryHeapStream(128);

            Marshal(val, stream);

            return stream.GetArrayCopy();
        }

        /// <summary>
        /// Marshal object.
        /// </summary>
        /// <param name="val">Value.</param>
        /// <param name="stream">Output stream.</param>
        /// <returns>Collection of metadatas (if any).</returns>
        private void Marshal<T>(T val, IBinaryStream stream)
        {
            BinaryWriter writer = StartMarshal(stream);

            writer.Write(val);

            FinishMarshal(writer);
        }

        /// <summary>
        /// Start marshal session.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <returns>Writer.</returns>
        public BinaryWriter StartMarshal(IBinaryStream stream)
        {
            return new BinaryWriter(this, stream);
        }

        /// <summary>
        /// Finish marshal session.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <returns>Dictionary with metadata.</returns>
        public void FinishMarshal(BinaryWriter writer)
        {
            var metas = writer.GetBinaryTypes();

            var ignite = Ignite;

            if (ignite != null && metas != null && metas.Count > 0)
                ignite.PutBinaryTypes(metas);
        }

        /// <summary>
        /// Unmarshal object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">Data array.</param>
        /// <param name="keepBinary">Whether to keep binarizable as binary.</param>
        /// <returns>
        /// Object.
        /// </returns>
        public T Unmarshal<T>(byte[] data, bool keepBinary)
        {
            return Unmarshal<T>(new BinaryHeapStream(data), keepBinary);
        }

        /// <summary>
        /// Unmarshal object.
        /// </summary>
        /// <param name="data">Data array.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>
        /// Object.
        /// </returns>
        public T Unmarshal<T>(byte[] data, BinaryMode mode = BinaryMode.Deserialize)
        {
            return Unmarshal<T>(new BinaryHeapStream(data), mode);
        }

        /// <summary>
        /// Unmarshal object.
        /// </summary>
        /// <param name="stream">Stream over underlying byte array with correct position.</param>
        /// <param name="keepBinary">Whether to keep binary objects in binary form.</param>
        /// <returns>
        /// Object.
        /// </returns>
        public T Unmarshal<T>(IBinaryStream stream, bool keepBinary)
        {
            return Unmarshal<T>(stream, keepBinary ? BinaryMode.KeepBinary : BinaryMode.Deserialize, null);
        }

        /// <summary>
        /// Unmarshal object.
        /// </summary>
        /// <param name="stream">Stream over underlying byte array with correct position.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>
        /// Object.
        /// </returns>
        public T Unmarshal<T>(IBinaryStream stream, BinaryMode mode = BinaryMode.Deserialize)
        {
            return Unmarshal<T>(stream, mode, null);
        }

        /// <summary>
        /// Unmarshal object.
        /// </summary>
        /// <param name="stream">Stream over underlying byte array with correct position.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="builder">Builder.</param>
        /// <returns>
        /// Object.
        /// </returns>
        public T Unmarshal<T>(IBinaryStream stream, BinaryMode mode, BinaryObjectBuilder builder)
        {
            return new BinaryReader(this, _idToDesc, stream, mode, builder).Deserialize<T>();
        }

        /// <summary>
        /// Start unmarshal session.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="keepBinary">Whether to keep binarizable as binary.</param>
        /// <returns>
        /// Reader.
        /// </returns>
        public BinaryReader StartUnmarshal(IBinaryStream stream, bool keepBinary)
        {
            return new BinaryReader(this, _idToDesc, stream,
                keepBinary ? BinaryMode.KeepBinary : BinaryMode.Deserialize, null);
        }

        /// <summary>
        /// Start unmarshal session.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>Reader.</returns>
        public BinaryReader StartUnmarshal(IBinaryStream stream, BinaryMode mode = BinaryMode.Deserialize)
        {
            return new BinaryReader(this, _idToDesc, stream, mode, null);
        }
        
        /// <summary>
        /// Gets metadata for the given type ID.
        /// </summary>
        /// <param name="typeId">Type ID.</param>
        /// <returns>Metadata or null.</returns>
        public IBinaryType GetBinaryType(int typeId)
        {
            if (Ignite != null)
            {
                IBinaryType meta = Ignite.GetBinaryType(typeId);

                if (meta != null)
                    return meta;
            }

            return BinaryType.Empty;
        }

        /// <summary>
        /// Puts the binary type metadata to Ignite.
        /// </summary>
        /// <param name="desc">Descriptor.</param>
        /// <param name="fields">Fields.</param>
        public void PutBinaryType(IBinaryTypeDescriptor desc, IDictionary<string, int> fields = null)
        {
            Debug.Assert(desc != null);

            GetBinaryTypeHandler(desc);  // ensure that handler exists

            if (Ignite != null)
                Ignite.PutBinaryTypes(new[] {new BinaryType(desc, fields)});
        }

        /// <summary>
        /// Gets binary type handler for the given type ID.
        /// </summary>
        /// <param name="desc">Type descriptor.</param>
        /// <returns>Binary type handler.</returns>
        public IBinaryTypeHandler GetBinaryTypeHandler(IBinaryTypeDescriptor desc)
        {
            BinaryTypeHolder holder;

            if (!_metas.TryGetValue(desc.TypeId, out holder))
            {
                lock (this)
                {
                    if (!_metas.TryGetValue(desc.TypeId, out holder))
                    {
                        IDictionary<int, BinaryTypeHolder> metas0 =
                            new Dictionary<int, BinaryTypeHolder>(_metas);

                        holder = new BinaryTypeHolder(desc.TypeId, desc.TypeName, desc.AffinityKeyFieldName, desc.IsEnum);

                        metas0[desc.TypeId] = holder;

                        _metas = metas0;
                    }
                }
            }

            if (holder != null)
            {
                ICollection<int> ids = holder.GetFieldIds();

                bool newType = ids.Count == 0 && !holder.Saved();

                return new BinaryTypeHashsetHandler(ids, newType);
            }

            return null;
        }

        /// <summary>
        /// Callback invoked when metadata has been sent to the server and acknowledged by it.
        /// </summary>
        /// <param name="newMetas">Binary types.</param>
        public void OnBinaryTypesSent(IEnumerable<BinaryType> newMetas)
        {
            foreach (var meta in newMetas)
            {
                var mergeInfo = new Dictionary<int, Tuple<string, int>>(meta.GetFieldsMap().Count);

                foreach (KeyValuePair<string, int> fieldMeta in meta.GetFieldsMap())
                {
                    int fieldId = BinaryUtils.FieldId(meta.TypeId, fieldMeta.Key, null, null);

                    mergeInfo[fieldId] = new Tuple<string, int>(fieldMeta.Key, fieldMeta.Value);
                }

                _metas[meta.TypeId].Merge(mergeInfo);
            }
        }
        
        /// <summary>
        /// Gets descriptor for type.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>Descriptor.</returns>
        public IBinaryTypeDescriptor GetDescriptor(Type type)
        {
            IBinaryTypeDescriptor desc;

            _typeToDesc.TryGetValue(type, out desc);

            return desc;
        }

        /// <summary>
        /// Gets descriptor for type name.
        /// </summary>
        /// <param name="typeName">Type name.</param>
        /// <returns>Descriptor.</returns>
        public IBinaryTypeDescriptor GetDescriptor(string typeName)
        {
            IBinaryTypeDescriptor desc;

            return _typeNameToDesc.TryGetValue(typeName, out desc) ? desc : 
                new BinarySurrogateTypeDescriptor(_cfg, typeName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userType"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public IBinaryTypeDescriptor GetDescriptor(bool userType, int typeId)
        {
            IBinaryTypeDescriptor desc;

            return _idToDesc.TryGetValue(BinaryUtils.TypeKey(userType, typeId), out desc) ? desc :
                userType ? new BinarySurrogateTypeDescriptor(_cfg, typeId) : null;
        }

        /// <summary>
        /// Add user type.
        /// </summary>
        /// <param name="cfg">Configuration.</param>
        /// <param name="typeCfg">Type configuration.</param>
        /// <param name="typeResolver">The type resolver.</param>
        /// <param name="dfltSerializer">The default serializer.</param>
        private void AddUserType(BinaryConfiguration cfg, BinaryTypeConfiguration typeCfg, 
            TypeResolver typeResolver, IBinarySerializer dfltSerializer)
        {
            // Get converter/mapper/serializer.
            IBinaryNameMapper nameMapper = typeCfg.NameMapper ?? cfg.DefaultNameMapper;

            IBinaryIdMapper idMapper = typeCfg.IdMapper ?? cfg.DefaultIdMapper;

            bool keepDeserialized = typeCfg.KeepDeserialized ?? cfg.DefaultKeepDeserialized;

            // Try resolving type.
            Type type = typeResolver.ResolveType(typeCfg.TypeName);

            if (type != null)
            {
                // Type is found.
                var typeName = BinaryUtils.GetTypeName(type);

                int typeId = BinaryUtils.TypeId(typeName, nameMapper, idMapper);

                var serializer = typeCfg.Serializer ?? cfg.DefaultSerializer
                                 ?? GetBinarizableSerializer(type) ?? dfltSerializer;

                var refSerializer = serializer as BinaryReflectiveSerializer;

                if (refSerializer != null)
                    refSerializer.Register(type, typeId, nameMapper, idMapper);

                if (typeCfg.IsEnum != type.IsEnum)
                    throw new BinaryObjectException(
                        string.Format(
                            "Invalid IsEnum flag in binary type configuration. " +
                            "Configuration value: IsEnum={0}, actual type: IsEnum={1}",
                            typeCfg.IsEnum, type.IsEnum));

                AddType(type, typeId, typeName, true, keepDeserialized, nameMapper, idMapper, serializer,
                    typeCfg.AffinityKeyFieldName, type.IsEnum);
            }
            else
            {
                // Type is not found.
                string typeName = BinaryUtils.SimpleTypeName(typeCfg.TypeName);

                int typeId = BinaryUtils.TypeId(typeName, nameMapper, idMapper);

                AddType(null, typeId, typeName, true, keepDeserialized, nameMapper, idMapper, null,
                    typeCfg.AffinityKeyFieldName, typeCfg.IsEnum);
            }
        }

        /// <summary>
        /// Gets the <see cref="BinarizableSerializer"/> for a type if it is compatible.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Resulting <see cref="BinarizableSerializer"/>, or null.</returns>
        private static IBinarySerializer GetBinarizableSerializer(Type type)
        {
            return type.GetInterfaces().Contains(typeof (IBinarizable)) 
                ? BinarizableSerializer.Instance 
                : null;
        }

        /// <summary>
        /// Add type.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="typeId">Type ID.</param>
        /// <param name="typeName">Type name.</param>
        /// <param name="userType">User type flag.</param>
        /// <param name="keepDeserialized">Whether to cache deserialized value in IBinaryObject</param>
        /// <param name="nameMapper">Name mapper.</param>
        /// <param name="idMapper">ID mapper.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="affKeyFieldName">Affinity key field name.</param>
        /// <param name="isEnum">Enum flag.</param>
        private void AddType(Type type, int typeId, string typeName, bool userType, 
            bool keepDeserialized, IBinaryNameMapper nameMapper, IBinaryIdMapper idMapper,
            IBinarySerializer serializer, string affKeyFieldName, bool isEnum)
        {
            long typeKey = BinaryUtils.TypeKey(userType, typeId);

            IBinaryTypeDescriptor conflictingType;

            if (_idToDesc.TryGetValue(typeKey, out conflictingType))
            {
                var type1 = conflictingType.Type != null
                    ? conflictingType.Type.AssemblyQualifiedName
                    : conflictingType.TypeName;

                var type2 = type != null ? type.AssemblyQualifiedName : typeName;

                throw new BinaryObjectException(string.Format("Conflicting type IDs [type1='{0}', " +
                                                              "type2='{1}', typeId={2}]", type1, type2, typeId));
            }

            if (userType && _typeNameToDesc.ContainsKey(typeName))
                throw new BinaryObjectException("Conflicting type name: " + typeName);

            var descriptor = new BinaryFullTypeDescriptor(type, typeId, typeName, userType, nameMapper, idMapper, 
                serializer, keepDeserialized, affKeyFieldName, isEnum);

            if (type != null)
                _typeToDesc[type] = descriptor;

            if (userType)
                _typeNameToDesc[typeName] = descriptor;

            _idToDesc[typeKey] = descriptor;            
        }

        /// <summary>
        /// Adds a predefined system type.
        /// </summary>
        private void AddSystemType<T>(byte typeId, Func<BinaryReader, T> ctor) where T : IBinaryWriteAware
        {
            var type = typeof(T);

            var serializer = new BinarySystemTypeSerializer<T>(ctor);

            AddType(type, typeId, BinaryUtils.GetTypeName(type), false, false, null, null, serializer, null, false);
        }

        /// <summary>
        /// Adds predefined system types.
        /// </summary>
        private void AddSystemTypes()
        {
            AddSystemType(BinaryUtils.TypeNativeJobHolder, w => new ComputeJobHolder(w));
            AddSystemType(BinaryUtils.TypeComputeJobWrapper, w => new ComputeJobWrapper(w));
            AddSystemType(BinaryUtils.TypeIgniteProxy, w => new IgniteProxy());
            AddSystemType(BinaryUtils.TypeComputeOutFuncJob, w => new ComputeOutFuncJob(w));
            AddSystemType(BinaryUtils.TypeComputeOutFuncWrapper, w => new ComputeOutFuncWrapper(w));
            AddSystemType(BinaryUtils.TypeComputeFuncWrapper, w => new ComputeFuncWrapper(w));
            AddSystemType(BinaryUtils.TypeComputeFuncJob, w => new ComputeFuncJob(w));
            AddSystemType(BinaryUtils.TypeComputeActionJob, w => new ComputeActionJob(w));
            AddSystemType(BinaryUtils.TypeContinuousQueryRemoteFilterHolder, w => new ContinuousQueryFilterHolder(w));
            AddSystemType(BinaryUtils.TypeSerializableHolder, w => new SerializableObjectHolder(w));
            AddSystemType(BinaryUtils.TypeDateTimeHolder, w => new DateTimeHolder(w));
            AddSystemType(BinaryUtils.TypeCacheEntryProcessorHolder, w => new CacheEntryProcessorHolder(w));
            AddSystemType(BinaryUtils.TypeCacheEntryPredicateHolder, w => new CacheEntryFilterHolder(w));
            AddSystemType(BinaryUtils.TypeMessageListenerHolder, w => new MessageListenerHolder(w));
            AddSystemType(BinaryUtils.TypeStreamReceiverHolder, w => new StreamReceiverHolder(w));
        }
    }
}
