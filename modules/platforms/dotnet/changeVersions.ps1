# Number of days since 4/28/2016
$buildNum = ([System.DateTime]::Now - [System.DateTime]::FromBinary(-8587399024854775808)).Days

[System.IO.Directory]::GetFiles('C:\W\incubator-ignite\modules\platforms\dotnet', "AssemblyInfo.cs", [System.IO.SearchOption]::AllDirectories) `
    | ForEach-Object {
        (Get-Content $_) `
            -replace 'AssemblyVersion\("(\d+\.\d+\.\d+).*?"\)', ('AssemblyVersion("$1.' + $num + '")') `
            -replace 'AssemblyFileVersion\("(\d+\.\d+\.\d+).*?"\)', ('AssemblyFileVersion("$1.' + $num + '")') `
            -replace 'AssemblyInformationalVersion\("(\d+\.\d+\.\d+).*?"\)', ('AssemblyInformationalVersion("$1.' + $num + '-nightly")') `
            | Out-File $_ -Encoding utf8
      }