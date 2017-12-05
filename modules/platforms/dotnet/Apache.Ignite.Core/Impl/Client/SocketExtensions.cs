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
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Socket extension methods.
    /// </summary>
    internal static class SocketExtensions
    {
        /// <summary>
        /// Sends data asynchronously.
        /// </summary>
        /// <param name="socket">Socket.</param>
        /// <param name="buffer">Buffer.</param>
        /// <param name="length">Length.</param>
        /// <returns>Task where result is sent bytes.</returns>
        public static Task<int> SendAsync(this Socket socket, byte[] buffer, int length)
        {
            Debug.Assert(socket != null);
            Debug.Assert(buffer != null);
            Debug.Assert(length > 0 && length <= buffer.Length);

            return Task<int>.Factory.FromAsync((cb, state) =>
                    socket.BeginSend(buffer, 0, length, SocketFlags.None, cb, state),
                    socket.EndSend, null);
        }
    }
}
