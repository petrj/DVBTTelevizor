Set-Location $PSScriptRoot

./Clear.ps1

$env:JAVA_HOME = "/usr/lib/jvm/java-17-openjdk-amd64"
$env:PATH = "${$env:JAVA_HOME}/bin:$env:PATH"

$env:ANDROID_HOME = "$HOME/Android"
$env:PATH = "$env:ANDROID_HOME/bin:$env:PATH"

dotnet publish --framework "net9.0-android35.0" /p:AndroidSdkDirectory=$HOME/Android/Sdk/ /p:AndroidPackageFormat=aab /t:Build DVBTTelevizor.MAUI/DVBTTelevizor.MAUI.csproj

