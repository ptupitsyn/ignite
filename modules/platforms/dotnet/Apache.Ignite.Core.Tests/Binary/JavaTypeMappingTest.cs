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

namespace Apache.Ignite.Core.Tests.Binary
{
    using System;
    using Apache.Ignite.Core.Impl.Binary;
    using NUnit.Framework;

    /// <summary>
    /// Tests the type mapping between .NET and Java.
    /// </summary>
    public class JavaTypeMappingTest
    {
        /// <summary>
        /// Tests .NET to Java type mapping.
        /// </summary>
        [Test]
        public void TestDotNetToJavaMapping()
        {
            Assert.AreEqual("java.lang.Boolean", JavaTypes.GetJavaTypeName(typeof(bool)));
            Assert.AreEqual("java.lang.Boolean", JavaTypes.GetJavaTypeName(typeof(bool?)));

            Assert.AreEqual("java.lang.Byte", JavaTypes.GetJavaTypeName(typeof(byte)));
            Assert.AreEqual("java.lang.Byte", JavaTypes.GetJavaTypeName(typeof(byte?)));
            Assert.AreEqual("java.lang.Byte", JavaTypes.GetJavaTypeName(typeof(sbyte)));
            Assert.AreEqual("java.lang.Byte", JavaTypes.GetJavaTypeName(typeof(sbyte?)));

            Assert.AreEqual("java.lang.Short", JavaTypes.GetJavaTypeName(typeof(short)));
            Assert.AreEqual("java.lang.Short", JavaTypes.GetJavaTypeName(typeof(short?)));
            Assert.AreEqual("java.lang.Short", JavaTypes.GetJavaTypeName(typeof(ushort)));
            Assert.AreEqual("java.lang.Short", JavaTypes.GetJavaTypeName(typeof(ushort?)));

            Assert.AreEqual("java.lang.Integer", JavaTypes.GetJavaTypeName(typeof(int)));
            Assert.AreEqual("java.lang.Integer", JavaTypes.GetJavaTypeName(typeof(int?)));
            Assert.AreEqual("java.lang.Integer", JavaTypes.GetJavaTypeName(typeof(uint)));
            Assert.AreEqual("java.lang.Integer", JavaTypes.GetJavaTypeName(typeof(uint?)));

            Assert.AreEqual("java.lang.Long", JavaTypes.GetJavaTypeName(typeof(long)));
            Assert.AreEqual("java.lang.Long", JavaTypes.GetJavaTypeName(typeof(long?)));
            Assert.AreEqual("java.lang.Long", JavaTypes.GetJavaTypeName(typeof(ulong)));
            Assert.AreEqual("java.lang.Long", JavaTypes.GetJavaTypeName(typeof(ulong?)));

            Assert.AreEqual("java.lang.Float", JavaTypes.GetJavaTypeName(typeof(float)));
            Assert.AreEqual("java.lang.Float", JavaTypes.GetJavaTypeName(typeof(float?)));

            Assert.AreEqual("java.lang.Double", JavaTypes.GetJavaTypeName(typeof(double)));
            Assert.AreEqual("java.lang.Double", JavaTypes.GetJavaTypeName(typeof(double?)));

            Assert.AreEqual("java.math.BigDecimal", JavaTypes.GetJavaTypeName(typeof(decimal)));
            Assert.AreEqual("java.math.BigDecimal", JavaTypes.GetJavaTypeName(typeof(decimal?)));

            Assert.AreEqual("java.lang.Character", JavaTypes.GetJavaTypeName(typeof(char)));
            Assert.AreEqual("java.lang.Character", JavaTypes.GetJavaTypeName(typeof(char?)));

            Assert.AreEqual("java.lang.String", JavaTypes.GetJavaTypeName(typeof(string)));

            Assert.AreEqual("java.sql.Timestamp", JavaTypes.GetJavaTypeName(typeof(DateTime)));
            Assert.AreEqual("java.sql.Timestamp", JavaTypes.GetJavaTypeName(typeof(DateTime?)));

            Assert.AreEqual("java.util.UUID", JavaTypes.GetJavaTypeName(typeof(Guid)));
            Assert.AreEqual("java.util.UUID", JavaTypes.GetJavaTypeName(typeof(Guid?)));
        }

        /// <summary>
        /// Tests the indirect mapping check.
        /// </summary>
        [Test]
        public void TestIndirectMappingCheck()
        {
            Assert.AreEqual(typeof(bool), JavaTypes.GetDirectlyMappedType(typeof(bool)));
            Assert.AreEqual(typeof(bool?), JavaTypes.GetDirectlyMappedType(typeof(bool?)));
            Assert.AreEqual(typeof(byte), JavaTypes.GetDirectlyMappedType(typeof(byte)));
            Assert.AreEqual(typeof(byte?), JavaTypes.GetDirectlyMappedType(typeof(byte?)));
            Assert.AreEqual(typeof(char), JavaTypes.GetDirectlyMappedType(typeof(char)));
            Assert.AreEqual(typeof(char?), JavaTypes.GetDirectlyMappedType(typeof(char?)));
            Assert.AreEqual(typeof(DateTime), JavaTypes.GetDirectlyMappedType(typeof(DateTime)));
            Assert.AreEqual(typeof(DateTime?), JavaTypes.GetDirectlyMappedType(typeof(DateTime?)));
            Assert.AreEqual(typeof(decimal), JavaTypes.GetDirectlyMappedType(typeof(decimal)));
            Assert.AreEqual(typeof(decimal?), JavaTypes.GetDirectlyMappedType(typeof(decimal?)));
            Assert.AreEqual(typeof(double), JavaTypes.GetDirectlyMappedType(typeof(double)));
            Assert.AreEqual(typeof(double?), JavaTypes.GetDirectlyMappedType(typeof(double?)));
            Assert.AreEqual(typeof(float), JavaTypes.GetDirectlyMappedType(typeof(float)));
            Assert.AreEqual(typeof(float?), JavaTypes.GetDirectlyMappedType(typeof(float?)));
            Assert.AreEqual(typeof(Guid), JavaTypes.GetDirectlyMappedType(typeof(Guid)));
            Assert.AreEqual(typeof(Guid?), JavaTypes.GetDirectlyMappedType(typeof(Guid?)));
            Assert.AreEqual(typeof(int), JavaTypes.GetDirectlyMappedType(typeof(int)));
            Assert.AreEqual(typeof(int?), JavaTypes.GetDirectlyMappedType(typeof(int?)));
            Assert.AreEqual(typeof(long), JavaTypes.GetDirectlyMappedType(typeof(long)));
            Assert.AreEqual(typeof(long?), JavaTypes.GetDirectlyMappedType(typeof(long?)));
            Assert.AreEqual(typeof(byte), JavaTypes.GetDirectlyMappedType(typeof(sbyte)));
            Assert.AreEqual(typeof(byte), JavaTypes.GetDirectlyMappedType(typeof(sbyte?)));
            Assert.AreEqual(typeof(short), JavaTypes.GetDirectlyMappedType(typeof(short)));
            Assert.AreEqual(typeof(short?), JavaTypes.GetDirectlyMappedType(typeof(short?)));
            Assert.AreEqual(typeof(string), JavaTypes.GetDirectlyMappedType(typeof(string)));
            Assert.AreEqual(typeof(int), JavaTypes.GetDirectlyMappedType(typeof(uint)));
            Assert.AreEqual(typeof(int), JavaTypes.GetDirectlyMappedType(typeof(uint?)));
            Assert.AreEqual(typeof(long), JavaTypes.GetDirectlyMappedType(typeof(ulong)));
            Assert.AreEqual(typeof(long), JavaTypes.GetDirectlyMappedType(typeof(ulong?)));
            Assert.AreEqual(typeof(short), JavaTypes.GetDirectlyMappedType(typeof(ushort)));
            Assert.AreEqual(typeof(short), JavaTypes.GetDirectlyMappedType(typeof(ushort?)));

            // Arbitrary type.
            Assert.AreEqual(typeof(JavaTypeMappingTest), JavaTypes.GetDirectlyMappedType(typeof(JavaTypeMappingTest)));
        }
    }
}
