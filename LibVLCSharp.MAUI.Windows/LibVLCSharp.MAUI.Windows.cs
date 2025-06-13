using LibVLCSharp.Shared;

namespace LibVLCSharp.MAUI
{
    public static class MauiAppBuilderibVLC
    {
        public static MauiAppBuilder UseLibVLCSharp(this MauiAppBuilder builder)
        {
            return builder;
        }
    }

    public class VideoView : View
    {
        public MediaPlayer MediaPlayer { get; set; }
    }


}
