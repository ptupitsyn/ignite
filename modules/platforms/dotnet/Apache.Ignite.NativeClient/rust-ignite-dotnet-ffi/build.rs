fn main() {
    // LD_LIBRARY_PATH=/home/pavel/w/ignite/modules/platforms/dotnet/Apache.Ignite.NativeClient/bin/Release/net7.0/linux-x64/publish
    // Apache.Ignite.NativeClient.so
    println!("cargo:rustc-link-search=native=/home/pavel/w/ignite/modules/platforms/dotnet/Apache.Ignite.NativeClient/bin/Release/net7.0/linux-x64/publish");
    println!("cargo:rustc-link-lib=ignite");
}