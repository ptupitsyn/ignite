namespace Apache.Ignite.BenchmarkDotNet
{
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Client;
    using global::BenchmarkDotNet.Attributes;

    public class EnumSerPerf
    {
        /** */
        public IIgnite Ignite { get; set; }
        
        public ICache<int, object> Cache { get; set; }

        /// <summary>
        /// Sets up the benchmark.
        /// </summary>
        [GlobalSetup]
        public virtual void GlobalSetup()
        {
            Ignite = Ignition.Start(Utils.GetIgniteConfiguration());
            Cache = Ignite.GetOrCreateCache<int, object>("c");
        }

        /// <summary>
        /// Cleans up the benchmark.
        /// </summary>
        [GlobalCleanup]
        public virtual void GlobalCleanup()
        {
            Ignite.Dispose();
        }
        
        [Benchmark]
        public void PutFoo()
        {
            Cache.Put(1, new Foo());
        }
        
        [Benchmark]
        public void PutFooWithEnum()
        {
            Cache.Put(1, new FooWithEnum());
        }

        private class Foo : IBinarizable
        {
            public string Bar { get; set; }
            
            public void WriteBinary(IBinaryWriter writer)
            {
                writer.WriteString("bar", Bar);
            }

            public void ReadBinary(IBinaryReader reader)
            {
                throw new System.NotImplementedException();
            }
        }
        
        private class FooWithEnum : IBinarizable
        {
            public string Bar { get; set; }
            
            public MyEnum MyEnum { get; set; }
            
            public void WriteBinary(IBinaryWriter writer)
            {
                writer.WriteString("bar", Bar);
                writer.WriteEnum("enum", MyEnum);
            }

            public void ReadBinary(IBinaryReader reader)
            {
                throw new System.NotImplementedException();
            }
        }

        private enum MyEnum
        {
            Undefined,
            Stock,
            Option,
            Future,
            Bond,
            Strategy,
            DNTP,
            Forex,
            ForexFW,
            Repo,
            CFD,
            TAPO,
            CDS,
            Swap,
            Forward
        }
    }
}