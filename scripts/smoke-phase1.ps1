#Requires -Version 7.0
<#
.SYNOPSIS
  Phase 1 API smoke checks for NoticeSaaS (auth, dashboard, clients, sync, usage).

.NOTES
  Start the API first (dotnet run or Docker :8080).
  Override base URL with $env:NOTICE_API_BASE if needed.
#>
param(
    [string] $ApiBase = $(if ($env:NOTICE_API_BASE) { $env:NOTICE_API_BASE } else { "http://localhost:5166" })
)

$ErrorActionPreference = "Stop"
$failed = 0

function Assert-True([bool] $condition, [string] $message) {
    if ($condition) {
        Write-Host "  PASS  $message" -ForegroundColor Green
    }
    else {
        Write-Host "  FAIL  $message" -ForegroundColor Red
        $script:failed++
    }
}

Write-Host "NoticeSaaS Phase 1 smoke against $ApiBase" -ForegroundColor Cyan

# 1. Health
try {
    $health = Invoke-WebRequest -Uri "$ApiBase/health" -UseBasicParsing
    Assert-True ($health.StatusCode -eq 200) "GET /health returns 200"
}
catch {
    Assert-True $false "GET /health reachable ($($_.Exception.Message))"
    Write-Host "API does not appear to be running. Aborting." -ForegroundColor Yellow
    exit 1
}

# 2. Login
$loginBody = @{
    email = "admin@noticesaas.local"
    password = "Admin@12345"
    forceLogout = $true
} | ConvertTo-Json

try {
    $login = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/login" `
        -ContentType "application/json" -Body $loginBody
    Assert-True (-not [string]::IsNullOrWhiteSpace($login.accessToken)) "POST /api/auth/login returns accessToken"
    $token = $login.accessToken
}
catch {
    Assert-True $false "POST /api/auth/login ($($_.Exception.Message))"
    exit 1
}

$headers = @{ Authorization = "Bearer $token" }

# 3. Session / me
try {
    $session = Invoke-RestMethod -Uri "$ApiBase/api/auth/session" -Headers $headers
    Assert-True ($null -ne $session) "GET /api/auth/session works"
}
catch {
    Assert-True $false "GET /api/auth/session ($($_.Exception.Message))"
}

# 4. Dashboard
try {
    $dash = Invoke-RestMethod -Uri "$ApiBase/api/v1/dashboard/summary?module=IncomeTax&period=Monthly" -Headers $headers
    Assert-True ($null -ne $dash.tasks) "GET /api/v1/dashboard/summary has tasks buckets"
}
catch {
    Assert-True $false "GET /api/v1/dashboard/summary ($($_.Exception.Message))"
}

# 5. Usage
try {
    $usage = Invoke-RestMethod -Uri "$ApiBase/api/v1/usage" -Headers $headers
    Assert-True ($usage.assesseeLimit -gt 0) "GET /api/v1/usage has assesseeLimit"
    Assert-True ($usage.syncCreditLimit -gt 0) "GET /api/v1/usage has syncCreditLimit"
}
catch {
    Assert-True $false "GET /api/v1/usage ($($_.Exception.Message))"
}

# 6. Team + Master
try {
    $team = Invoke-RestMethod -Uri "$ApiBase/api/v1/team" -Headers $headers
    Assert-True ($team.total -ge 1) "GET /api/v1/team returns members"
}
catch {
    Assert-True $false "GET /api/v1/team ($($_.Exception.Message))"
}

try {
    $deps = Invoke-RestMethod -Uri "$ApiBase/api/v1/master/departments" -Headers $headers
    Assert-True (@($deps).Count -ge 4) "GET /api/v1/master/departments includes seeded depts"
}
catch {
    Assert-True $false "GET /api/v1/master/departments ($($_.Exception.Message))"
}

# 7. Clients list + sync demo client
try {
    $clients = Invoke-RestMethod -Uri "$ApiBase/api/v1/clients?module=IncomeTax" -Headers $headers
    $demo = @($clients) | Where-Object { $_.pan -eq "AABCM1234F" } | Select-Object -First 1
    Assert-True ($null -ne $demo) "Demo client AABCM1234F exists"

    if ($null -ne $demo) {
        $sync = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/v1/clients/$($demo.id)/sync" -Headers $headers
        Assert-True ($sync.status -eq "Succeeded" -or $sync.status -eq "AwaitingOtp") `
            "POST sync for demo client status=$($sync.status)"

        $notices = Invoke-RestMethod -Uri "$ApiBase/api/v1/clients/$($demo.id)/notices?kind=Notice" -Headers $headers
        Assert-True ($notices.notices.Count -ge 1) "Client notices list is non-empty"
    }
}
catch {
    Assert-True $false "Clients / sync path ($($_.Exception.Message))"
}

Write-Host ""
if ($failed -eq 0) {
    Write-Host "All smoke checks passed." -ForegroundColor Green
    exit 0
}

Write-Host "$failed smoke check(s) failed." -ForegroundColor Red
exit 1
