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

namespace Apache.Ignite.Core.Impl.Unmanaged.Jni
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Apache.Ignite.Core.Common;
    using Apache.Ignite.Core.Log;
    using Microsoft.Win32;

    /// <summary>
    /// Jvm.dll loader (libjvm.so on Linux, libjvm.dylib on macOs).
    /// </summary>
    internal static class JvmDll
    {
        /** Environment variable: JAVA_HOME. */
        private const string EnvJavaHome = "JAVA_HOME";

        /** Lookup paths. */
        private static readonly string[] JvmDllLookupPaths = Os.IsWindows
            ? new[]
            {
                // JRE paths
                @"bin\server",
                @"bin\client",

                // JDK paths
                @"jre\bin\server",
                @"jre\bin\client",
                @"jre\bin\default"
            }
            : new[]
            {
                // JRE paths
                "lib/server",
                "lib/client",
                "lib/amd64/server",
                "lib/amd64/client",

                // JDK paths
                "jre/lib/server",
                "jre/lib/client",
                "jre/lib/amd64/server",
                "jre/lib/amd64/client"
            };

        /** Registry lookup paths. */
        private static readonly string[] JreRegistryKeys =
        {
            @"Software\JavaSoft\Java Runtime Environment",
            @"Software\Wow6432Node\JavaSoft\Java Runtime Environment"
        };

        /** Jvm dll file name. */
        internal static readonly string FileJvmDll = Os.IsWindows
            ? "jvm.dll"
            : Os.IsMacOs
                ? "libjvm.dylib"
                : "libjvm.so";

        /// <summary>
        /// Loads the JVM DLL.
        /// </summary>
        public static void Load(string configJvmDllPath, ILogger log)
        {
            // TODO: Keep ptr in a static variable.
            // TODO: log.Debug("JNI dll is already loaded.");

            var messages = new List<string>();
            foreach (var dllPath in GetJvmDllPaths(configJvmDllPath))
            {
                log.Debug("Trying to load {0} from [option={1}, path={2}]...", FileJvmDll, dllPath.Key, dllPath.Value);

                var errInfo = LoadDll(dllPath.Value, FileJvmDll);
                if (errInfo == null)
                {
                    log.Debug("{0} successfully loaded from [option={1}, path={2}]",
                        FileJvmDll, dllPath.Key, dllPath.Value);
                    return;
                }

                var message = string.Format(CultureInfo.InvariantCulture, "[option={0}, path={1}, error={2}]",
                    dllPath.Key, dllPath.Value, errInfo);
                messages.Add(message);

                log.Debug("Failed to load {0}:  {1}", FileJvmDll, message);

                if (dllPath.Value == configJvmDllPath)
                    break; // if configJvmDllPath is specified and is invalid - do not try other options
            }

            if (!messages.Any()) // not loaded and no messages - everything was null
            {
                messages.Add(string.Format(CultureInfo.InvariantCulture,
                    "Please specify IgniteConfiguration.JvmDllPath or {0}.", EnvJavaHome));
            }

            if (messages.Count == 1)
            {
                throw new IgniteException(string.Format(CultureInfo.InvariantCulture, "Failed to load {0} ({1})",
                    FileJvmDll, messages[0]));
            }

            var combinedMessage =
                messages.Aggregate((x, y) => string.Format(CultureInfo.InvariantCulture, "{0}\n{1}", x, y));

            throw new IgniteException(string.Format(CultureInfo.InvariantCulture, "Failed to load {0}:\n{1}",
                FileJvmDll, combinedMessage));
        }

        /// <summary>
        /// Try loading DLLs first using file path, then using it's simple name.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="simpleName"></param>
        /// <returns>Null in case of success, error info in case of failure.</returns>
        private static string LoadDll(string filePath, string simpleName)
        {
            string res = null;

            if (filePath != null)
            {
                res = DllLoader.Load(filePath);

                if (res == null)
                {
                    return null; // Success.
                }
            }

            // Failed to load using file path, fallback to simple name.
            var res2 = DllLoader.Load(simpleName);

            if (res2 == null)
            {
                return null; // Success.
            }

            return res;
        }

        /// <summary>
        /// Gets the JVM DLL paths in order of lookup priority.
        /// </summary>
        private static IEnumerable<KeyValuePair<string, string>> GetJvmDllPaths(string configJvmDllPath)
        {
            if (!string.IsNullOrEmpty(configJvmDllPath))
            {
                yield return new KeyValuePair<string, string>("IgniteConfiguration.JvmDllPath", configJvmDllPath);
            }

            var javaHomeDir = Environment.GetEnvironmentVariable(EnvJavaHome);

            if (!string.IsNullOrEmpty(javaHomeDir))
            {
                foreach (var path in JvmDllLookupPaths)
                {
                    yield return
                        new KeyValuePair<string, string>(EnvJavaHome, Path.Combine(javaHomeDir, path, FileJvmDll));
                }
            }

            foreach (var keyValuePair in GetJvmDllPathsFromRegistry().Concat(GetJvmDllPathsFromSymlink()))
            {
                yield return keyValuePair;
            }
        }

        /// <summary>
        /// Gets Jvm dll paths from Windows registry.
        /// </summary>
        private static IEnumerable<KeyValuePair<string, string>> GetJvmDllPathsFromRegistry()
        {
            if (!Os.IsWindows)
            {
                yield break;
            }

            foreach (var regPath in JreRegistryKeys)
            {
                using (var jSubKey = Registry.LocalMachine.OpenSubKey(regPath))
                {
                    if (jSubKey == null)
                        continue;

                    var curVer = jSubKey.GetValue("CurrentVersion") as string;

                    // Current version comes first
                    var versions = new[] {curVer}.Concat(jSubKey.GetSubKeyNames().Where(x => x != curVer));

                    foreach (var ver in versions.Where(v => !string.IsNullOrEmpty(v)))
                    {
                        using (var verKey = jSubKey.OpenSubKey(ver))
                        {
                            var dllPath = verKey == null ? null : verKey.GetValue("RuntimeLib") as string;

                            if (dllPath != null)
                                yield return new KeyValuePair<string, string>(verKey.Name, dllPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Jvm dll paths from symlink.
        /// </summary>
        private static IEnumerable<KeyValuePair<string, string>> GetJvmDllPathsFromSymlink()
        {
            if (Os.IsWindows || Os.IsMacOs)
            {
                yield break;
            }

            const string javaExec = "/usr/bin/java";
            if (!File.Exists(javaExec))
            {
                yield break;
            }

            var file = Shell.BashExecute("readlink -f /usr/bin/java");
            // /usr/lib/jvm/java-8-openjdk-amd64/jre/bin/java

            var dir = Path.GetDirectoryName(file);
            // /usr/lib/jvm/java-8-openjdk-amd64/jre/bin

            if (dir == null)
            {
                yield break;
            }

            var libFolder = Path.GetFullPath(Path.Combine(dir, "../lib/"));
            if (!Directory.Exists(libFolder))
            {
                yield break;
            }

            // Predefined path: /usr/lib/jvm/java-8-openjdk-amd64/jre/lib/amd64/server/libjvm.so
            yield return new KeyValuePair<string, string>(javaExec,
                Path.Combine(libFolder, "amd64", "server", FileJvmDll));

            // Last resort - custom paths:
            foreach (var f in Directory.GetFiles(libFolder, FileJvmDll, SearchOption.AllDirectories))
            {
                yield return new KeyValuePair<string, string>(javaExec, f);
            }
        }
    }
}