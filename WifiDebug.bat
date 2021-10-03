@rem https://docs.microsoft.com/cs-cz/xamarin/android/get-started/installation/set-up-device-for-development
@rem adb kill-server
adb devices
adb tcpip 5555
adb connect 10.0.0.15:5555
@rem adb usb


@rem https://www.andreafortuna.org/2018/05/28/how-to-install-and-run-tcpdump-on-android-devices/
@rem adb shell
@rem tcpdump -D
@rem tcpdump -vv -i any -s 0 -w /sdcard/dump.pcap
