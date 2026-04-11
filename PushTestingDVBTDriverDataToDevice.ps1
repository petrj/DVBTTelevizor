Param($device)

Write-Host "Pushing test driver data to Android device $device"

cd $PSScriptRoot

$androidFolder="/storage/emulated/0/Android/media/net.petrjanousek.DVBTTelevizor/"

foreach ($file in Get-ChildItem -Path "TestingDVBTDriverData")
{   
    $adbPath =  "C:\'Program Files (x86)'\Android\android-sdk\platform-tools\adb.exe"

    $cmd =  "$adbPath -s $device push $($file.FullName) $androidFolder"    
    
    Write-Host $cmd

    if (-not ([String]::IsNullOrWhiteSpace($device)))
    {        
        Invoke-Expression $cmd
        
    } else
    {
        Invoke-Expression "$adbPath push $($file.FullName) $androidFolder"
    }
}


 
