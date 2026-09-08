# Loading Powershell.Modules BuildModule

$h = $env:HOME
if ([string]::IsNullOrWhiteSpace($h))
{
    $h = $HOME
}

$maxVersion = Get-ChildItem "$h/.nuget/packages/powershell.modules/" | Select-Object -Property Name -ExpandProperty Name | sort-object -Descending | Select-Object -First 1
$modulePath = "$h/.nuget/packages/powershell.modules/$maxVersion/Powershell.Modules/"


if (Get-Module -Name BuildModule)
{
    Write-Host "Reloading BuildModule module version $maxVersion..."
    Remove-Module BuildModule
} else
{
    Write-Host "Loading BuildModule module version $maxVersion from $modulePath ..."
}

Import-Module $modulePath/BuildModule/BuildModule.psd1
