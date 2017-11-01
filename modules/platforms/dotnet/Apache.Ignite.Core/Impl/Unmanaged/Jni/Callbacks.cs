using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apache.Ignite.Core.Impl.Unmanaged.Jni
{
    using System.Runtime.InteropServices;
    using Apache.Ignite.Core.Impl.Binary;

    internal class Callbacks
    {
        /// <summary>
        /// Registers native callbacks.
        /// </summary>
        private void RegisterNatives(Env env)
        {
            // Native callbacks are per-jvm.
            // Every callback (except ConsoleWrite) includes envPtr (third arg) to identify Ignite instance.

            using (var callbackUtils = env.FindClass(
                "org/apache/ignite/internal/processors/platform/callback/PlatformCallbackUtils"))
            {
                // Any signature works, but wrong one will cause segfault eventually.
                var methods = new[]
                {
                    GetNativeMethod("loggerLog", "(JILjava/lang/String;Ljava/lang/String;Ljava/lang/String;J)V",
                        (CallbackDelegates.LoggerLog) LoggerLog),

                    GetNativeMethod("loggerIsLevelEnabled", "(JI)Z",
                        (CallbackDelegates.LoggerIsLevelEnabled) LoggerIsLevelEnabled),

                    GetNativeMethod("consoleWrite", "(Ljava/lang/String;Z)V",
                        (CallbackDelegates.ConsoleWrite) ConsoleWrite),

                    GetNativeMethod("inLongOutLong", "(JIJ)J", (Action) (() => { Console.WriteLine("woot"); })),

                    GetNativeMethod("inLongLongLongObjectOutLong", "(JIJJJLjava/lang/Object;)J",
                        (CallbackDelegates.InLongLongLongObjectOutLong) InLongLongLongObjectOutLong)
                };

                try
                {
                    env.RegisterNatives(callbackUtils, methods);
                }
                finally
                {
                    foreach (var nativeMethod in methods)
                    {
                        Marshal.FreeHGlobal(nativeMethod.Name);
                        Marshal.FreeHGlobal(nativeMethod.Signature);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the native method.
        /// </summary>
        private static unsafe NativeMethod GetNativeMethod(string name, string sig, Delegate d)
        {
            return new NativeMethod
            {
                Name = (IntPtr)IgniteUtils.StringToUtf8Unmanaged(name),
                Signature = (IntPtr)IgniteUtils.StringToUtf8Unmanaged(sig),
                FuncPtr = Marshal.GetFunctionPointerForDelegate(d)
            };
        }


        private void LoggerLog(IntPtr env, IntPtr clazz, int level, IntPtr message, IntPtr category, IntPtr error,
            long memPtr)
        {

        }

        private void ConsoleWrite(IntPtr envPtr, IntPtr clazz, IntPtr message, bool isError)
        {
            // TODO: This causes crash some times (probably unreleased stuff or incorrect env handling)
            var env = Jvm.Get().AttachCurrentThread();
            var msg = env.JStringToString(message);

            Console.Write(msg);
        }

        private bool LoggerIsLevelEnabled(IntPtr env, IntPtr clazz, int level)
        {
            return false;
        }

        private long InLongLongLongObjectOutLong(IntPtr env, IntPtr clazz, long igniteId,
            int op, long arg1, long arg2, long arg3, IntPtr arg)
        {
            if (op == (int)UnmanagedCallbackOp.ExtensionInLongLongOutLong && arg1 == 1)
            {
                Console.WriteLine("OpPrepareDotNet");
                using (var inStream = IgniteManager.Memory.Get(arg2).GetStream())
                using (var outStream = IgniteManager.Memory.Get(arg3).GetStream())
                {
                    var writer = BinaryUtils.Marshaller.StartMarshal(outStream);

                    // Config.
                    // TODO
                    //TestUtils.GetTestConfiguration().Write(writer);

                    // Beans.
                    writer.WriteInt(0);

                    outStream.SynchronizeOutput();

                    return 0;
                }

            }
            else
            {
                Console.WriteLine("UNKNOWN CALLBACK: " + op);
            }

            return 0;
        }
    }
}
