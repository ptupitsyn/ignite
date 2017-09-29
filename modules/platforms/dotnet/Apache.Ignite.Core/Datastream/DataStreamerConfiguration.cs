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

namespace Apache.Ignite.Core.Datastream
{
    /// <summary>
    /// Data streamer configuration.
    /// </summary>
    public static class DataStreamerConfiguration
    {
        /// <summary>
        /// The default per node buffer size, see <see cref="IDataStreamer{TK,TV}.PerNodeBufferSize"/>.
        /// </summary>
        public const int DefaultPerNodeBufferSize = 512;

        /// <summary>
        /// Default multiplier for parallel operations per node:
        /// <see cref="IDataStreamer{TK,TV}.PerNodeParallelOperations"/> = 
        /// <see cref="IgniteConfiguration.DataStreamerThreadPoolSize"/> * 
        /// <see cref="DefaultParallelOperationsMultiplier"/>.
        /// </summary>
        public const int DefaultParallelOperationsMultiplier = 8;
    }
}
