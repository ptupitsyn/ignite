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

namespace Apache.Ignite.Core.Communication
{
    using System;

    /// <summary>
    /// Defines additional node connectivity configuration, such as binary TCP and JSON over HTTP.
    /// <para />
    /// By default, when ignite-rest-http module is present in classpath (corresponding NuGet package is installed),
    /// HTTP interface is available on 8080 port.
    /// <example>
    /// curl http://localhost:8080/ignite?cmd=version
    /// </example>
    /// </summary>
    public sealed class ConnectorConfiguration
    {
        /// <summary>
        /// Gets or sets the host. This can be either an IP address or a domain name.
        /// <para />
        /// If not defined, system-wide local address will be used (see <see cref="IgniteConfiguration.Localhost"/>).
        /// <para />
        /// You can also use <c>0.0.0.0</c> to bind to all locally-available IP addresses.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the idle query cursor check frequency that is used to discard inactive query cursors.
        /// </summary>
        public TimeSpan IdleQueryCursorCheckFrequency { get; set; }

        /// <summary>
        /// Gets or sets the idle query cursor timeout. If no fetch request occurs within timeout, the cursor will be
        /// discarded on next idle check (see <see cref="IdleQueryCursorCheckFrequency"/>).
        /// </summary>
        public TimeSpan IdleQueryCursorTimeout { get; set; }

        /// <summary>
        /// Gets or sets the idle timeout. Half-opened sockets are closed when no packets come within this timeout.
        /// </summary>
        public TimeSpan IdleTimeout { get; set; }

        /// <summary>
        /// Gets or sets the Jetty web server configuration path.
        /// </summary>
        public string JettyConfigPath { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the port range. When <see cref="Port"/> is already in use, a number of consecutive ports
        /// will be tried according to specified range.
        /// </summary>
        public int PortRange { get; set; }
    }
}
