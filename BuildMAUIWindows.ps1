param (
    [ValidateSet("net9.0-windows10.0.22000.0", "net9.0-android35.0")]
    [string]$framework,

    [ValidateSet('Release', 'Debug')]
    [string]$configuration
)

#########################################################################################################################

if ([String]::IsNullOrWhiteSpace($framework))
{
    $framework =  (
        $MyInvocation.MyCommand.Parameters['framework'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
        ).ValidValues[0]
}

if ([String]::IsNullOrWhiteSpace($configuration))
{
    $configuration = (
        $MyInvocation.MyCommand.Parameters['configuration'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
        ).ValidValues[0]
}

#########################################################################################################################

Write-Host "Building: $framework, $configuration"

.\Clear.ps1
#Clear-Host

dotnet build .\DVBTTelevizor.MAUI\DVBTTelevizor.MAUI.csproj --framework $framework --configuration $configuration

