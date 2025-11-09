# Performance Benchmark Script for Markdn API
# Tests Success Criteria: SC-001, SC-007, SC-008

Write-Host "Starting Markdn API Performance Benchmarks..." -ForegroundColor Cyan
Write-Host ""

# Start the API in background
Write-Host "Starting API server..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Markdn.Api/Markdn.Api.csproj --no-build --urls http://localhost:5123" -PassThru -WindowStyle Hidden
Write-Host "Waiting for API to start (15 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 15  # Wait for API to start

try {
    $baseUrl = "http://localhost:5123"
    
    Write-Host "Testing API health..." -ForegroundColor Yellow
    $healthCheck = Invoke-WebRequest -Uri "$baseUrl/api/health" -UseBasicParsing
    if ($healthCheck.StatusCode -ne 200) {
        Write-Host "API health check failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ API is running" -ForegroundColor Green
    Write-Host ""

    # T157: Benchmark GET /content/{slug} with 1MB file (Target: <100ms per SC-001)
    Write-Host "T157: Benchmarking GET /content/{slug} response time (SC-001: <100ms)" -ForegroundColor Cyan
    $slugTimes = @()
    for ($i = 1; $i -le 10; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri "$baseUrl/api/content/test-post-1" -UseBasicParsing
        $sw.Stop()
        $slugTimes += $sw.ElapsedMilliseconds
    }
    $avgSlugTime = ($slugTimes | Measure-Object -Average).Average
    Write-Host "  Average response time: $([math]::Round($avgSlugTime, 2))ms" -ForegroundColor $(if ($avgSlugTime -lt 100) { "Green" } else { "Yellow" })
    Write-Host "  Min: $($slugTimes | Measure-Object -Minimum | Select-Object -ExpandProperty Minimum)ms, Max: $($slugTimes | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum)ms"
    if ($avgSlugTime -lt 100) {
        Write-Host "  ✓ PASS: Meets SC-001 (<100ms)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ WARNING: Exceeds SC-001 target" -ForegroundColor Yellow
    }
    Write-Host ""

    # T156: Benchmark GET /content with 100 files (Target: <200ms per SC-007)
    Write-Host "T156: Benchmarking GET /content response time (SC-007: <200ms)" -ForegroundColor Cyan
    $listTimes = @()
    for ($i = 1; $i -le 10; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri "$baseUrl/api/content?page=1&pageSize=20" -UseBasicParsing
        $sw.Stop()
        $listTimes += $sw.ElapsedMilliseconds
    }
    $avgListTime = ($listTimes | Measure-Object -Average).Average
    Write-Host "  Average response time: $([math]::Round($avgListTime, 2))ms" -ForegroundColor $(if ($avgListTime -lt 200) { "Green" } else { "Yellow" })
    Write-Host "  Min: $($listTimes | Measure-Object -Minimum | Select-Object -ExpandProperty Minimum)ms, Max: $($listTimes | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum)ms"
    if ($avgListTime -lt 200) {
        Write-Host "  ✓ PASS: Meets SC-007 (<200ms)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ WARNING: Exceeds SC-007 target" -ForegroundColor Yellow
    }
    Write-Host ""

    # T159: Benchmark filtered queries
    Write-Host "T159: Benchmarking filtered query performance" -ForegroundColor Cyan
    $filterTimes = @()
    for ($i = 1; $i -le 10; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri "$baseUrl/api/content?tag=tutorial&category=blog&sortBy=date&sortOrder=desc" -UseBasicParsing
        $sw.Stop()
        $filterTimes += $sw.ElapsedMilliseconds
    }
    $avgFilterTime = ($filterTimes | Measure-Object -Average).Average
    Write-Host "  Average response time: $([math]::Round($avgFilterTime, 2))ms" -ForegroundColor $(if ($avgFilterTime -lt 200) { "Green" } else { "Yellow" })
    Write-Host "  Min: $($filterTimes | Measure-Object -Minimum | Select-Object -ExpandProperty Minimum)ms, Max: $($filterTimes | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum)ms"
    Write-Host ""

    # T160: Cache performance test
    Write-Host "T160: Testing cache effectiveness" -ForegroundColor Cyan
    Write-Host "  First request (cold cache):" -ForegroundColor Gray
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $response1 = Invoke-WebRequest -Uri "$baseUrl/api/content/test-post-50" -UseBasicParsing
    $sw.Stop()
    $coldTime = $sw.ElapsedMilliseconds
    Write-Host "    Time: ${coldTime}ms" -ForegroundColor Gray

    Write-Host "  Second request (warm cache):" -ForegroundColor Gray
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $response2 = Invoke-WebRequest -Uri "$baseUrl/api/content/test-post-50" -UseBasicParsing
    $sw.Stop()
    $warmTime = $sw.ElapsedMilliseconds
    Write-Host "    Time: ${warmTime}ms" -ForegroundColor Gray

    $improvement = [math]::Round((($coldTime - $warmTime) / $coldTime) * 100, 1)
    Write-Host "  Cache improvement: $improvement%" -ForegroundColor $(if ($warmTime -lt $coldTime) { "Green" } else { "Yellow" })
    Write-Host ""

    # Summary
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host "BENCHMARK SUMMARY" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host "SC-001 (GET /content/{slug}):  $([math]::Round($avgSlugTime, 2))ms $(if ($avgSlugTime -lt 100) { '✓ PASS' } else { '⚠ FAIL' })" -ForegroundColor $(if ($avgSlugTime -lt 100) { "Green" } else { "Yellow" })
    Write-Host "SC-007 (GET /content):          $([math]::Round($avgListTime, 2))ms $(if ($avgListTime -lt 200) { '✓ PASS' } else { '⚠ FAIL' })" -ForegroundColor $(if ($avgListTime -lt 200) { "Green" } else { "Yellow" })
    Write-Host "Filtered queries:               $([math]::Round($avgFilterTime, 2))ms" -ForegroundColor White
    Write-Host "Cache effectiveness:            $improvement% improvement" -ForegroundColor White
    Write-Host ""

} finally {
    # Stop the API
    Write-Host "Stopping API server..." -ForegroundColor Yellow
    Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    Write-Host "Done!" -ForegroundColor Green
}
