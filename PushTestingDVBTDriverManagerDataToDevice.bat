@cd /d %~dp0
cd TestingDVBTDriverManagerData

set androidFolder=/storage/emulated/0/Android/media/net.petrjanousek.DVBTTelevizor/

adb push *.* %androidFolder%
