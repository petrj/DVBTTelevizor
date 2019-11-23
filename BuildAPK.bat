@cd /d %~dp0
powershell.exe  -ExecutionPolicy Bypass ./BuildAPK.ps1 Debug"
powershell.exe  -ExecutionPolicy Bypass ./BuildAPK.ps1 Release"

