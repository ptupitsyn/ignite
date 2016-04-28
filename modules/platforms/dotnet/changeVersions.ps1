# Number of days since 4/28/2016
$buildNum = ([System.DateTime]::Now - [System.DateTime]::FromBinary(-8587399024854775808)).Days

echo "Updating versions to $buildnum-nightly"

Get-ChildItem -Filter AssemblyInfo.cs -Recurse `
    | ForEach-Object {
        $file = $_.FullName
        echo "Updating $file..."
        (Get-Content $file) `
            -replace 'AssemblyVersion\("(\d+\.\d+\.\d+).*?"\)', ('AssemblyVersion("$1.' + $buildNum + '")') `
            -replace 'AssemblyFileVersion\("(\d+\.\d+\.\d+).*?"\)', ('AssemblyFileVersion("$1.' + $buildNum + '")') `
            -replace 'AssemblyInformationalVersion\("(\d+\.\d+\.\d+).*?"\)', ('AssemblyInformationalVersion("$1.' + $buildNum + '-nightly")') `
            | Out-File $file -Encoding utf8
      }