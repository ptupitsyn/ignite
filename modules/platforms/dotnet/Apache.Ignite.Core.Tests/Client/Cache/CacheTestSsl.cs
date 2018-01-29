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

namespace Apache.Ignite.Core.Tests.Client.Cache
{
    using System;
    using Apache.Ignite.Core.Client;
    using NUnit.Framework;

    /// <summary>
    /// Async cache test.
    /// </summary>
    [TestFixture]
    public sealed class CacheTestSsl : CacheTest
    {
        /// <summary>
        /// Gets the Ignite configuration.
        /// </summary>
        protected override IgniteConfiguration GetIgniteConfiguration()
        {
            // TODO: Add our own configs.
            Environment.SetEnvironmentVariable("IGNITE_NATIVE_TEST_ODBC_CONFIG_PATH",
                @"c:\w\incubator-ignite\modules\platforms\cpp\odbc-test\config");

            return new IgniteConfiguration(base.GetIgniteConfiguration())
            {
                SpringConfigUrl = @"c:\w\incubator-ignite\modules\platforms\cpp\odbc-test\config\queries-ssl.xml"
            };
        }

        /// <summary>
        /// Gets the client configuration.
        /// </summary>
        protected override IgniteClientConfiguration GetClientConfiguration()
        {
            return new IgniteClientConfiguration(base.GetClientConfiguration())
            {
                Port = 11110,
                SslStreamFactory = new SslStreamFactory
                {
                    CertificatePath = @"Config\thin-client-cert.pfx",
                    CertificatePassword = "123456",
                    SkipServerCertificateValidation = true
                }
            };
        }
    }
}
