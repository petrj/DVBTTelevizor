Set-Location $PSScriptRoot
Import-Module .\MAUI-Build-Module.psm1 -Force

#./Clear.ps1

$env:JAVA_HOME = "/usr/lib/jvm/java-17-openjdk-amd64"
$env:PATH = "${$env:JAVA_HOME}/bin:$env:PATH"

$env:ANDROID_HOME = "$HOME/Android"
$env:PATH = "$env:ANDROID_HOME/bin:$env:PATH"

$aABPackage = Get-Item ".\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj" `
    | Publish-AABPackage `
        -Configuration Release `
        -PackageName "net.petrjanousek.DVBTTelevizor" `
        -AndroidSDKDirectory "$HOME/Android/Sdk/" 

$aABPackage | Copy-Item -Destination . -Force -Verbose

