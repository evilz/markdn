$root = Join-Path $env:USERPROFILE '.nuget\packages'
Write-Output "Searching $root for *SourceGenerators.Testing*.dll"
Get-ChildItem -Path $root -Recurse -Filter *SourceGenerators.Testing*.dll -ErrorAction SilentlyContinue | ForEach-Object { Write-Output $_.FullName }
Get-ChildItem -Path $root -Recurse -Filter *Microsoft.CodeAnalysis.Testing*.dll -ErrorAction SilentlyContinue | ForEach-Object { Write-Output $_.FullName }
Get-ChildItem -Path $root -Recurse -Filter *Analyzer.Testing*.dll -ErrorAction SilentlyContinue | ForEach-Object { Write-Output $_.FullName }
