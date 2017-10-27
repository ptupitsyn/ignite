namespace Apache.Ignite.Core.Impl.Unmanaged.Jni
{
    using System;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;

    internal sealed class JniGlobalHandle : SafeHandle
    {
        public static bool IsNull(JniGlobalHandle handle)
        {
            return handle == null || handle.IsInvalid;
        }

        private readonly JavaVM _javaVm;

        public JavaVM JavaVm
        {
            get { return _javaVm; }
        }

        private static readonly JniGlobalHandle zero = new JniGlobalHandle(IntPtr.Zero, null);

        public static JniGlobalHandle Zero
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get { return zero; }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public JniGlobalHandle(IntPtr handleValue, JavaVM javaVM)
            : base(IntPtr.Zero, true)
        {
            _javaVm = javaVM;
            SetHandle(handleValue);
        }

        public override bool IsInvalid
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get { return handle == IntPtr.Zero; }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected override bool ReleaseHandle()
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    // TODO
                    //JNIEnv env = JNIEnv.GetEnvNoThrow(javaVM);
                    //if (env == null)
                    //{
                    //    return false;
                    //}
                    //env.DeleteGlobalRef(this);
                }
                return true;
            }
            finally
            {
                handle = IntPtr.Zero;
            }
        }
    }
}