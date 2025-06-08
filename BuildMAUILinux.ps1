#dotnet build /p:AndroidSdkDirectory=/home/kirk/Android/Sdk/ /t:Run -p:AndroidEmulator=true DVBTTelevizor.MAUI/DVBTTelevizor.MAUI.csproj 
./Clear.ps1
dotnet build /p:AndroidSdkDirectory=/home/kirk/Android/Sdk/ /t:Build DVBTTelevizor.MAUI/DVBTTelevizor.MAUI.csproj 

