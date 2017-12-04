namespace Apache.Ignite.Core.Tests.ApiParity
{
    using System.Collections.Generic;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Datastream;
    using NUnit.Framework;

    /// <summary>
    /// Tests that <see cref="IBinary"/> has all APIs from Java Ignite interface.
    /// </summary>
    public class BinaryParityTest
    {
        /** Known name mappings. */
        private static readonly Dictionary<string, string> KnownMappings = new Dictionary<string, string>
        {
            {"type", "GetBinaryType"},
            {"types", "GetBinaryTypes"}
        };

        /// <summary>
        /// Tests the API parity.
        /// </summary>
        [Test]
        public void TestBinary()
        {
            ParityTest.CheckInterfaceParity(
                @"modules\core\src\main\java\org\apache\ignite\IgniteBinary.java",
                typeof(IBinary),
                knownMappings: KnownMappings);
        }
    }
}