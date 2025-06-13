using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT.Interop;
using LibVLCSharp.Shared;
using Border = Microsoft.UI.Xaml.Controls.Border;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using DVBTTelevizor.MAUI;
using Microsoft.Maui.Platform;

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
            return new Grid();
        }

        public static void MapMediaPlayer(VideoViewHandler handler, VideoView view)
        {
            try
            {
                if (handler.PlatformView != null && view.MediaPlayer != null)
                {
                    var platformWindow = App.Current.Windows.FirstOrDefault()?.Handler?.PlatformView;

                    if (platformWindow is Microsoft.UI.Xaml.Window xamlWindow)
                    {
                        var hwnd = WindowNative.GetWindowHandle(xamlWindow);
                        view.MediaPlayer.Hwnd = hwnd;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("MapMediaPlayer error: " + ex);
            }
        }
    }
}