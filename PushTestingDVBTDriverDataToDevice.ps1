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
        Invoke-Expression "adb push $($file.FullName) $androidFolder"
    }
}

#Invoke-Expression "adb push TestingDVBTDriverData/DVBT-MPEGTS-514MHz-2023-08-15-23-13-38.ts $androidFolder"

 
