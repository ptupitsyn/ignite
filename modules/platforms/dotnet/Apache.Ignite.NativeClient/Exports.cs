namespace Apache.Ignite.NativeClient;

using System.Runtime.InteropServices;

public static class Exports
{
    [UnmanagedCallersOnly(EntryPoint = "AddOne")]
    public static int AddOne(int i) => i + 1;
}
