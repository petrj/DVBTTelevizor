#adb install DVBTTelevizor.MAUI/bin/Debug/net9.0-android35.0/net.petrjanousek.DVBTTelevizor.MAUI-armeabi-v7a-Signed.apk
#adb install DVBTTelevizor.MAUI/bin/Debug/net9.0-android35.0/net.petrjanousek.DVBTTelevizor.MAUI-arm64-v8a-Signed.apk
adb uninstall net.petrjanousek.DVBTTelevizor.MAUI
adb install DVBTTelevizor.MAUI/bin/Release/net9.0-android35.0/net.petrjanousek.DVBTTelevizor.MAUI-Signed.apk
adb shell monkey -p net.petrjanousek.DVBTTelevizor.MAUI -c android.intent.category.LAUNCHER 1

