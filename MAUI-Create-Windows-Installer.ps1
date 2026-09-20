<#
.SYNOPSIS
    Builds the native Windows platform installer (MSIX package) for DVBT Televizor (.NET MAUI).

.DESCRIPTION
    Builds and packages the application using .NET MAUI's built-in Windows MSIX packaging pipeline,
    signs the package with a developer code-signing certificate, and exports the installer package.

.PARAMETER Configuration
    Build configuration ("Release" or "Debug"). Default is "Release".

.PARAMETER TargetFramework
    Target framework moniker. Default is "net10.0-windows10.0.26100.0".

.PARAMETER Version
    Optional version override. If omitted, uses the version defined in DVBTTelevizor.MAUI.csproj.

.PARAMETER Clean
    Runs Clear.ps1 before building.

.EXAMPLE
    .\MAUI-Create-Windows-Installer.ps1

.EXAMPLE
    .\MAUI-Create-Windows-Installer.ps1 -Clean -Configuration Release
#>

[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [string]$TargetFramework = "net10.0-windows10.0.26100.0",

    [string]$Version = "",

    [switch]$Clean
)

Set-Location $PSScriptRoot
$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj"
if (-not (Test-Path -LiteralPath $projectPath))
{
    throw "Project file not found at: $projectPath"
}

# 1. Clean if requested
if ($Clean)
{
    $clearScript = Join-Path $PSScriptRoot "Clear.ps1"
    if (Test-Path -LiteralPath $clearScript)
    {
        Write-Host "Cleaning build folders using Clear.ps1..." -ForegroundColor Cyan
        & $clearScript
    }
}

# 2. Extract version from csproj if not specified
[xml]$csproj = Get-Content -LiteralPath $projectPath
$winGroup = $csproj.Project.PropertyGroup | Where-Object { $_.Condition -like "*windows*" }
$detectedVer = $winGroup.ApplicationDisplayVersion
if ($detectedVer -is [System.Array])
{
    $detectedVer = $detectedVer | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($detectedVer))
{
    $detectedVer = $winGroup.ApplicationVersion
    if ($detectedVer -is [System.Array])
    {
        $detectedVer = $detectedVer | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
    }
}
if ([string]::IsNullOrWhiteSpace($detectedVer))
{
    $detectedVer = "2026"
}

$displayVersion = if ([string]::IsNullOrWhiteSpace($Version)) { [string]$detectedVer } else { $Version }

Write-Host "=======================================================" -ForegroundColor Green
Write-Host " DVBT Televizor - Windows Platform Installer (MSIX)" -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host " Configuration   : $Configuration"
Write-Host " Target Framework: $TargetFramework"
Write-Host " Version         : $displayVersion"
Write-Host " Project         : $projectPath"
Write-Host "=======================================================" -ForegroundColor Green

# 3. Ensure developer signing certificate exists
$pfxPath = Join-Path $PSScriptRoot "DVBTTelevizor_DevCert.pfx"
$cerPath = Join-Path $PSScriptRoot "DVBTTelevizor.cer"
$certPassword = "DVBTTelevizor"

if (-not (Test-Path -LiteralPath $pfxPath) -or -not (Test-Path -LiteralPath $cerPath))
{
    Write-Host "`nGenerating developer code-signing certificate (CN=User Name)..." -ForegroundColor Cyan
    $cert = New-SelfSignedCertificate -Type Custom `
        -Subject "CN=User Name" `
        -KeyUsage DigitalSignature `
        -FriendlyName "DVBT Televizor Developer Certificate" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")

    $secPass = ConvertTo-SecureString -String $certPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $secPass | Out-Null
    Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
    Write-Host "Certificate exported to: $cerPath" -ForegroundColor Green
}

# 4. Execute dotnet publish for MSIX
Write-Host "`nPublishing Windows MSIX package..." -ForegroundColor Cyan

$publishArgs = @(
    "publish",
    $projectPath,
    "-f", $TargetFramework,
    "-c", $Configuration,
    "-p:WindowsPackageType=MSIX"
)

# Only override version on CLI if explicitly provided by user
if ($PSBoundParameters.ContainsKey('Version') -and -not [string]::IsNullOrWhiteSpace($Version))
{
    $publishArgs += "-p:ApplicationDisplayVersion=$Version"
    $publishArgs += "-p:ApplicationVersion=$Version"
}

dotnet @publishArgs

if ($LASTEXITCODE -ne 0)
{
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

# 5. Find generated MSIX package
$appPackagesDir = Join-Path $PSScriptRoot "DVBTTelevizor.MAUI\AppPackages"
$latestPackageFolder = Get-ChildItem -Path $appPackagesDir -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $latestPackageFolder)
{
    throw "Could not find generated AppPackages directory in $appPackagesDir"
}

$msixFile = Get-ChildItem -Path $latestPackageFolder.FullName -Filter "*.msix" | Where-Object { $_.Name -notlike "*Dependencies*" } | Select-Object -First 1

if (-not $msixFile)
{
    throw "Could not find .msix file in $($latestPackageFolder.FullName)"
}

Write-Host "`nMSIX package built: $($msixFile.FullName)" -ForegroundColor Green

# 6. Sign package with developer certificate
$signtool = (Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages\microsoft.windows.sdk.buildtools" -Filter "signtool.exe" -Recurse | Where-Object { $_.FullName -like "*x64*" } | Select-Object -First 1).FullName
if ($signtool -and (Test-Path -LiteralPath $signtool))
{
    Write-Host "Signing MSIX package with developer certificate using signtool..." -ForegroundColor Cyan
    & $signtool sign /fd SHA256 /f $pfxPath /p $certPassword $msixFile.FullName
    if ($LASTEXITCODE -eq 0)
    {
        Write-Host "MSIX package digitally signed successfully!" -ForegroundColor Green
    }
}
else
{
    Write-Warning "signtool.exe was not found. The package may need to be signed manually."
}

# 7. Copy package to project root (consistent with Android aab/apk deploy scripts)
$rootPackageName = "net.petrjanousek.DVBTTelevizor.$displayVersion.msix"
$rootPackagePath = Join-Path $PSScriptRoot $rootPackageName
Copy-Item -LiteralPath $msixFile.FullName -Destination $rootPackagePath -Force -Verbose

# Also copy original named package if different
$destOriginal = Join-Path $PSScriptRoot $msixFile.Name
if ($destOriginal -ne $rootPackagePath)
{
    Copy-Item -LiteralPath $msixFile.FullName -Destination $destOriginal -Force
}

# 8. Create Install-App.ps1 helper script
$installHelperPath = Join-Path $PSScriptRoot "Install-App.ps1"
$installAppScript = @"
Set-Location `$PSScriptRoot

`$certPath = Join-Path `$PSScriptRoot "DVBTTelevizor.cer"
`$msixPath = Join-Path `$PSScriptRoot "$rootPackageName"

if (-not (Test-Path -LiteralPath `$msixPath))
{
    `$msixPath = Get-ChildItem -Path `$PSScriptRoot -Filter "*.msix" | Sort-Object LastWriteTime -Descending | Select-Object -ExpandProperty FullName -First 1
}

if (-not (Test-Path -LiteralPath `$certPath))
{
    Write-Error "Certificate file not found: `$certPath"
    exit 1
}

`$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not `$isAdmin)
{
    Write-Host "Elevating permissions to trust certificate and install app..." -ForegroundColor Cyan
    Start-Process powershell -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -Command `"Import-Certificate -FilePath '`$certPath' -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople'; Add-AppxPackage -Path '`$msixPath'; Write-Host 'DVBT Televizor installed successfully!'; Start-Sleep -Seconds 3`""
}
else
{
    Import-Certificate -FilePath `$certPath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople"
    Add-AppxPackage -Path `$msixPath
    Write-Host "DVBT Televizor installed successfully!" -ForegroundColor Green
}
"@
Set-Content -Path $installHelperPath -Value $installAppScript -Encoding UTF8

Write-Host "`n=======================================================" -ForegroundColor Green
Write-Host " Windows Platform Installer created and signed!" -ForegroundColor Green
Write-Host " Installer package: $rootPackagePath" -ForegroundColor Green
Write-Host " Certificate      : $cerPath" -ForegroundColor Green
Write-Host " Helper script    : $installHelperPath" -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host "`nTo install on Windows (First-time setup):" -ForegroundColor Yellow
Write-Host "  Option 1: Run .\Install-App.ps1 (trusts certificate and installs the app)" -ForegroundColor Yellow
Write-Host "  Option 2: Double-click DVBTTelevizor.cer -> Install Certificate -> Local Machine -> Trusted People." -ForegroundColor Yellow
Write-Host "            Then double-click $rootPackageName to install!" -ForegroundColor Yellow
Write-Host "=======================================================" -ForegroundColor Green
