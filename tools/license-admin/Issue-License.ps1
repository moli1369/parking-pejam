param(
    [Parameter(Mandatory=$true)] [string]$Company,
    [Parameter(Mandatory=$true)] [string]$InstallationId,
    [Parameter(Mandatory=$true)] [string]$Plan,
    [Parameter(Mandatory=$true)] [string]$PrivateKeyPath,
    [Parameter(Mandatory=$true)] [string]$OutputPath,
    [string]$LicenseId = ("PP-" + (Get-Date -Format "yyyyMMdd-HHmmss")),
    [int]$Days = 365,
    [int]$MaxUsers = 10,
    [int]$MaxYards = 1,
    [int]$MaxVehiclesPerMonth = 5000,
    [int]$OfflineGraceDays = 30,
    [string[]]$Modules = @("Import","Manifest","Tally","Yard","Inspection","Customs","Gate","Dispatch","Sensors","LPR","Reports")
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $PrivateKeyPath)) { throw "Private signing key not found: $PrivateKeyPath" }

$now = [DateTimeOffset]::UtcNow
$payload = [ordered]@{
    licenseId = $LicenseId
    company = $Company
    installationId = $InstallationId
    plan = $Plan
    issuedAtUtc = $now.ToString("O")
    expiresAtUtc = $now.AddDays($Days).ToString("O")
    gracePeriodDays = $OfflineGraceDays
    maxUsers = $MaxUsers
    maxYards = $MaxYards
    maxVehiclesPerMonth = $MaxVehiclesPerMonth
    modules = @($Modules | Sort-Object -Unique)
}

$canonical = ($payload | ConvertTo-Json -Compress -Depth 5)
$bytes = [Text.Encoding]::UTF8.GetBytes($canonical)
$rsa = [Security.Cryptography.RSA]::Create()
try {
    $pem = Get-Content -LiteralPath $PrivateKeyPath -Raw
    $rsa.ImportFromPem($pem)
    $signature = $rsa.SignData($bytes, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)
}
finally { $rsa.Dispose() }

$result = [ordered]@{
    algorithm = "RSA-PSS-SHA256-3072"
    payload = $payload
    signature = [Convert]::ToBase64String($signature)
}
$json = $result | ConvertTo-Json -Depth 8
$parent = Split-Path -Parent $OutputPath
if ($parent -and -not (Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
Set-Content -LiteralPath $OutputPath -Value $json -Encoding UTF8

Write-Host "License created: $OutputPath"
Write-Host "License ID: $LicenseId"
Write-Host "Company: $Company"
Write-Host "Installation: $InstallationId"
Write-Host "Plan: $Plan"
Write-Host "Expires: $($payload.expiresAtUtc)"
Write-Host "IMPORTANT: keep the private signing key off GitHub and off customer servers."