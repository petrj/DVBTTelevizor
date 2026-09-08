Set-Location $PSScriptRoot

./LoadBuildModule.ps1

$token = Get-SecureStringFromUserInput -Message "Enter SonarQube token:" -EnvironmentVariable $env:SONAR_TOKEN
$key = Get-SecureStringFromUserInput -Message "Enter SonarQube project key:" -EnvironmentVariable $env:SONAR_KEY_DVBTTELEVIZOR
$url = Get-SecureStringFromUserInput -Message "Enter SonarQube project url:" -EnvironmentVariable $env:SONAR_URL

Invoke-SonarAnalysis -Token $token -ProjectKey $key -Url $url -WorkingDirectory  $PSScriptRoot
