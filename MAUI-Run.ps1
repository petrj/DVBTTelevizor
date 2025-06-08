# crate emulator:
./avdmanager create avd -n Pixel_34 -k "system-images;android-34;google_apis;x86_64" -d "pixel"

# run emulator:
./emulator -avd Pixel_34

adb uninstall net.petrjanousek.DVBTTelevizor.MAUI
adb install DVBTTelevizor.MAUI/bin/Release/net9.0-android35.0/net.petrjanousek.DVBTTelevizor.MAUI-x86_64-Signed.apk
adb shell monkey -p net.petrjanousek.DVBTTelevizor.MAUI -c android.intent.category.LAUNCHER 1

