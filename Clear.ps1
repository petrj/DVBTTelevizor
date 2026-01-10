cd $PSScriptRoot

#dotnet nuget locals all --clear

#dotnet clean C:\DVBTTelevizor\DVBTTelevizor.MAUI\



foreach ($folder in `
    @(
    "DVBTTelevizor\DVBTTelevizor\bin",
    "DVBTTelevizor\DVBTTelevizor\obj",
    "DVBTTelevizor\DVBTTelevizor.Android\bin",
    "DVBTTelevizor\DVBTTelevizor.Android\obj",
    "DVBTTelevizor.Driver\bin",
    "DVBTTelevizor.Driver\obj",
    "DVBTTelevizor.TV\bin",
    "DVBTTelevizor.TV\obj",    
    "DVBTTelevizor.MAUI\bin",
    "DVBTTelevizor.MAUI\obj",
    "LibVLCSharp.MAUI.Windows\bin",
    "LibVLCSharp.MAUI.Windows\obj",
    "RTLSDR\bin",
    "RTLSDR\obj",
    "RTLSDR.Audio\bin",
    "RTLSDR.Audio\obj",
    "RTLSDR.Common\bin",
    "RTLSDR.Common\obj",
    "RTLSDR.FM\bin",
    "RTLSDR.FM\obj",
    "packages",
    ".vs"
     ))
{
    $fullPath = [System.IO.Path]::Combine($scriptPath,$folder)
    if (-not $fullPath.EndsWith("\"))
    {
            $fullPath += "\"
    }

    Write-Host "Removing $fullPath"

    if (Test-Path -Path $fullPath)
    {
	    Remove-Item -Path $fullPath -Recurse -Force -Verbose		
    }
}


Get-ChildItem -Path ($env:LOCALAPPDATA + "\Microsoft") -Recurse -Directory | Where-Object { $_.FullName -like "*VisualStudio\*\ComponentModelCache"  } | Get-ChildItem | Remove-Item -Force -Verbose

#dotnet restore .\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj --force


