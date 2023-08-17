@cd /d %~dp0
cd TestingDVBTDriverData

set androidFolder=/storage/emulated/0/Android/media/net.petrjanousek.DVBTTelevizor/

adb push DVBT-MPEGTS-514MHz-2023-08-15-23-13-38.ts %androidFolder%