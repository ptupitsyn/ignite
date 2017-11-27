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

namespace Apache.Ignite.Core.Impl.Client
{
    /// <summary>
    /// Client op code.
    /// </summary>
    internal enum ClientOp : short
    {
        ResourceClose = 0,
        CacheGet = 100,
        CachePut = 101,
        CachePutIfAbsent = 102,
        CacheGetAll = 103,
        CachePutAll = 104,
        CacheGetAndPut = 105,
        CacheGetAndReplace = 106,
        CacheGetAndRemove = 107,
        CacheGetAndPutIfAbsent = 108,
        CacheReplace = 109,
        CacheReplaceIfEquals = 110,
        CacheContainsKey = 111,
        CacheContainsKeys = 112,
        CacheClear = 113,
        CacheClearKey = 114,
        CacheClearKeys = 115,
        CacheRemoveKey = 116,
        CacheRemoveIfEquals = 117,
        CacheRemoveKeys = 118,
        CacheRemoveAll = 119,
        CacheGetSize = 120,
        CacheGetNames = 150,
        CacheCreateWithName = 151,
        CacheGetOrCreateWithName = 152,
        CacheCreateWithConfiguration = 153,
        CacheGetOrCreateWithConfiguration = 154,
        CacheGetConfiguration = 155,
        CacheDestroy = 156,
        QueryScan = 200,
        QueryScanCursorGetPage = 201,
        QuerySql = 202,
        QuerySqlCursorGetPage = 203,
        QuerySqlFields = 204,
        QuerySqlFieldsCursorGetPage = 205,
        BinaryTypeNameGet = 300,
        BinaryTypeNamePut = 301,
        BinaryTypeGet = 302,
        BinaryTypePut = 303
    }
}
