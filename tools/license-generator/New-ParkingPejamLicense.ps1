param(
    [Parameter(Mandatory=$true)][string]$PrivateKeyPath,
    [Parameter(Mandatory=$true)][string]$OutputPath,
    [Parameter(Mandatory=$true)][string]$LicenseId,
    [Parameter(Mandatory=$true)][string]$CompanyName,
    [Parameter(Mandatory=$true)][string]$InstallationId,
    [Parameter(Mandatory=$true)][string]$Plan,
    [Parameter(Mandatory=$true)][datetime]$ExpiresAtUtc,
    [int]$MaxUsers = 10,
    [int]$MaxYards = 1,
    [int]$MaxVehiclesPerMonth = 2000,
    [int]$GracePeriodDays = 30,
    [string[]]$Modules = @('Import','Manifest','Tally','Yard','Inspection','Customs','Gate','Dispatch','Documents','Reports')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-CanonicalJson($Object) {
    return ($Object | ConvertTo-Json -Depth 8 -Compress)
}

if (-not (Test-Path -LiteralPath $PrivateKeyPath)) { throw "Private key file not found: $PrivateKeyPath" }

Add-Type -AssemblyName System.Security
$rsa = [System.Security.Cryptography.RSA]::Create()
$pem = [System.IO.File]::ReadAllText((Resolve-Path $PrivateKeyPath))
$rsa.ImportFromPem($pem)

$payload = [ordered]@{
    licenseId = $LicenseId
    companyName = $CompanyName
    installationId = $InstallationId
    plan = $Plan
    issuedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    expiresAtUtc = ([DateTimeOffset]$ExpiresAtUtc.ToUniversalTime()).ToString('o')
    maxUsers = $MaxUsers
    maxYards = $MaxYards
    maxVehiclesPerMonth = $MaxVehiclesPerMonth
    gracePeriodDays = $GracePeriodDays
    modules = @($Modules | Sort-Object)
}

$payloadJson = ConvertTo-CanonicalJson $payload
$bytes = [Text.Encoding]::UTF8.GetBytes($payloadJson)
$signature = $rsa.SignData($bytes, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)

$license = [ordered]@{
    payload = $payload
    signature = [Convert]::ToBase64String($signature)
}

$directory = Split-Path -Parent $OutputPath
if ($directory -and -not (Test-Path $directory)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
$license | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8
Write-Host "License written to $OutputPath"
