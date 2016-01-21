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

namespace Apache.Ignite.Core.Impl.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// Serializes <see cref="IgniteConfiguration"/> to XML.
    /// </summary>
    internal static class IgniteConfigurationXmlSerializer
    {
        /** Attribute that specifies a type for abstract properties, such as IpFinder. */
        private const string TypeNameAttribute = "type";

        /// <summary>
        /// Deserializes <see cref="IgniteConfiguration"/> from specified <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>Resulting <see cref="IgniteConfiguration"/>.</returns>
        public static IgniteConfiguration Deserialize(XmlReader reader)
        {
            var cfg = new IgniteConfiguration();

            if (reader.Read())
                ReadElement(reader, cfg);

            return cfg;
        }

        /// <summary>
        /// Reads the element.
        /// </summary>
        private static void ReadElement(XmlReader reader, object target)
        {
            var targetType = target.GetType();

            // Read attributes
            while (reader.MoveToNextAttribute())
            {
                var name = reader.Name;
                var val = reader.Value;

                SetProperty(target, name, val);
            }

            // Read content
            reader.MoveToElement();

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                var name = reader.Name;
                var prop = GetPropertyOrThrow(name, reader.Value, targetType);
                var propType = prop.PropertyType;

                if (IsBasicType(propType))
                {
                    // Regular property in xmlElement form
                    SetProperty(target, name, reader.ReadString());
                }
                else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof (ICollection<>))
                {
                    // Collection
                    ReadCollection(reader, prop, target);
                }
                else
                {
                    // Nested object (complex property)
                    var nestedVal = ReadNestedObject(reader, propType, prop.Name, targetType);
                    prop.SetValue(target, nestedVal, null);
                }
            }
        }

        /// <summary>
        /// Reads the nested object.
        /// </summary>
        private static object ReadNestedObject(XmlReader reader, Type propType, string propName, Type targetType)
        {
            if (propType.IsAbstract || propType.IsInterface)
            {
                var typeName = reader.GetAttribute(TypeNameAttribute);

                var derivedTypes = GetConcreteDerivedTypes(propType);

                propType = typeName == null
                    ? null
                    : Type.GetType(typeName, false) ?? derivedTypes.FirstOrDefault(x => x.Name == typeName);

                if (propType == null)
                {
                    var message = string.Format("'type' attribute is required for '{0}.{1}' property", targetType.Name,
                        propName);

                    if (typeName != null)
                    {
                        message += ", specified type cannot be resolved: " + typeName;
                    }
                    else if (derivedTypes.Any())
                        message += ", possible values are: " + string.Join(", ", derivedTypes.Select(x => x.Name));

                    throw new ConfigurationErrorsException(message);
                }
            }

            var nestedVal = Activator.CreateInstance(propType);

            using (var subReader = reader.ReadSubtree())
            {
                subReader.Read();  // read first element

                ReadElement(subReader, nestedVal);
            }

            return nestedVal;
        }

        /// <summary>
        /// Reads the collection.
        /// </summary>
        private static void ReadCollection(XmlReader reader, PropertyInfo prop, object target)
        {
            var elementType = prop.PropertyType.GetGenericArguments().Single();

            var listType = typeof (List<>).MakeGenericType(elementType);

            var list = (IList) Activator.CreateInstance(listType);

            var converter = IsBasicType(elementType) ? GetConverter(prop, elementType) : null;

            using (var subReader = reader.ReadSubtree())
            {
                subReader.Read();  // skip list head
                while (subReader.Read())
                {
                    if (subReader.NodeType != XmlNodeType.Element)
                        continue;

                    if (subReader.Name != PropertyNameToXmlName(elementType.Name))
                        throw new ConfigurationErrorsException(
                            string.Format("Invalid list element in IgniteConfiguration: expected '{0}', but was '{1}'",
                                PropertyNameToXmlName(elementType.Name), subReader.Name));

                    list.Add(converter != null
                        ? converter.ConvertFromString(subReader.ReadString())
                        : ReadNestedObject(subReader, elementType, prop.Name, target.GetType()));
                }
            }

            prop.SetValue(target, list, null);
        }

        /// <summary>
        /// Sets the property.
        /// </summary>
        private static void SetProperty(object target, string propName, string propVal)
        {
            if (propName == TypeNameAttribute)
                return;

            var type = target.GetType();
            var property = GetPropertyOrThrow(propName, propVal, type);

            var converter = GetConverter(property, property.PropertyType);

            // TODO: try-catch and wrap
            var convertedVal = converter.ConvertFromString(propVal);

            property.SetValue(target, convertedVal, null);
        }

        /// <summary>
        /// Gets concrete derived types.
        /// </summary>
        private static List<Type> GetConcreteDerivedTypes(Type type)
        {
            return type.Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(type)).ToList();
        }

        /// <summary>
        /// Gets specified property from a type or throws an exception.
        /// </summary>
        private static PropertyInfo GetPropertyOrThrow(string propName, string propVal, Type type)
        {
            var property = type.GetProperty(XmlNameToPropertyName(propName));

            if (property == null)
                throw new ConfigurationErrorsException(
                    string.Format(
                        "Invalid IgniteConfiguration attribute '{0}={1}', there is no such property on '{2}'",
                        propName, propVal, type));

            return property;
        }

        /// <summary>
        /// Converts an XML name to CLR name.
        /// </summary>
        private static string XmlNameToPropertyName(string name)
        {
            Debug.Assert(name.Length > 0);

            if (name == "int")
                return "Int32";  // allow aliases

            return char.ToUpper(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Converts a CLR name to XML name.
        /// </summary>
        private static string PropertyNameToXmlName(string name)
        {
            Debug.Assert(name.Length > 0);

            if (name == "Int32")
                return "int";  // allow aliases

            return char.ToLower(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Determines whether specified type is a basic built-in type.
        /// </summary>
        private static bool IsBasicType(Type propertyType)
        {
            Debug.Assert(propertyType != null);

            return propertyType.IsValueType || propertyType == typeof(string) || propertyType == typeof(Type);
        }

        /// <summary>
        /// Gets converter for a property.
        /// </summary>
        private static TypeConverter GetConverter(PropertyInfo property, Type propertyType)
        {
            Debug.Assert(property != null);
            Debug.Assert(propertyType != null);

            if (propertyType.IsEnum)
                return new GenericEnumConverter(propertyType);

            if (propertyType == typeof (Type))
                return TypeStringConverter.Instance;

            if (property.DeclaringType == typeof (IgniteConfiguration) && property.Name == "IncludedEventTypes")
                return EventTypeConverter.Instance;

            var converter = TypeDescriptor.GetConverter(propertyType);

            if (converter == null || !converter.CanConvertFrom(typeof(string)) ||
                !converter.CanConvertTo(typeof(string)))
                throw new ConfigurationErrorsException("No converter for type " + propertyType);

            return converter;
        }
    }
}
