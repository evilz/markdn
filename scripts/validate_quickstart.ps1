param()

$project = "src/Markdn.Blazor.App/Markdn.Blazor.App.csproj"
Write-Host "Building $project..."
dotnet build $project --nologo

# Search for generated .md.g.cs files
$objDir = "src/Markdn.Blazor.App/obj"
$generated = Get-ChildItem -Path $objDir -Recurse -Filter "*.md.g.cs" -ErrorAction SilentlyContinue
if ($null -eq $generated) {
    Write-Host "Validation FAILED: no generated .md.g.cs files found under $objDir"
    exit 1
}
else {
    Write-Host "Validation PASSED: found $($generated.Count) generated .md.g.cs files"
    $generated | Select-Object -First 10 | ForEach-Object { Write-Host $_.FullName }
}

# Optionally, run the quickstart tasks checklist (lightweight): ensure tasks.md references Quickstart
$tasks = Get-Content -Path "specs/003-blazor-markdown-components/quickstart.md" -ErrorAction SilentlyContinue
if ($tasks -and ($tasks -join "`n") -match "componentNamespaces") {
    Write-Host "Quickstart.md contains componentNamespaces guidance"
}

Write-Host "Quickstart validation complete."
