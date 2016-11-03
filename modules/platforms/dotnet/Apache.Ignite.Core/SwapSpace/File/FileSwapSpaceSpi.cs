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

namespace Apache.Ignite.Core.SwapSpace.File
{
    using System.ComponentModel;

    /// <summary>
    /// File-based swap space SPI implementation which holds keys in memory and values on disk.
    /// It is intended for cases when value is bigger than 100 bytes, otherwise it will not 
    /// have any positive effect.
    /// </summary>
    public class FileSwapSpaceSpi : ISwapSpaceSpi
    {
        public const float DefaultMaximumSparsity = 0.5f;

        /// <summary>
        /// Gets or sets the base directory.
        /// </summary>
        public string BaseDirectory { get; set; }

        /// <summary>
        /// Gets or sets the maximum sparsity. This property defines maximum acceptable
        /// wasted file space to whole file size ratio.
        /// When this ratio becomes higher than specified number compacting thread starts working.
        /// </summary>
        /// <value>
        /// The maximum sparsity. Must be between 0 and 1.
        /// </value>
        [DefaultValue(DefaultMaximumSparsity)]
        public float MaximumSparsity { get; set; }

        /// <summary>
        /// Gets or sets the maximum size of the write queue in bytes. If there are more values are waiting
        /// to be written to disk then specified size, SPI will block on write operation.
        /// </summary>
        /// <value>
        /// The maximum size of the write queue in bytes.
        /// </value>
        public int MaximumWriteQueueSize { get; set; }

        public int ReadStripesNumber { get; set; }

        public int WriteBufferSize { get; set; }
    }
}
