cd $PSScriptRoot

./Clear.ps1

$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $msbuild))
{
    $msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\msbuild.exe"
}

$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$configuration = "Debug"
$manifestPath = "DVBTTelevizor\DVBTTelevizor.Android\Properties\AndroidManifest.xml" 

$slnFileName = Join-Path -Path $PSScriptRoot -ChildPath "DVBTTelevizor.sln"

$projFileName = Join-Path -Path $PSScriptRoot -ChildPath "DVBTTelevizor\DVBTTelevizor.Android\DVBTTelevizor.Android.csproj"
$projFileNameTmp = [System.IO.Path]::GetTempFileName()

Copy-Item $projFileName -Destination $projFileNameTmp -Force
[string]$proj = Get-Content -Path $projFileName

$proj = $proj.Replace("<DebugSymbols>true</DebugSymbols>","<DebugSymbols>false</DebugSymbols>")
$proj = $proj.Replace("<EmbedAssembliesIntoApk>false</EmbedAssembliesIntoApk>","<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>")
$proj = $proj.Replace("<AndroidUseSharedRuntime>true</AndroidUseSharedRuntime>","<AndroidUseSharedRuntime>false</AndroidUseSharedRuntime>")

$proj | Out-File -FilePath $projFileName

# restore nuget packages

if (-not (Test-Path -Path "nuget.exe"))
{
    Invoke-WebRequest -Uri $nugetUrl -OutFile "nuget.exe"
}

.\nuget.exe restore .\DVBTTelevizor.sln

try
{

    & $msbuild $slnFileName /t:SignAndroidPackage /p:Configuration="$configuration" /v:d | Out-Host

    $apk = "DVBTTelevizor\DVBTTelevizor.Android\bin\Debug\net.petrjanousek.DVBTTelevizor-Signed.apk"

    if (Test-Path -Path $apk)
    {
        $manifestPath = "DVBTTelevizor\DVBTTelevizor.Android\Properties\AndroidManifest.xml"  

        [xml]$manifest = Get-content -Path $manifestPath
        $version = $manifest.manifest.versionCode

        Move-Item -Path $apk -Destination "./net.petrjanousek.DVBTTelevizor.v$version.$configuration.apk"  -Force -Verbose

    } else
    {
        throw "$apk does not exist"
    }
} finally
{
    Copy-Item $projFileNameTmp -Destination $projFileName -Force
}

