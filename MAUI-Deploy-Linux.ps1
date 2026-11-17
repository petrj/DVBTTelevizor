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

# Using Powershell.Modules from latest NuGet package

    $maxVersion = Get-ChildItem "$env:HOME/.nuget/packages/powershell.modules/" | Select-Object -Property Name -ExpandProperty Name | sort-object -Descending | Select-Object -First 1
    $modulePath = "$env:HOME/.nuget/packages/powershell.modules/$maxVersion/Powershell.Modules/"

    if (Get-Module -Name BuildModule)
    {
        Write-Host "Reloading BuildModule module version $maxVersion..."
        Remove-Module BuildModule
    } else
    {
        Write-Host "Loading BuildModule module version $maxVersion from $modulePath ..."
    }

    Import-Module $modulePath/BuildModule/BuildModule.psd1

$passw = Get-SecureStringFromUserInput -Message "Enter password to Android store:" -EnvironmentVariable $env:PJsAndroidStore

./Clear.ps1

$env:JAVA_HOME = "/usr/lib/jvm/java-17-openjdk-amd64"
$env:PATH = "${$env:JAVA_HOME}/bin:$env:PATH"

$env:ANDROID_HOME = "$HOME/Android"
$env:PATH = "$env:ANDROID_HOME/bin:$env:PATH"

$aABPackage = Get-Item ".\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj" `
    | Publish-AABPackage `
        -Configuration Release `
        -PackageName "net.petrjanousek.DVBTTelevizor" `
        -AndroidSDKDirectory "$HOME/Android/Sdk/"

$aABPackageForSymbols = $aABPackage

if (-not [String]::IsNullOrEmpty($passw))
{
    $signedAABPackage = $aABPackage `
        | Protect-BySignature `
            -JarSigner /usr/lib/jvm/java-17-openjdk-amd64/bin/jarsigner `
            -Keystore ~/PJsAndroidKeyStore/PJsAndroidKeyStore.keystore `
            -Password $passw `
            -Alias "PJsAndroidKeyStore"

    $signedAPKPackage = $signedAABPackage | ConvertTo-APK `
            -Java "/usr/lib/jvm/java-17-openjdk-amd64/bin/java" `
            -BundleTool "/opt/bundletool-all-1.18.1.jar" `
            -Keystore "~/PJsAndroidKeyStore/PJsAndroidKeyStore.keystore" `
            -Alias "PJsAndroidKeyStore" `
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
