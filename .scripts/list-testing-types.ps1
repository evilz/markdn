param([string]$packageRoot = "$env:USERPROFILE\.nuget\packages\microsoft.codeanalysis.csharp.sourcegenerators.testing.xunit\1.1.0\lib")

Write-Output "Searching package lib folder: $packageRoot"

if (-not (Test-Path $packageRoot)) {
    Write-Error "Package lib folder not found: $packageRoot"
    exit 1
}

$dlls = Get-ChildItem -Path $packageRoot -Recurse -Filter Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit.dll -ErrorAction SilentlyContinue

if ($dlls.Count -eq 0) {
    Write-Error "No matching DLLs found under $packageRoot"
    exit 1
}

foreach ($dllInfo in $dlls) {
    $path = $dllInfo.FullName
    $folder = $dllInfo.DirectoryName
    Write-Output "\n--- Trying assembly: $path ---"
    try {
        # Preload known dependency DLLs from the global NuGet cache first
        $globalRoot = Join-Path $env:USERPROFILE '.nuget\packages'
        $depNames = @(
            "Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.dll",
            "Microsoft.CodeAnalysis.SourceGenerators.Testing.dll",
            "Microsoft.CodeAnalysis.Testing.dll",
            "Microsoft.CodeAnalysis.Testing.Verifiers.XUnit.dll",
            "Microsoft.CodeAnalysis.Testing.Verifiers.dll",
            "Microsoft.CodeAnalysis.Analyzer.Testing.dll"
        )
        foreach ($dep in $depNames) {
            Get-ChildItem -Path $globalRoot -Recurse -Filter $dep -ErrorAction SilentlyContinue | ForEach-Object {
                try { [Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null } catch { }
            }
        }

        # Preload all DLLs in the same folder to satisfy additional dependencies
        Get-ChildItem -Path $folder -Filter *.dll -ErrorAction SilentlyContinue | ForEach-Object {
            try { [Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null } catch { }
        }

        $asm = [Reflection.Assembly]::LoadFrom($path)
        $asm.GetExportedTypes() | Sort-Object FullName | ForEach-Object { Write-Output $_.FullName }
    }
    catch {
        Write-Error "Failed to load $path : $_"
    }
}

Write-Output "\n--- Also inspect Microsoft.CodeAnalysis.SourceGenerators.Testing.dll candidates ---"
Get-ChildItem -Path (Join-Path $env:USERPROFILE '.nuget\packages') -Recurse -Filter Microsoft.CodeAnalysis.SourceGenerators.Testing.dll -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Output "\n--- Trying: $($_.FullName) ---"
    try {
        $asm = [Reflection.Assembly]::LoadFrom($_.FullName)
        $asm.GetExportedTypes() | Sort-Object FullName | ForEach-Object { Write-Output $_.FullName }
    }
    catch {
        Write-Error "Failed to load $_.FullName : $_"
    }
}
