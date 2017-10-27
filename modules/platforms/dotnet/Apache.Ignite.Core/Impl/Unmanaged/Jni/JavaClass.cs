namespace Apache.Ignite.Core.Impl.Unmanaged.Jni
{
    internal class JavaClass
    {
        private readonly JniGlobalHandle _handle;

        public JavaClass(JniGlobalHandle handle)
        {
            _handle = handle;
        }
    }
}
