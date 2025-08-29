Set-Location $PSScriptRoot

.\Clear.ps1

dotnet publish .\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj -c Release -f net9.0-android35.0 /p:AndroidPackageFormat=aab

$jarsigner = "C:\Program Files (x86)\Android\openjdk\jdk-17.0.14\bin\jarsigner.exe"
$aab = "DVBTTelevizor.MAUI\bin\Release\net9.0-android35.0\net.petrjanousek.DVBTTelevizor.aab"
$keystore = "C:\Users\petrj\AppData\Local\Xamarin\Mono for Android\KeyStore\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore"

& $jarsigner -keystore $keystore $aab "PJsAndroidKeyStore"

[xml]$manifest = Get-Content ".\DVBTTelevizor.MAUI\Platforms\Android\AndroidManifest.xml"
$version = $manifest.manifest.versionName

Copy-Item $aab "net.petrjanousek.DVBTTelevizor.AndroidTV.$version.aab"

