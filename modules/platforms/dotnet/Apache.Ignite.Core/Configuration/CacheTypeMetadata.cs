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

namespace Apache.Ignite.Core.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary;

    /// <summary>
    /// Cache type metadata describes cache type for queries.
    /// </summary>
    public class CacheTypeMetadata
    {
        /// <summary>
        /// Gets or sets the name of the database schema.
        /// </summary>
        public string DatabaseSchemaName { get; set; }

        /// <summary>
        /// Gets or sets the name of the database table.
        /// </summary>
        public string DatabaseTableName { get; set; }

        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        public Type KeyType { get; set; }

        /// <summary>
        /// Gets or sets the type of the value.
        /// </summary>
        public Type ValueType { get; set; }

        /// <summary>
        /// Gets or sets the query fields.
        /// </summary>
        public IDictionary<string, Type> QueryFields { get; set; }

        /// <summary>
        /// Gets or sets the text fields.
        /// </summary>
        public IDictionary<string, Type> TextFields { get; set; }
        /// <summary>
        /// Gets or sets the ascending fields.
        /// </summary>
        public IDictionary<string, Type> AscendingFields { get; set; }

        /// <summary>
        /// Gets or sets the descending fields.
        /// </summary>
        public IDictionary<string, Type> DescendingFields { get; set; }

        /// <summary>
        /// Gets or sets the aliases.
        /// </summary>
        public IDictionary<string, string> Aliases { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheTypeMetadata"/> class.
        /// </summary>
        public CacheTypeMetadata()
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheTypeMetadata" /> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="binaryConfiguration">The binary configuration.</param>
        internal CacheTypeMetadata(IBinaryRawReader reader, BinaryConfiguration binaryConfiguration)
        {
            DatabaseSchemaName = reader.ReadString();
            DatabaseTableName = reader.ReadString();

            KeyType = ReadType(reader, binaryConfiguration);
            ValueType = ReadType(reader, binaryConfiguration);

            QueryFields = ReadDictionary(reader, binaryConfiguration);
            TextFields = ReadDictionary(reader, binaryConfiguration);
            AscendingFields = ReadDictionary(reader, binaryConfiguration);
            DescendingFields = ReadDictionary(reader, binaryConfiguration);

            Aliases = ReadDictionary(reader);
        }

        /// <summary>
        /// Writes this instance to the specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        internal void Write(IBinaryRawWriter writer)
        {
            writer.WriteString(DatabaseSchemaName);
            writer.WriteString(DatabaseTableName);

            WriteType(writer, KeyType);
            WriteType(writer, ValueType);

            WriteDictionary(writer, QueryFields);
            WriteDictionary(writer, TextFields);
            WriteDictionary(writer, AscendingFields);
            WriteDictionary(writer, DescendingFields);

            WriteDictionary(writer, Aliases);
        }

        /// <summary>
        /// Writes type to the writer.
        /// </summary>
        private static void WriteType(IBinaryRawWriter writer, Type type)
        {
            var typeName = type == null ? null : BinaryUtils.SimpleTypeName(type.Name);

            writer.WriteString(typeName);
        }

        /// <summary>
        /// Reads type from the reader.
        /// </summary>
        private static Type ReadType(IBinaryRawReader reader, BinaryConfiguration cfg)
        {
            var typeName = reader.ReadString();

            if (typeName == null || cfg == null || (cfg.TypeConfigurations == null && cfg.Types == null))
                return null;

            var typeNames = cfg.Types ?? Enumerable.Empty<string>();

            if (cfg.TypeConfigurations != null)
                typeNames = typeNames.Concat(cfg.TypeConfigurations.Select(x => x.TypeName));

            var nsName = '.' + typeName;

            return typeNames.Where(x => x.EndsWith(nsName)).Select(x => Type.GetType(x, false)).FirstOrDefault();
        }

        /// <summary>
        /// Writes type to the writer.
        /// </summary>
        private static void WriteDictionary(IBinaryRawWriter writer, IDictionary<string, Type> dict)
        {
            if (dict == null)
            {
                writer.WriteInt(0);
                return;
            }

            writer.WriteInt(dict.Count);

            foreach (var pair in dict)
            {
                writer.WriteString(pair.Key);
                WriteType(writer, pair.Value);
            }
        }

        /// <summary>
        /// Reads dictionary.
        /// </summary>
        private static Dictionary<string, Type> ReadDictionary(IBinaryRawReader reader, BinaryConfiguration cfg)
        {
            var count = reader.ReadInt();

            if (count == 0)
                return null;

            Debug.Assert(count > 0);

            var dict = new Dictionary<string, Type>();

            for (var i = 0; i < count; i++)
                dict.Add(reader.ReadString(), ReadType(reader, cfg));

            return dict;
        }

        /// <summary>
        /// Reads dictionary.
        /// </summary>
        private static Dictionary<string, string> ReadDictionary(IBinaryRawReader reader)
        {
            var count = reader.ReadInt();

            if (count == 0)
                return null;

            Debug.Assert(count > 0);

            var dict = new Dictionary<string, string>();

            for (var i = 0; i < count; i++)
                dict.Add(reader.ReadString(), reader.ReadString());

            return dict;
        }

        /// <summary>
        /// Writes type to the writer.
        /// </summary>
        private static void WriteDictionary(IBinaryRawWriter writer, IDictionary<string, string> dict)
        {
            if (dict == null)
            {
                writer.WriteInt(0);
                return;
            }

            writer.WriteInt(dict.Count);

            foreach (var pair in dict)
            {
                writer.WriteString(pair.Key);
                writer.WriteString(pair.Value);
            }
        }
    }
}
