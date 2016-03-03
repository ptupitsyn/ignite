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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Security;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Cache.Store;
    using Apache.Ignite.Core.Cluster;
    using Apache.Ignite.Core.Common;
    using Apache.Ignite.Core.Compute;
    using Apache.Ignite.Core.Impl.Binary;
    using Apache.Ignite.Core.Transactions;

    /// <summary>
    /// Managed environment. Acts as a gateway for native code.
    /// </summary>
    internal static class ExceptionUtils
    {
        /** NoClassDefFoundError fully-qualified class name which is important during startup phase. */
        private const string ClsNoClsDefFoundErr = "java.lang.NoClassDefFoundError";

        /** NoSuchMethodError fully-qualified class name which is important during startup phase. */
        private const string ClsNoSuchMthdErr = "java.lang.NoSuchMethodError";

        /** InteropCachePartialUpdateException. */
        private const string ClsCachePartialUpdateErr = "org.apache.ignite.internal.processors.platform.cache.PlatformCachePartialUpdateException";

        /** Map with predefined exceptions. */
        private static readonly IDictionary<string, ExceptionFactoryDelegate> Exs = new Dictionary<string, ExceptionFactoryDelegate>();

        /** Exception factory delegate. */
        private delegate Exception ExceptionFactoryDelegate(IIgnite ignite, string msg, Exception innerEx, string javaStackTrace);
        
        /** Inner class regex. */
        private static readonly Regex InnerClassRegex = new Regex(@"class ([^\s]+): (.*)", RegexOptions.Compiled);

        /// <summary>
        /// Static initializer.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline",
            Justification = "Readability")]
        static ExceptionUtils()
        {
            // Common Java exceptions mapped to common .Net exceptions.
            Exs["java.lang.IllegalArgumentException"] = (i, m, e, s) => new ArgumentException(m, e);
            Exs["java.lang.IllegalStateException"] = (i, m, e, s) => new InvalidOperationException(m, e);
            Exs["java.lang.UnsupportedOperationException"] = (i, m, e, s) => new NotImplementedException(m, e);
            Exs["java.lang.InterruptedException"] = (i, m, e, s) => new ThreadInterruptedException(m, e);

            // Generic Ignite exceptions.
            Exs["org.apache.ignite.IgniteException"] = (i, m, e, s) => new IgniteException(m, e, s);
            Exs["org.apache.ignite.IgniteCheckedException"] = (i, m, e, s) => new IgniteException(m, e, s);
            Exs["org.apache.ignite.IgniteClientDisconnectedException"] = (i, m, e, s) => new ClientDisconnectedException(m, e, i.GetCluster().ClientReconnectTask);
            Exs["org.apache.ignite.internal.IgniteClientDisconnectedCheckedException"] = (i, m, e, s) => new ClientDisconnectedException(m, e, i.GetCluster().ClientReconnectTask);

            // Cluster exceptions.
            Exs["org.apache.ignite.cluster.ClusterGroupEmptyException"] = (i, m, e, s) => new ClusterGroupEmptyException(m, s, e);
            Exs["org.apache.ignite.cluster.ClusterTopologyException"] = (i, m, e, s) => new ClusterTopologyException(m, s, e);

            // Compute exceptions.
            Exs["org.apache.ignite.compute.ComputeExecutionRejectedException"] = (i, m, e, s) => new ComputeExecutionRejectedException(m, s, e);
            Exs["org.apache.ignite.compute.ComputeJobFailoverException"] = (i, m, e, s) => new ComputeJobFailoverException(m, s, e);
            Exs["org.apache.ignite.compute.ComputeTaskCancelledException"] = (i, m, e, s) => new ComputeTaskCancelledException(m, e);
            Exs["org.apache.ignite.compute.ComputeTaskTimeoutException"] = (i, m, e, s) => new ComputeTaskTimeoutException(m, e);
            Exs["org.apache.ignite.compute.ComputeUserUndeclaredException"] = (i, m, e, s) => new ComputeUserUndeclaredException(m, s, e);

            // Cache exceptions.
            Exs["javax.cache.CacheException"] = (i, m, e, s) => new CacheException(m, s, e);
            Exs["javax.cache.integration.CacheLoaderException"] = (i, m, e, s) => new CacheStoreException(m, s, e);
            Exs["javax.cache.integration.CacheWriterException"] = (i, m, e, s) => new CacheStoreException(m, s, e);
            Exs["javax.cache.processor.EntryProcessorException"] = (i, m, e, s) => new CacheEntryProcessorException(m, s, e);
            Exs["org.apache.ignite.cache.CacheAtomicUpdateTimeoutException"] = (i, m, e, s) => new CacheAtomicUpdateTimeoutException(m, s, e);

            // Transaction exceptions.
            Exs["org.apache.ignite.transactions.TransactionOptimisticException"] = (i, m, e, s) => new TransactionOptimisticException(m, s, e);
            Exs["org.apache.ignite.transactions.TransactionTimeoutException"] = (i, m, e, s) => new TransactionTimeoutException(m, s, e);
            Exs["org.apache.ignite.transactions.TransactionRollbackException"] = (i, m, e, s) => new TransactionRollbackException(m, s, e);
            Exs["org.apache.ignite.transactions.TransactionHeuristicException"] = (i, m, e, s) => new TransactionHeuristicException(m, s, e);

            // Security exceptions.
            Exs["org.apache.ignite.IgniteAuthenticationException"] = (i, m, e, s) => new SecurityException(m, e);
            Exs["org.apache.ignite.plugin.security.GridSecurityException"] = (i, m, e, s) => new SecurityException(m, e);

            // Future exceptions
            Exs["org.apache.ignite.lang.IgniteFutureCancelledException"] = (i, m, e, s) => new IgniteFutureCancelledException(m, e);
            Exs["org.apache.ignite.internal.IgniteFutureCancelledCheckedException"] = (i, m, e, s) => new IgniteFutureCancelledException(m, e);
        }

        /// <summary>
        /// Creates exception according to native code class and message.
        /// </summary>
        /// <param name="ignite">The ignite.</param>
        /// <param name="clsName">Exception class name.</param>
        /// <param name="msg">Exception message.</param>
        /// <param name="stackTrace">Native stack trace.</param>
        /// <param name="reader">Error data reader.</param>
        /// <returns>Exception.</returns>
        public static Exception GetException(IIgnite ignite, string clsName, string msg, string stackTrace, BinaryReader reader = null)
        {
            ExceptionFactoryDelegate ctor;

            if (Exs.TryGetValue(clsName, out ctor))
            {
                var match = InnerClassRegex.Match(msg);

                ExceptionFactoryDelegate innerCtor;

                if (match.Success && Exs.TryGetValue(match.Groups[1].Value, out innerCtor))
                    return ctor(ignite, msg, innerCtor(ignite, match.Groups[2].Value, null, stackTrace), stackTrace);

                return ctor(ignite, msg, null, stackTrace);
            }

            if (ClsNoClsDefFoundErr.Equals(clsName, StringComparison.OrdinalIgnoreCase))
                return new IgniteException("Java class is not found (did you set IGNITE_HOME environment " +
                    "variable?): " + msg);

            if (ClsNoSuchMthdErr.Equals(clsName, StringComparison.OrdinalIgnoreCase))
                return new IgniteException("Java class method is not found (did you set IGNITE_HOME environment " +
                    "variable?): " + msg);

            if (ClsCachePartialUpdateErr.Equals(clsName, StringComparison.OrdinalIgnoreCase))
                return ProcessCachePartialUpdateException(ignite, msg, stackTrace, reader);

            return
                new IgniteException(string.Format("Java exception occurred [class={0}, message={1}, stack tace={2}]",
                    clsName, msg, stackTrace));
        }

        /// <summary>
        /// Process cache partial update exception.
        /// </summary>
        /// <param name="ignite">The ignite.</param>
        /// <param name="msg">Message.</param>
        /// <param name="stackTrace">Stack trace.</param>
        /// <param name="reader">Reader.</param>
        /// <returns>CachePartialUpdateException.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static Exception ProcessCachePartialUpdateException(IIgnite ignite, string msg, string stackTrace, BinaryReader reader)
        {
            if (reader == null)
                return new CachePartialUpdateException(msg, new IgniteException("Failed keys are not available."));

            bool dataExists = reader.ReadBoolean();

            Debug.Assert(dataExists);

            if (reader.ReadBoolean())
            {
                bool keepBinary = reader.ReadBoolean();

                BinaryReader keysReader = reader.Marshaller.StartUnmarshal(reader.Stream, keepBinary);

                try
                {
                    return new CachePartialUpdateException(msg, ReadNullableList(keysReader));
                }
                catch (Exception e)
                {
                    // Failed to deserialize data.
                    return new CachePartialUpdateException(msg, e);
                }
            }

            // Was not able to write keys.
            string innerErrCls = reader.ReadString();
            string innerErrMsg = reader.ReadString();

            Exception innerErr = GetException(ignite, innerErrCls, innerErrMsg, stackTrace);

            return new CachePartialUpdateException(msg, innerErr);
        }

        /// <summary>
        /// Create JVM initialization exception.
        /// </summary>
        /// <param name="clsName">Class name.</param>
        /// <param name="msg">Message.</param>
        /// <param name="stackTrace">Stack trace.</param>
        /// <returns>Exception.</returns>
        public static Exception GetJvmInitializeException(string clsName, string msg, string stackTrace)
        {
            if (clsName != null)
                return new IgniteException("Failed to initialize JVM.", GetException(null, clsName, msg, stackTrace));

            if (msg != null)
                return new IgniteException("Failed to initialize JVM: " + msg + "\n" + stackTrace);

            return new IgniteException("Failed to initialize JVM.");
        }

        /// <summary>
        /// Reads nullable list.
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <returns>List.</returns>
        private static List<object> ReadNullableList(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;

            var size = reader.ReadInt();

            var list = new List<object>(size);

            for (int i = 0; i < size; i++)
                list.Add(reader.ReadObject<object>());

            return list;
        }
    }
}
