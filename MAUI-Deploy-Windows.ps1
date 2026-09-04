Set-Location $PSScriptRoot
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

if (-not [String]::IsNullOrEmpty($passw))
{
    $signedAABPackage = $aABPackage `
        | Protect-BySignature `
            -Keystore "$HOME\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore" `
            -JarSigner "C:\Program Files\Java\jdk-26\bin\jarsigner.exe" `
            -Alias "PJsAndroidKeyStore" `
            -Password $passw `

    $signedAPKPackage = $signedAABPackage | ConvertTo-APK `
            -Keystore "$HOME\PJsAndroidKeyStore\PJsAndroidKeyStore.keystore" `
            -Alias "PJsAndroidKeyStore" `
            -Java  "java.exe"`
            -Password $passw 

        $signedAABPackage | Copy-Item -Destination . -Force -Verbose
        $signedAPKPackage | Copy-Item -Destination . -Force -Verbose
} else
{
    
    $aABPackage | Copy-Item -Destination . -Force -Verbose
}
  


   