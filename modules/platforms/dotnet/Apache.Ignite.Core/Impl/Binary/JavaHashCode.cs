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

namespace Apache.Ignite.Core.Impl.Binary
{
    /// <summary>
    /// Java hash code implementations for basic types.
    /// </summary>
    internal static class JavaHashCode
    {
        /// <summary>
        /// Gets the hash code using Java algorithm for basic types.
        /// </summary>
        public static int GetHashCode(object val)
        {
            if (val == null)
                return 0;

            // Types check sequence is designed to minimize comparisons for the most frequent types.
            if (val is int)
                return (int)val;

            if (val is long)
            {
                var l = (long) val;
                return (int) (l ^ (l >> 32));
            }

            if (val is bool)
                return (bool) val ? 1231 : 1237;

            // Fall back to default for all other types.
            return val.GetHashCode();
        }
    }
}
