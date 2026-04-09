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

Set-Location $PSScriptRoot
Import-Module .\MAUI-Build-Module.psm1 -Force

$passw = Get-SecureStringFromUserInput -Message "Enter password to Android store:" -EnvironmentVariable $Env:PJsAndroidStore

.\Clear.ps1

$aABPackage = Get-Item ".\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj" `
    | Publish-AABPackage `
        -Configuration Release `
        -PackageName "net.petrjanousek.DVBTTelevizor"

if (-not [String]::IsNullOrEmpty($passw))
{
    $signedAABPackage = $aABPackage `
        | Protect-BySignature `
            -Keystore "$HOME\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore" `
            -JarSigner "C:\Program Files\Android\openjdk\jdk-21.0.8\bin\jarsigner.exe" `
            -Alias "PJsAndroidKeyStore" `
            -Password $passw `

    $signedAPKPackage = $signedAABPackage | ConvertTo-APK `
            -Keystore "$HOME\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore" `
            -Alias "PJsAndroidKeyStore" `
            -Java  "C:\Program Files\Android\openjdk\jdk-21.0.8\bin\java.exe"`
            -Password $passw 

        $signedAABPackage | Copy-Item -Destination . -Force -Verbose
        $signedAPKPackage | Copy-Item -Destination . -Force -Verbose
} else
{
    
    $aABPackage | Copy-Item -Destination . -Force -Verbose
}
    


   