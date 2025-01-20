$scriptPath = $PSScriptRoot
cd $PSScriptRoot

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
    "packages",
    ".vs"
     ))
{
    $fullPath = [System.IO.Path]::Combine($scriptPath,$folder)
    if (-not $fullPath.EndsWith("\"))
    {
            $fullPath += "\"
    }

    if (Test-Path -Path $fullPath)
    {
	Remove-Item -Path $fullPath -Recurse -Force -Verbose		
    }
}
