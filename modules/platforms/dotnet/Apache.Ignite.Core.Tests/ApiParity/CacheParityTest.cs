namespace Apache.Ignite.Core.Tests.ApiParity
{
    using System.Collections.Generic;
    using Apache.Ignite.Core.Cache;
    using NUnit.Framework;

    /// <summary>
    /// Tests that <see cref="ICache{TK,TV}"/> has all APIs from Java Ignite interface.
    /// </summary>
    public class CacheParityTest
    {
        /** Methods that are not needed on .NET side. */
        private static readonly string[] UnneededMethods =
        {
        };

        /** Members that are missing on .NET side and should be added in future. */
        private static readonly string[] MissingProperties =
        {
        };

        /** Known name mappings. */
        private static readonly Dictionary<string, string> KnownMappings = new Dictionary<string, string>
        {
        };

        /// <summary>
        /// Tests the API parity.
        /// </summary>
        [Test]
        public void TestCache()
        {
            ParityTest.CheckInterfaceParity(
                @"modules\core\src\main\java\org\apache\ignite\IgniteCache.java",
                typeof(ICache<,>),
                UnneededMethods,
                MissingProperties,
                KnownMappings);
        }
    }
}