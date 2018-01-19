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

namespace Apache.Ignite.Core.Impl
{
    using System.Configuration;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Provides access to <see cref="IgniteConfigurationSection"/> in config files.
    /// </summary>
    internal static class IgniteConfigurationManager
    {
        /// <summary>
        /// Gets the ignite configuration section.
        /// </summary>
        public static IgniteConfigurationSection GetIgniteConfigurationSection(string sectionName)
        {
            IgniteArgumentCheck.NotNullOrEmpty(sectionName, "sectionName");

            var section = ConfigurationManager.GetSection(sectionName) as IgniteConfigurationSection;

            if (section == null)
            {
                throw new ConfigurationErrorsException(string.Format("Could not find {0} with name '{1}'",
                    typeof(IgniteConfigurationSection).Name, sectionName));
            }

            if (section.IgniteConfiguration == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format("{0} with name '{1}' is defined in <configSections>, " +
                                  "but not present in configuration.",
                        typeof(IgniteConfigurationSection).Name, sectionName));
            }

            return section;
        }
    }
}
