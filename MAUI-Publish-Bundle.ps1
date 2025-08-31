<#

Script for creating AAB/APK release for publishing to Google Play

    Android TV necessary release modifications:

     1) DVBTTelevizor.MAUI\Platforms\Android\AndroidManifest.xml
  
        <uses-feature android:name="android.software.leanback" android:required="true" />
 
     2) DVBTTelevizor.MAUI\Platforms\Android\MainActivity.cs
 
        [IntentFilter(new[] { Intent.ActionMain }, AutoVerify = true, Categories = new[] { Intent.CategoryLeanbackLauncher })]

    Do not include AndroidTV modifications to non-Android TV release!

#>

Set-Location $PSScriptRoot
Import-Module .\MAUI-BuildModule.psm1 -Force

$passw = Get-Password

.\Clear.ps1

$signedAABPackage = Get-Item ".\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj" `
    | Publish-AABPackage `
        -Configuration Release `
        -PackageName "net.petrjanousek.DVBTTelevizor" `
    | Protect-BySignature `
        -Keystore "C:\Users\petrj\AppData\Local\Xamarin\Mono for Android\KeyStore\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore" `
        -Alias "PJsAndroidKeyStore" `
        -Password $passw `

$signedAPKPackage = $signedAABPackage | ConvertTo-APK `
        -Keystore "C:\Users\petrj\AppData\Local\Xamarin\Mono for Android\KeyStore\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore" `
        -Alias "PJsAndroidKeyStore" `
        -Password $passw 
    

$signedAABPackage | Copy-Item -Destination . -Force -Verbose
$signedAPKPackage | Copy-Item -Destination . -Force -Verbose
   