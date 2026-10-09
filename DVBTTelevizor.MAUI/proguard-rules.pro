# ====================================================================
# .NET MAUI & Xamarin Android Runtime Rules
# ====================================================================

# Keep essential runtime classes and wrappers for .NET Android
-keep class mono.MonoRuntimeProvider { *; }
-keep class mono.MonoAOTHelper { *; }
-keep class mono.android.** { *; }
-keep class com.xamarin.** { *; }
-keep class microsoft.maui.** { *; }

# Preserve attributes required for reflection, exceptions, and generics
-keepattributes *Annotation*,Signature,InnerClasses,EnclosingMethod

# Suppress warnings for missing optional AndroidX/Support classes
-dontwarn android.support.**
-dontwarn androidx.**


# ====================================================================
# LibVLC & LibVLCSharp Complete Exclusions (Video & Audio Playback)
# ====================================================================

# Completely skip shrinking, optimization, and obfuscation for LibVLC Java/Android libraries
-keep class org.videolan.** { *; }
-keep interface org.videolan.** { *; }
-dontwarn org.videolan.**

# Completely keep LibVLCSharp C# Android bindings & custom views if present in Java wrappers
-keep class libvlcsharp.** { *; }
-keep interface libvlcsharp.** { *; }
-dontwarn libvlcsharp.**

# Preserve all VideoView / Video player wrapper classes
-keep class **.VideoView** { *; }
-keep class **.MediaPlayer** { *; }


# ====================================================================
# Android View, Surface & Graphics Rules (Video Rendering Protection)
# ====================================================================

# Keep all Android Surface and View components used by LibVLC AWindow/Vout
-keep class android.view.Surface { *; }
-keep class android.view.SurfaceHolder** { *; }
-keep class android.view.SurfaceView { *; }
-keep class android.view.TextureView** { *; }
-keep class android.graphics.SurfaceTexture { *; }
-keep class android.opengl.** { *; }


# ====================================================================
# JNI & Native Methods Protection (Critical for LibVLC C/C++ <-> Java)
# ====================================================================

# Preserve all classes containing native methods so JNI calls remain intact
-keepclasseswithmembernames class * {
    native <methods>;
}