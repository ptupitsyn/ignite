namespace Apache.Ignite.Core.Tests.Client
{
    using System.Threading.Tasks;
    using Apache.Ignite.Core.Client;
    using NUnit.Framework;

    public class ThinClientProtocolSanityTest
    {
        [Test]
        public async Task Test1()
        {
            Assert.AreEqual("x", "y");
            using var client = Ignition.StartClient(new IgniteClientConfiguration("127.0.0.1:10800"));

        }
    }
}