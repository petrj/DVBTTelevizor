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
#$env:Path += ";C:\Program Files (x86)\Android\android-sdk\platform-tools\"


Write-Host "Enter the password to KeyStore, please:" 

$passwEncrypted = Read-Host -AsSecureString
$ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($passwEncrypted)
$passw = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)

# Clear

.\Clear.ps1

# Build

dotnet publish .\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj -c Release -f net9.0-android35.0 /p:AndroidPackageFormat=aab

# Sign aab

$jarsigner = "C:\Program Files (x86)\Android\openjdk\jdk-17.0.14\bin\jarsigner.exe"
$aab = "DVBTTelevizor.MAUI\bin\Release\net9.0-android35.0\net.petrjanousek.DVBTTelevizor.aab"
$keystore = "C:\Users\petrj\AppData\Local\Xamarin\Mono for Android\KeyStore\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore"
$keystoreAlias = "PJsAndroidKeyStore"

Write-Host "Signing the package"

& $jarsigner -keystore $keystore  -storepass $passw $aab $keystoreAlias

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

$aabName = "net.petrjanousek.DVBTTelevizor.${version}${suffix}.aab"

Copy-Item $aab $aabName

# Create APK

    # https://github.com/google/bundletool/releases
    # "C:\Program Files (x86)\Android\android-sdk\platform-tools\bundletool-all-1.18.1.jar"

$bundleTool = "C:\Program Files (x86)\Android\android-sdk\platform-tools\bundletool-all-1.18.1.jar"
$java = "C:\Program Files (x86)\Android\openjdk\jdk-17.0.14\bin\java.exe"

$outputArchive = Join-Path -Path $PSScriptRoot -ChildPath "$aabName.apks"

if (Test-Path -Path $outputArchive)
{
    Remove-Item -Path $outputArchive -Force -Verbose
} 

Write-Host "Creating universal APK"

& $java -jar $bundleTool build-apks --bundle=$aabName --output=$outputArchive --mode=universal --ks=$keystore --ks-key-alias=$keystoreAlias --ks-pass=pass:$passw 


Rename-Item -Path $outputArchive -NewName ($outputArchive + ".zip")
$outputArchive+=".zip"

$zip = [System.IO.Compression.ZipFile]::OpenRead($outputArchive)
$apkName =  [System.IO.Path]::GetFileNameWithoutExtension($aabName) + ".apk"

Expand-Archive -Path $outputArchive -DestinationPath . -Verbose

Remove-Item "*.pb"
Remove-Item $outputArchive
Rename-Item "universal.apk" $apkName

Get-Item $apkName