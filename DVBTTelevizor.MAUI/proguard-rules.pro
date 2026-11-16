-keepattributes *Annotation*,Signature,InnerClasses,EnclosingMethod

-keep class org.videolan.libvlc.** { *; }
-keep interface org.videolan.libvlc.** { *; }

-keep class **.VideoView** { *; }
-keep class **.MediaPlayer** { *; }

-keepclasseswithmembernames class * {
    native <methods>;
}
