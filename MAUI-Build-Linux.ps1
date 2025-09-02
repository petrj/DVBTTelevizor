Set-Location $PSScriptRoot
Import-Module .\MAUI-Build-Module.psm1 -Force

$passw = Get-Password

./Clear.ps1

$env:JAVA_HOME = "/usr/lib/jvm/java-17-openjdk-amd64"
$env:PATH = "${$env:JAVA_HOME}/bin:$env:PATH"

$env:ANDROID_HOME = "$HOME/Android"
$env:PATH = "$env:ANDROID_HOME/bin:$env:PATH"

$signedAABPackage = Get-Item ".\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj" `
    | Publish-AABPackage `
        -Configuration Release `
        -PackageName "net.petrjanousek.DVBTTelevizor" `
        -AndroidSDKDirectory "$HOME/Android/Sdk/" `
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
