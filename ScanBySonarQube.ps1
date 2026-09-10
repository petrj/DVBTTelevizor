Set-Location $PSScriptRoot

./LoadBuildModule.ps1

$token = Get-SecureStringFromUserInput -Message "Enter SonarQube token:" -EnvironmentVariable $env:SONAR_TOKEN
$url = Get-SecureStringFromUserInput -Message "Enter SonarQube project url:" -EnvironmentVariable $env:SONAR_URL

Write-Host "dir: $PSScriptRoot"


Invoke-SonarAnalysis -Token $token -ProjectKey "DVBTTelevizor" -Url $url -WorkingDirectory  $PSScriptRoot


Export-SonarQubeAnalysis -ProjectKey "DVBTTelevizor" -ServerUrl $env:SONAR_URL -Token $env:SONAR_TOKEN 