cd $PSScriptRoot

$files = Get-ChildItem -Path .\DVBTTelevizor.MAUI -Recurse | where { $_.Name.EndsWith(".cs") -or $_.Name.EndsWith(".xaml") }

foreach ($f in $files)
{
    foreach ($line in Get-Content -Path $f.FullName)
    {
        if ($line.Contains("Translated()"))
        {
            $line.Split(",")
        }
    }
}