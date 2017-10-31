using System;

namespace Apache.Ignite.Core.Impl.Unmanaged.Jni
{
    using System.Runtime.InteropServices;
    using System.Security;

    [SuppressUnmanagedCodeSecurity]
    internal class Delegates  // TODO: remove prefix from all Jni* classes
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate JNIResult CallStaticVoidMethod(
            IntPtr env, IntPtr clazz, IntPtr methodIdJavaPtr, params JavaValue[] args);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate JNIResult CallStaticVoidMethodV(
            IntPtr env, IntPtr clazz, IntPtr methodIdJavaPtr, IntPtr vaList);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr NewGlobalRef(IntPtr env, IntPtr lobj);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void DeleteLocalRef(IntPtr env, IntPtr lref);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [SuppressUnmanagedCodeSecurity]
        internal delegate void DeleteGlobalRef(IntPtr env, IntPtr gref);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr FindClass(IntPtr env, [MarshalAs(UnmanagedType.LPStr)] string name);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr GetStaticMethodID(IntPtr env, IntPtr clazz, 
            [MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string sig);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr GetMethodID(IntPtr env, IntPtr clazz, [MarshalAs(UnmanagedType.LPStr)] string name,
            [MarshalAs(UnmanagedType.LPStr)] string sig);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr NewStringUTF(IntPtr env, IntPtr utf);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr ExceptionOccurred(IntPtr env);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr GetObjectClass(IntPtr env, IntPtr obj);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr CallObjectMethod(
            IntPtr env, IntPtr obj, IntPtr methodIdJavaPtr, params JavaValue[] args);

    }
}
