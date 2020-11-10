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
    /// |           Method |     Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    /// |----------------- |---------:|----------:|----------:|-------:|------:|------:|----------:|
    /// |            Write | 2.163 us | 0.0129 us | 0.0115 us | 0.2861 |     - |     - |   1.77 KB |
    /// | WriteBinarizable | 1.838 us | 0.0111 us | 0.0098 us | 0.2823 |     - |     - |   1.73 KB |
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
