[System.IO.Directory]::GetFiles('C:\W\incubator-ignite\modules\platforms\dotnet', "AssemblyInfo.cs", [System.IO.SearchOption]::AllDirectories) `
    | ForEach-Object {
        (Get-Content $_) -replace '1', '2' | Out-File $_ -Encoding utf8
      }