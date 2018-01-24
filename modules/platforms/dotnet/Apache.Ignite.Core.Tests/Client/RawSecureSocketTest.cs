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

namespace Apache.Ignite.Core.Tests.Client
{
    using Apache.Ignite.Core.Client;
    using NUnit.Framework;

    /// <summary>
    /// Tests the thin client mode with a raw secure socket.
    /// </summary>
    public class RawSecureSocketTest
    {
        // TODO: See queries_ssl_test.cpp, queries-ssl.xml
        
        
        [Test]
        public void TestSslOnServer()
        {
            using (var ignite = Ignition.Start(
                @"S:\W\incubator-ignite\modules\platforms\cpp\odbc-test\config\queries-ssl.xml"))
            {
                var cfg = new IgniteClientConfiguration
                {
                    Host = "127.0.0.1",
                    Port = 11110
                };
                using (var client = Ignition.StartClient(cfg))
                {
                    client.GetCacheNames();
                }
            }
        }
    }
}
