Set-Location $PSScriptRoot

function New-GooglePlayNativeDebugSymbols
{
    param (
        [Parameter(Mandatory)]
        [System.IO.FileInfo]$AppBundle,

        [Parameter(Mandatory)]
        [string]$OutputPath
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $sourceArchive = $null
    $outputStream = $null
    $symbolsArchive = $null
    $symbolCount = 0
    $entryNames = @{}

    try
    {
        $sourceArchive = [System.IO.Compression.ZipFile]::OpenRead($AppBundle.FullName)
        $outputStream = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::ReadWrite)
        $symbolsArchive = [System.IO.Compression.ZipArchive]::new(
            $outputStream,
            [System.IO.Compression.ZipArchiveMode]::Create,
            $false
        )

        foreach ($entry in $sourceArchive.Entries)
        {
            if ($entry.FullName -match '^[^/]+/lib/(armeabi-v7a|arm64-v8a|x86|x86_64)/([^/]+\.so)$')
            {
                $symbolEntryName = "$($Matches[1])/$($Matches[2])"
                if ($entryNames.ContainsKey($symbolEntryName))
                {
                    throw "Duplicate native library in app bundle: $symbolEntryName"
                }

                $entryNames[$symbolEntryName] = $true
                $symbolEntry = $symbolsArchive.CreateEntry($symbolEntryName, [System.IO.Compression.CompressionLevel]::Optimal)
                $sourceStream = $entry.Open()
                $symbolStream = $symbolEntry.Open()
                try
                {
                    $sourceStream.CopyTo($symbolStream)
                }
                finally
                {
                    $symbolStream.Dispose()
                    $sourceStream.Dispose()
                }

                $symbolCount++
            }
        }
    }
    finally
    {
        if ($null -ne $symbolsArchive) { $symbolsArchive.Dispose() }
        if ($null -ne $outputStream) { $outputStream.Dispose() }
        if ($null -ne $sourceArchive) { $sourceArchive.Dispose() }
    }

    if ($symbolCount -eq 0)
    {
        Remove-Item -LiteralPath $OutputPath -Force -ErrorAction SilentlyContinue
        throw "No Android native libraries were found in $($AppBundle.FullName); cannot create Google Play symbols."
    }

    Write-Host "Created Google Play native debug symbols: $OutputPath ($symbolCount libraries)" -ForegroundColor Green
}

<#

Script for creating AAB/APK release for publishing to Google Play

    Android TV necessary release modifications:

     1) DVBTTelevizor.MAUI\Platforms\Android\AndroidManifest.xml

			<uses-feature android:name="android.software.leanback" android:required="true" />
			<uses-feature android:name="android.hardware.faketouch" android:required="false" />
			<uses-feature android:name="android.hardware.touchscreen" android:required="false" />

     2) DVBTTelevizor.MAUI\Platforms\Android\MainActivity.cs

        [IntentFilter(new[] { Intent.ActionMain }, AutoVerify = true, Categories = new[] { Intent.CategoryLeanbackLauncher })]


    Do not include AndroidTV modifications to non-Android TV release!

#>

# Using Powershell.Modules from latest NuGet package

    $maxVersion = Get-ChildItem "$env:USERPROFILE\.nuget\packages\powershell.modules\" | Select-Object -Property Name -ExpandProperty Name | sort-object -Descending | Select-Object -First 1
    $modulePath = "$env:USERPROFILE\.nuget\packages\powershell.modules\$maxVersion\PowerShell.Modules\"

    if (Get-Module -Name BuildModule)
    {
        Write-Host "Reloading BuildModule module version $maxVersion..."
        Remove-Module BuildModule
    } else
    {
        Write-Host "Loading BuildModule module version $maxVersion..."
    }

    Import-Module $modulePath\BuildModule\BuildModule.psd1

$passw = Get-SecureStringFromUserInput -Message "Enter password to Android store:" -EnvironmentVariable $env:PJsAndroidStore

.\Clear.ps1

$aABPackage = Get-Item ".\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj" `
    | Publish-AABPackage `
        -Configuration Release `
        -PackageName "net.petrjanousek.DVBTTelevizor"

$aABPackageForSymbols = $aABPackage

if (-not [String]::IsNullOrEmpty($passw))
{
    $signedAABPackage = $aABPackage `
        | Protect-BySignature `
            -Keystore "$HOME\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore" `
            -JarSigner "jarsigner.exe" `
            -Alias "PJsAndroidKeyStore" `
            -Password $passw `

    $signedAPKPackage = $signedAABPackage | ConvertTo-APK `
            -Keystore "$HOME\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore" `
            -Alias "PJsAndroidKeyStore" `
            -Java  "java.exe"`
            -Password $passw

        $aABPackageForSymbols = $signedAABPackage
        $signedAABPackage | Copy-Item -Destination . -Force -Verbose
        $signedAPKPackage | Copy-Item -Destination . -Force -Verbose
} else
{

    $aABPackage | Copy-Item -Destination . -Force -Verbose
}

$mappingFile = Join-Path $aABPackage.DirectoryName "mapping.txt"
if (-not (Test-Path -LiteralPath $mappingFile -PathType Leaf))
{
    throw "R8 mapping file was not generated: $mappingFile"
}

$rootMappingFile = Join-Path $PSScriptRoot "$($aABPackage.BaseName).mapping.txt"
Copy-Item -LiteralPath $mappingFile -Destination $rootMappingFile -Force -Verbose

$nativeDebugSymbols = Join-Path $PSScriptRoot "$($aABPackage.BaseName).symbols.zip"
New-GooglePlayNativeDebugSymbols -AppBundle $aABPackageForSymbols -OutputPath $nativeDebugSymbols


