$num = 13

[System.IO.Directory]::GetFiles('C:\W\incubator-ignite\modules\platforms\dotnet', "AssemblyInfo.cs", [System.IO.SearchOption]::AllDirectories) `
    | ForEach-Object {
        (Get-Content $_) -replace 'AssemblyVersion\("(\d+\.\d+\.\d+).*?"\)', 'AssemblyVersion("$1")' | Out-File $_ -Encoding utf8
      }