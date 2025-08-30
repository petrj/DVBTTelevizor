Set-Location $PSScriptRoot
.\Clear.ps1

dotnet publish .\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj -c Release -f net9.0-android35.0 /p:AndroidPackageFormat=aab

$jarsigner = "C:\Program Files (x86)\Android\openjdk\jdk-17.0.14\bin\jarsigner.exe"
$aab = "DVBTTelevizor.MAUI\bin\Release\net9.0-android35.0\net.petrjanousek.DVBTTelevizor.aab"
$keystore = "C:\Users\petrj\AppData\Local\Xamarin\Mono for Android\KeyStore\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore"

Write-Host "Signing the package. Enter the password, please"

$passwEncrypted = Read-Host -AsSecureString
$ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($passwEncrypted)
$passw = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)

& $jarsigner -keystore $keystore  -storepass $passw $aab "PJsAndroidKeyStore"

[xml]$manifest = Get-Content ".\DVBTTelevizor.MAUI\Platforms\Android\AndroidManifest.xml"
$version = $manifest.manifest.versionName

if ($manifest.manifest.'uses-feature'.name -eq  "android.software.leanback")
{
    $suffix = ".AndroidTV"
} else
{
    $suffix = ""
}

Copy-Item $aab "net.petrjanousek.DVBTTelevizor.AndroidTV.${version}${suffix}.aab"
