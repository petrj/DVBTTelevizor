cd $PSScriptRoot

$files = Get-ChildItem -Path .\DVBTTelevizor.MAUI -Recurse | where { $_.Name.EndsWith(".cs") -or $_.Name.EndsWith(".xaml") }
$referenceDict = Get-Content -Path .\DVBTTelevizor.MAUI\Resources\Raw\Czech.lng
$alreadyTranslatedText = @()

foreach ($line in $referenceDict)
{
    $enAndCZ = $line.Split("=")

    if (-not $alreadyTranslatedText.Contains($enAndCZ[0]))
    {
         $alreadyTranslatedText += $enAndCZ[0]
    }
}



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
      

      $pos = $Line.IndexOf("{local:LngXamlExt Input='")

      if ($pos -lt 0)
      {
        return $null
      }

      $t = $Line.Substring($pos+25);

      $pos = $t.IndexOf("'")
      #if ($Line.IndexOf("
      $t = $t.Substring(0,$pos)

      return $t
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

                    if ([String]::IsNullOrWhiteSpace($txt))
                    {
                        continue
                    }

                    if (-not $dict.Contains($txt))
                    {
                        $dict+= $txt
                    }
                }           

        }


        # searching Text="{local:LngXamlExt Input='No channel'}"
        if ($line.Contains("{local:LngXamlExt"))
        {
             foreach ($l in $line.Split("`""))
            {
                if ([String]::IsNullOrWhiteSpace($l))
                {
                    continue
                }
             
               # Write-Host $l -ForegroundColor Yellow
 
                $txt = $l | Get-XamlTranslatedText

                if ([String]::IsNullOrWhiteSpace($txt))
                {
                    continue
                }

                if (-not $dict.Contains($txt))
                {
                    $dict+= $txt
                }
            }
        }

    }
}


foreach($word in $dict)
{
    if (-not $alreadyTranslatedText.Contains($word))
    {
        Write-Host ($word + "=")
    }
}