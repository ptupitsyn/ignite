namespace Apache.Ignite.BenchmarkDotNet.Serialization
{
    using Apache.Ignite.BenchmarkDotNet.Models;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary;
    using global::BenchmarkDotNet.Attributes;

    /// <summary>
    /// Serialization benchmark.
    ///
    /// With TypeCaster (.NET Core 3.1):
    /// </summary>
    [MemoryDiagnoser]
    public class BasicPropertyTypesWriteBenchmark
    {
        private Marshaller _marsh;

        private readonly BasicPropertyTypes _obj = new BasicPropertyTypes();

        private readonly BasicPropertyTypesBinarizable _objBinarizable = new BasicPropertyTypesBinarizable();

        [GlobalSetup]
        public void GlobalSetup()
        {
            _marsh = new Marshaller(new BinaryConfiguration(
                typeof (BasicPropertyTypes),
                typeof (BasicPropertyTypesBinarizable)));
        }

        [Benchmark]
        public void Write()
        {
            _marsh.Marshal(_obj);
        }

        [Benchmark]
        public void WriteBinarizable()
        {
            _marsh.Marshal(_objBinarizable);
        }
    }
}
