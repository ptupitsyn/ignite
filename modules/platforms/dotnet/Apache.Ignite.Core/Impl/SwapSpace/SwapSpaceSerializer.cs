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

namespace Apache.Ignite.Core.Impl.SwapSpace
{
    using System;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.SwapSpace;
    using Apache.Ignite.Core.SwapSpace.File;
    using Apache.Ignite.Core.SwapSpace.Noop;

    /// <summary>
    /// SwapSpace config serializer.
    /// </summary>
    internal static class SwapSpaceSerializer
    {
        /// <summary>
        /// SwapSpace type.
        /// </summary>
        private enum Type : byte
        {
            None,
            File,
            Noop
        }

        /// <summary>
        /// Writes the configuration to writer.
        /// </summary>
        public static void Write(IBinaryRawWriter writer, ISwapSpaceSpi spi)
        {
            var fileSwap = spi as FileSwapSpaceSpi;

            if (spi == null)
            {
                writer.WriteByte((byte) Type.None);
            }
            else if (fileSwap != null)
            {
                writer.WriteByte((byte) Type.File);

                writer.WriteString(fileSwap.BaseDirectory);
                writer.WriteFloat(fileSwap.MaximumSparsity);
                writer.WriteInt(fileSwap.MaximumWriteQueueSize);
                writer.WriteInt(fileSwap.ReadStripesNumber);
                writer.WriteInt(fileSwap.WriteBufferSize);

            }
            else if (spi is NoopSwapSpaceSpi)
            {
                writer.WriteByte((byte) Type.Noop);
            }
            else
            {
                throw new InvalidOperationException("Unsupported swap space SPI: " + spi.GetType());
            }
        }

        /// <summary>
        /// Reads the configuration from reader.
        /// </summary>
        public static ISwapSpaceSpi Read(IBinaryRawReader reader)
        {
            var type = (Type)reader.ReadByte();

            switch (type)
            {
                case Type.None:
                    return null;

                case Type.File:
                    return new FileSwapSpaceSpi
                    {
                        BaseDirectory = reader.ReadString(),
                        MaximumSparsity = reader.ReadFloat(),
                        MaximumWriteQueueSize = reader.ReadInt(),
                        ReadStripesNumber = reader.ReadInt(),
                        WriteBufferSize = reader.ReadInt()
                    };

                case Type.Noop:
                    return new NoopSwapSpaceSpi();

                default:
                    throw new ArgumentOutOfRangeException("Invalid Swap Space SPI type: " + type);
            }
        }
    }
}
