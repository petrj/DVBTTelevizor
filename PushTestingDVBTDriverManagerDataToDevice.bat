@cd /d %~dp0
cd TestingDVBTDriverManagerData

set androidFolder=/storage/emulated/0/Android/media/net.petrjanousek.DVBTTelevizor/

adb push stream.ts %androidFolder%
adb push 310.ts %androidFolder%
adb push 2300.ts %androidFolder%
adb push 2400.ts %androidFolder%
adb push 7070.ts %androidFolder%