@rem https://docs.microsoft.com/cs-cz/xamarin/android/get-started/installation/set-up-device-for-development
@rem adb kill-server
adb devices
adb tcpip 5555
@rem adb connect 192.168.1.164:5555
@rem adb connect 192.168.1.204:5555
@rem adb connect 10.0.0.13:5555
#adb connect 10.0.0.16:5555
adb connect 10.0.0.18:5555
@rem adb usb


@rem https://www.andreafortuna.org/2018/05/28/how-to-install-and-run-tcpdump-on-android-devices/
@rem adb shell
@rem tcpdump -D
@rem tcpdump -vv -i any -s 0 -w /sdcard/dump.pcap
