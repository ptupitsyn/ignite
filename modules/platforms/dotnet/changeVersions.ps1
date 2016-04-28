# Number of days since 4/28/2016
$buildNum = ([System.DateTime]::Now - [System.DateTime]::FromBinary(-8587399024854775808)).Days

Get-ChildItem -Filter AssemblyInfo.cs -Recurse `
    | ForEach-Object {
        (Get-Content $_.FullName) `
            -replace 'AssemblyVersion\("(\d+\.\d+\.\d+).*?"\)', ('AssemblyVersion("$1.' + $buildNum + '")') `
            -replace 'AssemblyFileVersion\("(\d+\.\d+\.\d+).*?"\)', ('AssemblyFileVersion("$1.' + $buildNum + '")') `
            -replace 'AssemblyInformationalVersion\("(\d+\.\d+\.\d+).*?"\)', ('AssemblyInformationalVersion("$1.' + $buildNum + '-nightly")') `
            | Out-File $_ -Encoding utf8
      }