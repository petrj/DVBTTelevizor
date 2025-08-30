<#

DVBT Televizor script for publishing to Google Play

    Android TV necessary release modifications:

     1) DVBTTelevizor.MAUI\Platforms\Android\AndroidManifest.xml
  
        <uses-feature android:name="android.software.leanback" android:required="true" />
 
     2) DVBTTelevizor.MAUI\Platforms\Android\MainActivity.cs
 
        [IntentFilter(new[] { Intent.ActionMain }, AutoVerify = true, Categories = new[] { Intent.CategoryLeanbackLauncher })]

    Do not include AndroidTV modifications to non-Android TV channel!

#>

Set-Location $PSScriptRoot

# Clear

.\Clear.ps1

# Build

dotnet publish .\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj -c Release -f net9.0-android35.0 /p:AndroidPackageFormat=aab

# Sign

$jarsigner = "C:\Program Files (x86)\Android\openjdk\jdk-17.0.14\bin\jarsigner.exe"
$aab = "DVBTTelevizor.MAUI\bin\Release\net9.0-android35.0\net.petrjanousek.DVBTTelevizor.aab"
$keystore = "C:\Users\petrj\AppData\Local\Xamarin\Mono for Android\KeyStore\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore"

Write-Host "Signing the package. Enter the password, please"

$passwEncrypted = Read-Host -AsSecureString
$ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($passwEncrypted)
$passw = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)

& $jarsigner -keystore $keystore  -storepass $passw $aab "PJsAndroidKeyStore"

Write-Host "Signing the package........."

# Check AndroidTV

[xml]$manifest = Get-Content ".\DVBTTelevizor.MAUI\Platforms\Android\AndroidManifest.xml"
if ($manifest.manifest.'uses-feature'.name -eq  "android.software.leanback")
{
    $suffix = ".AndroidTV"
} else
{
    $suffix = ""
}

# Check Version

[xml]$csproj = Get-Content ".\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj"
$version = $csproj.Project.PropertyGroup.VersionCode
if ($version -is [System.Array])
{
    $version = $version[0]
}

Copy-Item $aab "net.petrjanousek.DVBTTelevizor.${version}${suffix}.aab"
