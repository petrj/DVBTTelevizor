cd $PSScriptRoot

$androidFolder="/storage/emulated/0/Android/media/net.petrjanousek.DVBTTelevizor.MAUI/"

foreach ($file in Get-ChildItem -Path "TestingDVBTDriverData")
{    
    Invoke-Expression "adb push $($file.FullName) $androidFolder"
}

#Invoke-Expression "adb push TestingDVBTDriverData/DVBT-MPEGTS-514MHz-2023-08-15-23-13-38.ts $androidFolder"

 
