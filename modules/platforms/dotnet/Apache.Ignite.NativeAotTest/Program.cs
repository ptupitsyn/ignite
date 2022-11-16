using Apache.Ignite.Core;
using Apache.Ignite.Core.Client;

Environment.SetEnvironmentVariable("IGNITE_NATIVE_TEST_CLASSPATH", "true");

var igniteConfiguration = new IgniteConfiguration
{
    IgniteHome = "/home/pavel/w/ignite"
};
using var server = Ignition.Start(igniteConfiguration);

var cfg = new IgniteClientConfiguration("127.0.0.1:10800");
using var client = Ignition.StartClient(cfg);

var cacheNames = client.GetCacheNames();

Console.WriteLine("Caches: " + cacheNames.Count);

var cache = client.GetOrCreateCache<int, string>("c");
cache[1] = "Hello, world!";

var res = cache.Get(1);

Console.WriteLine("result from cache: " + res);
