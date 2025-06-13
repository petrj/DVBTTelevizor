using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT.Interop;
using LibVLCSharp.Shared;
using Border = Microsoft.UI.Xaml.Controls.Border;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using DVBTTelevizor.MAUI;
using Microsoft.Maui.Platform;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;

namespace LibVLCSharp.MAUI
{
    public class VideoViewHandler : ViewHandler<VideoView, Grid>
    {
        private static IntPtr hwnd;

        public static IPropertyMapper<VideoView, VideoViewHandler> Mapper = new PropertyMapper<VideoView, VideoViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(VideoView.MediaPlayer)] = MapMediaPlayer
        };

        public VideoViewHandler() : base(Mapper)
        {
            WeakReferenceMessenger.Default.Register<ChangedVideoPositionMessage>(this, (r, m) =>
            {
                var rect = m.Value;
                SetWindowPos(hwnd, HWND_TOP,
                    Convert.ToInt32(rect?.Left),
                    Convert.ToInt32(rect?.Top),
                    Convert.ToInt32(rect?.Width),
                    Convert.ToInt32(rect?.Height),
                    SWP_NOZORDER | SWP_NOACTIVATE);

            });
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
                        //var hwnd = WindowNative.GetWindowHandle(xamlWindow);
                        var parentHwnd = WindowNative.GetWindowHandle(xamlWindow);

                        // You must register a window class first if not using a known one.
                        // For demo: use "STATIC" which is a predefined class name.
                        hwnd = CreateWindowEx(
                            0,
                            "STATIC",
                            "",
                            0x10000000 | 0x40000000 | 0x80000000, // WS_CHILD | WS_VISIBLE | WS_POPUP
                            x: 0,
                            y: 0,
                            nWidth: 400,
                            nHeight: 300,
                            hWndParent: parentHwnd,
                            hMenu: IntPtr.Zero,
                            hInstance: GetModuleHandle(null),
                            lpParam: IntPtr.Zero
                        );

                        // Assign it to VLC
                        view.MediaPlayer.Hwnd = hwnd;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("MapMediaPlayer error: " + ex);
            }
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
    }
}