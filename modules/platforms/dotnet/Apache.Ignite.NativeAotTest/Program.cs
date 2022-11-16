using Apache.Ignite.Core;
using Apache.Ignite.Core.Client;

var cfg = new IgniteClientConfiguration("127.0.0.1:10800");
using var client = Ignition.StartClient(cfg);

var cacheNames = client.GetCacheNames();

Console.WriteLine("Caches: " + cacheNames.Count);
