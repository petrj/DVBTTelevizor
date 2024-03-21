@cd /d %~dp0
cd TestingDVBTDriverData

set androidFolder=/storage/emulated/0/Android/media/net.petrjanousek.DVBTTelevizor/

adb push DVBT-MPEGTS-474MHz-2024-03-12-14-32-36.ts %androidFolder%
adb push DVBT-MPEGTS-490MHz-2024-03-12-16-06-02.ts %androidFolder%
adb push DVBT-MPEGTS-530MHz-2024-03-11-20-50-27.ts %androidFolder%
adb push DVBT-MPEGTS-626MHz-2024-03-11-21-08-27.ts %androidFolder%