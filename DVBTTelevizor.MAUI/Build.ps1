./Clear.ps1
dotnet restore
dotnet build -f net10.0-windows10.0.26100.0 -p:Platform=x64 -c Release
dotnet build -f net10.0-and -c Releaseroid -c Release
