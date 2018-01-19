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
    using System.IO;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Provides access to <see cref="IgniteConfigurationSection"/> in config files.
    /// </summary>
    internal static class IgniteConfigurationManager
    {
        /// <summary>
        /// Gets the ignite configuration section.
        /// </summary>
        public static IgniteConfiguration GetIgniteConfiguration(string sectionName)
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

            return section.IgniteConfiguration;
        }

        /// <summary>
        /// Gets the ignite configuration section.
        /// </summary>
        public static IgniteConfiguration GetIgniteConfiguration(string sectionName, string configPath)
        {
            IgniteArgumentCheck.NotNullOrEmpty(sectionName, "sectionName");
            IgniteArgumentCheck.NotNullOrEmpty(sectionName, "configPath");

            var section = GetConfigurationSection<IgniteConfigurationSection>(sectionName, configPath);

            if (section.IgniteConfiguration == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format("{0} with name '{1}' in file '{2}' is defined in <configSections>, " +
                                  "but not present in configuration.",
                        typeof(IgniteConfigurationSection).Name, sectionName, configPath));
            }

            return section.IgniteConfiguration;
        }

        /// <summary>
        /// Gets the configuration section.
        /// </summary>
        private static T GetConfigurationSection<T>(string sectionName, string configPath)
            where T : ConfigurationSection
        {
            IgniteArgumentCheck.NotNullOrEmpty(sectionName, "sectionName");
            IgniteArgumentCheck.NotNullOrEmpty(configPath, "configPath");

            var fileMap = GetConfigMap(configPath);
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            var section = config.GetSection(sectionName) as T;

            if (section == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format("Could not find {0} with name '{1}' in file '{2}'",
                        typeof(T).Name, sectionName, configPath));
            }

            return section;
        }

        /// <summary>
        /// Gets the configuration file map.
        /// </summary>
        private static ExeConfigurationFileMap GetConfigMap(string fileName)
        {
            var fullFileName = Path.GetFullPath(fileName);

            if (!File.Exists(fullFileName))
                throw new ConfigurationErrorsException("Specified config file does not exist: " + fileName);

            return new ExeConfigurationFileMap { ExeConfigFilename = fullFileName };
        }




    }
}
