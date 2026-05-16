Set-Location $PSScriptRoot




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

$passw = Get-Password

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

    $signedAABPackage | Copy-Item -Destination . -Force -Verbose
    $signedAPKPackage | Copy-Item -Destination . -Force -Verbose
} else
{
    $aABPackage | Copy-Item -Destination . -Force -Verbose
}
