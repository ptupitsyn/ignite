using System;

namespace Apache.Ignite.Core.Impl.Unmanaged.Jni
{
    using System.Runtime.InteropServices;
    using System.Security;

    [SuppressUnmanagedCodeSecurity]
    internal class Delegates
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate JNIResult CallStaticVoidMethodDelegate(
            IntPtr thiz, IntPtr clazz, IntPtr methodIdJavaPtr, params Value[] args);


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr NewGlobalRef(IntPtr thiz, IntPtr lobj);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void DeleteLocalRef(IntPtr thiz, IntPtr lref);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [SuppressUnmanagedCodeSecurity]
        internal delegate void DeleteGlobalRef(IntPtr thiz, IntPtr gref);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr FindClass(IntPtr thiz, [MarshalAs(UnmanagedType.LPStr)] string name);
    }
}
