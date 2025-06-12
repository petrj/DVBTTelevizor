using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT.Interop; // <- This is key
using LibVLCSharp.Shared;
using Border = Microsoft.UI.Xaml.Controls.Border;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using DVBTTelevizor.MAUI;

namespace LibVLCSharp.MAUI
{
    public class VideoViewHandler : ViewHandler<VideoView, Grid>
    {
        public static IPropertyMapper<VideoView, VideoViewHandler> Mapper = new PropertyMapper<VideoView, VideoViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(VideoView.MediaPlayer)] = MapMediaPlayer
        };

        public VideoViewHandler() : base(Mapper)
        {
        }

        protected override Grid CreatePlatformView()
        {
            var grid = new Grid();

            grid.Loaded += (s, e) =>
            {
                // Safe HWND access here:
                var hwnd = WindowNative.GetWindowHandle(Microsoft.UI.Xaml.Window.Current);

                if (VirtualView?.MediaPlayer != null)
                    VirtualView.MediaPlayer.Hwnd = hwnd;
            };

            return grid;
        }

        public static void MapMediaPlayer(VideoViewHandler handler, VideoView view)
        {
            if (handler.PlatformView != null && view.MediaPlayer != null)
            {
                var hwnd = WindowNative.GetWindowHandle(handler.PlatformView);
                view.MediaPlayer.Hwnd = hwnd;
            }
        }
    }
}