cd $PSScriptRoot

$files = Get-ChildItem -Path .\DVBTTelevizor.MAUI -Recurse | where { $_.Name.EndsWith(".cs") -or $_.Name.EndsWith(".xaml") }


function Get-TranslatedText 
{
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [string]$Line
    )

  if ($Line -match '"((?:[^"\\]|\\.)+)"\s*\.Translated\s*\(') {
        # Unescape \" -> "
        return ($matches[1] -replace '\\\"', '"')
        }
}


function Get-XamlTranslatedText {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [string]$Line
    )

    process {
       if ([String]::IsNullOrWhiteSpace($Line))
       {
        return $null
       }
       
    }
}


$dict = @()
foreach ($f in $files)
{
    foreach ($line in Get-Content -Path $f.FullName)
    {
        # searching  "text".Translated()
        if ($line.Contains("Translated("))
        {
                foreach ($l in $line.Split(","))
                {
                    if ([String]::IsNullOrWhiteSpace($l))
                    {
                        continue
                    }

                    $txt = $l | Get-TranslatedText
                    if (-not $dict.Contains($txt))
                    {
                        $dict+= $txt
                    }
                }            

        }


        # searching Text="{local:LngXamlExt Input='No channel'}"
        if ($line.Contains("LngXamlExt"))
        {
            foreach ($l in $line.Split(" "))
            {
                $l | Get-XamlTranslatedText
            }
        }

    }
}



#$dict | Out-GridView