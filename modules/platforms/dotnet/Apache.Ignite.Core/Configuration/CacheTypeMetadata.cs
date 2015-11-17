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
        public ICollection<string> TextFields { get; set; }
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
        internal CacheTypeMetadata(IBinaryRawReader reader)
        {
            DatabaseSchemaName = reader.ReadString();
            DatabaseTableName = reader.ReadString();

            KeyType = ReadType(reader);
            ValueType = ReadType(reader);

            QueryFields = ReadDictionary(reader);
            TextFields = ReadCollection(reader);
            AscendingFields = ReadDictionary(reader);
            DescendingFields = ReadDictionary(reader);

            Aliases = ReadStringDictionary(reader);
        }

        /// <summary>
        /// Writes this instance to the specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        internal void Write(IBinaryRawWriter writer)
        {
            writer.WriteString(DatabaseSchemaName);
            writer.WriteString(DatabaseTableName);

            writer.WriteString(GetJavaTypeName(KeyType));
            writer.WriteString(GetJavaTypeName(ValueType));

            WriteDictionary(writer, QueryFields);
            WriteCollection(writer, TextFields);
            WriteDictionary(writer, AscendingFields);
            WriteDictionary(writer, DescendingFields);

            WriteDictionary(writer, Aliases);
        }

        /// <summary>
        /// Gets the corresponding Java type name.
        /// </summary>
        private static string GetJavaTypeName(Type type)
        {
            if (type == null)
                return null;

            var typeName = JavaTypes.GetJavaTypeName(type);

            if (typeName == null)
                throw new InvalidOperationException(
                    string.Format(
                        "Unsupported type in cache type metadata: {0}. " +
                        "The following types are supported in queries: {1}", type, JavaTypes.SupportedTypesString));
            return typeName;
        }

        /// <summary>
        /// Reads type from the reader.
        /// </summary>
        private static Type ReadType(IBinaryRawReader reader)
        {
            var typeName = reader.ReadString();

            if (typeName == null)
                return null;   // TODO: Think

            return JavaTypes.GetDotNetType(typeName);
        }

        /// <summary>
        /// Writes dictionary to the writer.
        /// </summary>
        private static void WriteDictionary(IBinaryRawWriter writer, IDictionary<string, Type> dict)
        {
            WriteCollection(writer, dict, (w, p) =>
            {
                w.WriteString(p.Key);
                w.WriteString(GetJavaTypeName(p.Value));
            });
        }

        /// <summary>
        /// Writes collection to the writer.
        /// </summary>
        private static void WriteCollection<T>(IBinaryRawWriter writer, ICollection<T> col,
            Action<IBinaryRawWriter, T> writeAction = null)
        {
            if (col == null)
            {
                writer.WriteInt(0);
                return;
            }

            writer.WriteInt(col.Count);

            writeAction = writeAction ?? ((w, val) => w.WriteObject(val));

            foreach (var e in col)
                writeAction(writer, e);
        }

        /// <summary>
        /// Reads dictionary.
        /// </summary>
        private static Dictionary<string, Type> ReadDictionary(IBinaryRawReader reader)
        {
            var count = reader.ReadInt();

            if (count == 0)
                return null;

            Debug.Assert(count > 0);

            var dict = new Dictionary<string, Type>(count);

            for (var i = 0; i < count; i++)
                dict.Add(reader.ReadString(), ReadType(reader));

            return dict;
        }

        /// <summary>
        /// Reads collection.
        /// </summary>
        private static ICollection<string> ReadCollection(IBinaryRawReader reader)
        {
            var count = reader.ReadInt();

            if (count == 0)
                return null;

            Debug.Assert(count > 0);

            var list = new List<string>(count);

            for (var i = 0; i < count; i++)
                list.Add(reader.ReadString());

            return list;

        }

        /// <summary>
        /// Reads dictionary.
        /// </summary>
        private static Dictionary<string, string> ReadStringDictionary(IBinaryRawReader reader)
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
