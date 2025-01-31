namespace LibVLCSharp.MAUI
{
    public static class MauiAppBuilderibVLC
    {
        public static MauiAppBuilder UseLibVLCSharp(this MauiAppBuilder builder)
        {
            return builder;
        }
    }

    public class VideoView : BoxView
    {
        public LibVLCSharp.Shared.MediaPlayer MediaPlayer { get; set; }

        protected override void OnSizeAllocated(double width, double height)
        {
            BackgroundColor = Color.FromRgb(0, 0, 255);
            base.OnSizeAllocated(width, height);
        }
    }
}
