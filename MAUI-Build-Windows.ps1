param ([string]$framework,[string]$configuration)

$frameworks = @("net9.0-windows10.0.22000.0", "net9.0-android35.0")
$configurations = @("Release", "Debug")

#########################################################################################################################

function Get-Value
{
    Param
    (       
        $Title,
        $Values
    )
    Process
    {
        Write-Host $Title
        $i = 1
        foreach ($value in $Values)
        {
            Write-Host ($i.TosTring() + ") " + $value)
            $i++
        }

        $v = Read-Host
        $num = 0

        if (-not ([int]::TryParse($v, [ref] $num)))
        {
            throw "Invalid value"
        } 

        if (($num -lt 1) -or ($num -gt $Values.Count))
        {
            throw "Invalid value"
        }
        
        return ($Values[$num-1])
    }
}

if ([String]::IsNullOrWhiteSpace($framework))
{
    $framework =  $frameworks[0]
}

if ([String]::IsNullOrWhiteSpace($configuration))
{
    $configuration = $configurations[0]
}

if ($framework -eq "?")
{
    $framework = Get-Value -Title "Set framework:" -Values $frameworks
}

if ($configuration -eq "?")
{
    $configuration = Get-Value -Title "Set configuration:" -Values $configurations
}

if (-not ($configurations.Contains($configuration)))
{
    $configuration = $configurations | Where-Object { $_ -like "*$configuration*"} 
}

if (-not ($configurations.Contains($configuration)))
{
    throw "Invalid configuration"
}

if (-not ($frameworks.Contains($framework)))
{
    $framework = $frameworks | Where-Object { $_ -like "*$framework*"}
}

if (-not ($frameworks.Contains($framework)))
{
    throw "Invalid framework"
}

#########################################################################################################################

Write-Host "Building: $framework, $configuration"

.\Clear.ps1
#Clear-Host

dotnet build .\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj --framework $framework --configuration $configuration

