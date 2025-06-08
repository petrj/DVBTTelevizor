./Clear.ps1
dotnet publish --framework "net9.0-android35.0" /p:AndroidSdkDirectory=/home/kirk/Android/Sdk/ /p:AndroidPackageFormat=apk /t:Build DVBTTelevizor.MAUI/DVBTTelevizor.MAUI.csproj 

