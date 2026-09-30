namespace Apache.Ignite.Core.Tests.Client
{
    using Apache.Ignite.Core.Client;
    using NUnit.Framework;

    public class ClientProtocolSanityTest
    {
        private readonly IgniteClientConfiguration _igniteClientConfiguration = new IgniteClientConfiguration("127.0.0.1:10800");

        [Test]
        public void TestConnect()
        {
            var client = Ignition.StartClient(_igniteClientConfiguration);
            client.GetCacheNames();
        }
    }
}