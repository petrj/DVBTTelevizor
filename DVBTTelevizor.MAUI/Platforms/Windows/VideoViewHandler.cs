using Microsoft.Maui.Handlers;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI;
using DVBTTelevizor.MAUI.Messages;
using Grid = Microsoft.UI.Xaml.Controls.Grid;

namespace LibVLCSharp.MAUI
{
    /// <summary>
    /// Windows-only handler that embeds the LibVLC output as a child HWND inside the
    /// MAUI main window instead of opening a separate top-level window.
    /// Android is unaffected (it uses the official LibVLCSharp.MAUI NuGet handler).
    /// </summary>
    public class VideoViewHandler : ViewHandler<VideoView, Grid>
    {
        // Single embedded child HWND that VLC renders into.
        private static IntPtr _videoHwnd = IntPtr.Zero;
        private static IntPtr _parentHwnd = IntPtr.Zero;
        // Keep the WndProc delegate rooted so the GC does not collect it.
        private static WndProc? _wndProcDelegate;
        private static bool _classRegistered;
        private static bool _messageRegistered;

        private const string ClassName = "DVBTTelevizorEmbeddedVLCClass";

        public static IPropertyMapper<VideoView, VideoViewHandler> Mapper = new PropertyMapper<VideoView, VideoViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(VideoView.MediaPlayer)] = MapMediaPlayer
        };

        public VideoViewHandler() : base(Mapper)
        {
            RegisterPositionMessageHandler();
        }

        protected override Grid CreatePlatformView() => new Grid();

        private static void RegisterPositionMessageHandler()
        {
            if (_messageRegistered)
                return;
            _messageRegistered = true;

            WeakReferenceMessenger.Default.Register<ChangedVideoPositionMessage>(
                new object(),
                (_, m) =>
                {
                    if (_videoHwnd == IntPtr.Zero || _parentHwnd == IntPtr.Zero || m.Value == null)
                        return;

                    var rect = m.Value.Value;

                    try
                    {
                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                double scale = GetDpiForWindow(_parentHwnd) / 96.0;
                                if (scale <= 0) scale = 1.0;

                                int x = (int)Math.Round(rect.Left * scale);
                                int y = (int)Math.Round(rect.Top * scale);
                                int w = Math.Max(1, (int)Math.Round(rect.Width * scale));
                                int h = Math.Max(1, (int)Math.Round(rect.Height * scale));

                                // HWND_TOP + no SWP_NOZORDER -> keep video on top of the XAML sibling.
                                SetWindowPos(_videoHwnd, HWND_TOP, x, y, w, h, SWP_NOACTIVATE);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("VideoViewHandler position update failed: " + ex);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("VideoViewHandler position dispatch failed: " + ex);
                    }
                });
        }

        public static void MapMediaPlayer(VideoViewHandler handler, VideoView view)
        {
            try
            {
                if (handler.PlatformView == null || view.MediaPlayer == null)
                    return;

                if (_videoHwnd == IntPtr.Zero)
                {
                    var platformWindow = App.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView;
                    if (platformWindow is not Microsoft.UI.Xaml.Window xamlWindow)
                        return;

                    _parentHwnd = WindowNative.GetWindowHandle(xamlWindow);

                    EnsureClassRegistered();

                    _videoHwnd = CreateWindowEx(
                        WS_EX_TRANSPARENT,           // let the XAML sibling receive mouse input
                        ClassName,
                        string.Empty,
                        WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS,
                        0, 0, 1, 1,                  // sized to nothing until UpdateVideoWindowPosition fires
                        _parentHwnd,
                        IntPtr.Zero,
                        GetModuleHandle(null),
                        IntPtr.Zero);

                    if (_videoHwnd == IntPtr.Zero)
                    {
                        throw new System.ComponentModel.Win32Exception(
                            Marshal.GetLastWin32Error(),
                            "Failed to create embedded VLC child window");
                    }
                }

                view.MediaPlayer.Hwnd = _videoHwnd;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("VideoViewHandler.MapMediaPlayer error: " + ex);
            }
        }

        private static void EnsureClassRegistered()
        {
            if (_classRegistered)
                return;

            _wndProcDelegate = static (hWnd, msg, wParam, lParam) =>
            {
                // Make the child mouse-transparent so MAUI gesture recognizers still fire.
                if (msg == WM_NCHITTEST)
                    return (IntPtr)HTTRANSPARENT;

                return DefWindowProc(hWnd, msg, wParam, lParam);
            };

            var wc = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = GetModuleHandle(null),
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null!,
                lpszClassName = ClassName,
                hIconSm = IntPtr.Zero
            };

            if (RegisterClassEx(ref wc) == 0)
            {
                int err = Marshal.GetLastWin32Error();
                // ERROR_CLASS_ALREADY_EXISTS (1410) is fine, e.g. after hot reload.
                if (err != 1410)
                    throw new System.ComponentModel.Win32Exception(err, "RegisterClassEx failed");
            }

            _classRegistered = true;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

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
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_NOACTIVATE = 0x0010;

        private const uint WS_CHILD = 0x40000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_CLIPSIBLINGS = 0x04000000;

        private const int WS_EX_TRANSPARENT = 0x00000020;

        private const uint WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;
    }
}