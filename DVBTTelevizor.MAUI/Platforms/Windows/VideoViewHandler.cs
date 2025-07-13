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
            // updating audio/video window position

            /*
            WeakReferenceMessenger.Default.Register<ChangedVideoPositionMessage>(this, (r, m) =>
            {
                var platformWindow = App.Current.Windows.FirstOrDefault()?.Handler?.PlatformView;

                if (platformWindow is Microsoft.UI.Xaml.Window xamlWindow)
                {
                    //var hwnd = WindowNative.GetWindowHandle(xamlWindow);
                    var parentHwnd = WindowNative.GetWindowHandle(xamlWindow);

                    int titleBarHeight = GetSystemMetrics(4);  // 4 = SM_CYCAPTION

                    RECT parentWindowRectange;
                    GetWindowRect(parentHwnd, out parentWindowRectange);

                    var rect = m.Value;
                    SetWindowPos(hwnd, HWND_TOP,
                        Convert.ToInt32(parentWindowRectange.Left + rect?.Left),
                        Convert.ToInt32(parentWindowRectange.Top + titleBarHeight + rect?.Top),
                        Convert.ToInt32(rect?.Width),
                        Convert.ToInt32(rect?.Height),
                        SWP_NOZORDER | SWP_NOACTIVATE);
                }
            });
            */
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
                        WndProc wndProcDelegate = (hWnd, msg, wParam, lParam) =>
                        {
                            switch (msg)
                            {
                                case 0x0010: // WM_CLOSE
                                             // Your custom logic before window is destroyed
                                    Console.WriteLine("Video window is closing!");
                                    // Call DestroyWindow manually if you want to proceed
                                    //DestroyWindow(hWnd);
                                break;

                                case 0x0002: // WM_DESTROY
                                             // Cleanup logic here
                                    Console.WriteLine("Video window has been closed!");
                                    //PostQuitMessage(0); // Signals to end the message loop
                                break;
                            }

                            return DefWindowProc(hWnd, msg, wParam, lParam);
                        };

                        WNDCLASSEX windowClass = new WNDCLASSEX()
                        {
                            cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                            style = 0,
                            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate),
                            cbClsExtra = 0,
                            cbWndExtra = 0,
                            hInstance = GetModuleHandle(null),
                            hIcon = IntPtr.Zero,
                            hCursor = IntPtr.Zero,
                            hbrBackground = IntPtr.Zero,
                            lpszMenuName = null,
                            lpszClassName = "MyCustomWindowClass",
                            hIconSm = IntPtr.Zero
                        };

                        ushort regResult = RegisterClassEx(ref windowClass);

                        if (regResult == 0)
                        {
                            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to register window class");
                        }

                        IntPtr hwnd = CreateWindowEx(
                                WS_EX_APPWINDOW,
                                "MyCustomWindowClass",
                                "My Independent Window",
                                WS_OVERLAPPEDWINDOW | WS_VISIBLE,
                                100, 100, 800, 600,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                GetModuleHandle(null),
                                IntPtr.Zero
                            );


                        /*
                        //var hwnd = WindowNative.GetWindowHandle(xamlWindow);
                        var parentHwnd = WindowNative.GetWindowHandle(xamlWindow);

                        // You must register a window class first if not using a known one.
                        // For demo: use "STATIC" which is a predefined class name.
                        hwnd = CreateWindowEx(
                            0,
                            "STATIC",
                            "DVBTTelevizor",
                            WS_OVERLAPPEDWINDOW | WS_VISIBLE, // dwStyle — standard window with title bar
                            x: 0,
                            y: 0,
                            nWidth: 400,
                            nHeight: 300,
                            hWndParent: IntPtr.Zero, // hWndParent — must be NULL for independent windows,
                            hMenu: IntPtr.Zero,
                            hInstance: GetModuleHandle(null),
                            lpParam: IntPtr.Zero
                        );
                        */
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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

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

        // Window procedure delegate
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

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        private const uint WS_VISIBLE = 0x10000000;
        //private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        //private const uint WS_VISIBLE = 0x10000000;
        private const int WS_EX_APPWINDOW = 0x00040000;
    }
}