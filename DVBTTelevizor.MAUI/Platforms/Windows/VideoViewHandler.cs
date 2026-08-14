using Microsoft.Maui.Handlers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.Runtime.InteropServices;
using System.Threading;
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
        // Keep delegates rooted so GC does not collect them.
        private static WndProc? _wndProcDelegate;
        private static SubclassProc? _subclassProcDelegate;
        private static bool _classRegistered;
        private static bool _messageRegistered;
        // Static recipient prevents WeakReferenceMessenger from dropping the handler after GC.
        private static readonly object _positionRecipient = new object();
        private static bool _isVideoExplicitlyHidden;

        public static void SetVideoVisibility(bool visible)
        {
            _isVideoExplicitlyHidden = !visible;

            if (_videoHwnd == IntPtr.Zero)
                return;

            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_videoHwnd != IntPtr.Zero)
                    {
                        ShowWindow(_videoHwnd, visible ? SW_SHOWNA : SW_HIDE);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("VideoViewHandler.SetVideoVisibility error: " + ex);
            }
        }

        // Mouse gesture tracking
        private static bool _isMouseDown;
        private static POINT _mouseDownPos;
        private static long _mouseDownTime;
        private static bool _isDoubleClick;
        private static System.Threading.Timer? _singleClickTimer;
        private static readonly object _clickLock = new object();
        private const int SwipeThreshold = 50; // pixels

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

            WeakReferenceMessenger.Default.Register<ChangedMenuVisibilityMessage>(
                _positionRecipient,
                (_, m) =>
                {
                    SetVideoVisibility(!m.Value);
                });

            WeakReferenceMessenger.Default.Register<ChangedVideoPositionMessage>(
                _positionRecipient,
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

                                int titleBarH = GetTitleBarHeightPixels();
                                int x = (int)Math.Round(rect.Left * scale);
                                int rawY = (int)Math.Round(rect.Top * scale);
                                int y = Math.Max(rawY, titleBarH);
                                int w = Math.Max(1, (int)Math.Round(rect.Width * scale));
                                int h = Math.Max(1, (int)Math.Round((rect.Top + rect.Height) * scale) - y);

                                if (_isVideoExplicitlyHidden)
                                {
                                    SetWindowPos(_videoHwnd, HWND_TOP, x, y, w, h, SWP_NOACTIVATE | SWP_HIDEWINDOW);
                                }
                                else
                                {
                                    // HWND_TOP + no SWP_NOZORDER -> keep video on top of the XAML sibling.
                                    SetWindowPos(_videoHwnd, HWND_TOP, x, y, w, h, SWP_NOACTIVATE);
                                }

                                SubclassAllChildren(_videoHwnd);
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
                        0,
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

                view.MediaPlayer.EnableMouseInput = false;
                view.MediaPlayer.EnableKeyInput = false;
                view.MediaPlayer.Hwnd = _videoHwnd;

                SubclassAllChildren(_videoHwnd);
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
                if (msg == WM_PARENTNOTIFY && ((uint)wParam.ToInt64() & 0xFFFF) == WM_CREATE)
                {
                    SubclassChildWindow(lParam);
                }

                ProcessMouseMessage(hWnd, msg, wParam, lParam);

                return DefWindowProc(hWnd, msg, wParam, lParam);
            };

            _subclassProcDelegate = static (hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData) =>
            {
                ProcessMouseMessage(hWnd, uMsg, wParam, lParam);

                if (uMsg == WM_NCDESTROY)
                {
                    RemoveWindowSubclass(hWnd, _subclassProcDelegate!, uIdSubclass);
                }

                return DefSubclassProc(hWnd, uMsg, wParam, lParam);
            };

            var wc = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = CS_DBLCLKS,
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

        private static void SubclassChildWindow(IntPtr childHwnd)
        {
            if (childHwnd == IntPtr.Zero || _subclassProcDelegate == null)
                return;

            SetWindowSubclass(childHwnd, _subclassProcDelegate, (UIntPtr)1, UIntPtr.Zero);
        }

        private static void SubclassAllChildren(IntPtr parent)
        {
            if (parent == IntPtr.Zero)
                return;

            EnumChildWindows(parent, (childHwnd, _) =>
            {
                SubclassChildWindow(childHwnd);
                return true;
            }, IntPtr.Zero);
        }

        private static bool ProcessMouseMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_LBUTTONDOWN:
                    lock (_clickLock)
                    {
                        _isMouseDown = true;
                        _isDoubleClick = false;
                        GetCursorPos(out _mouseDownPos);
                        _mouseDownTime = Environment.TickCount64;
                        SetCapture(hWnd);
                    }
                    return true;

                case WM_LBUTTONDBLCLK:
                    lock (_clickLock)
                    {
                        _isDoubleClick = true;
                        _isMouseDown = false;
                        CancelSingleClickTimer();
                        ReleaseCapture();
                        DispatchGesture(VideoGestureType.DoubleClick);
                    }
                    return true;

                case WM_LBUTTONUP:
                    lock (_clickLock)
                    {
                        if (_isDoubleClick)
                        {
                            _isDoubleClick = false;
                            _isMouseDown = false;
                            ReleaseCapture();
                            return true;
                        }

                        if (!_isMouseDown)
                        {
                            ReleaseCapture();
                            return false;
                        }

                        _isMouseDown = false;
                        ReleaseCapture();

                        GetCursorPos(out POINT currentPos);
                        int dx = currentPos.X - _mouseDownPos.X;
                        int dy = currentPos.Y - _mouseDownPos.Y;
                        long elapsed = Environment.TickCount64 - _mouseDownTime;

                        // Check for horizontal swipe (at least 50px, mostly horizontal, within 1500ms)
                        if (Math.Abs(dx) >= SwipeThreshold && Math.Abs(dx) > Math.Abs(dy) && elapsed < 1500)
                        {
                            CancelSingleClickTimer();
                            if (dx < 0)
                            {
                                DispatchGesture(VideoGestureType.LeftSwipe);
                            }
                            else
                            {
                                DispatchGesture(VideoGestureType.RightSwipe);
                            }
                        }
                        else if (Math.Abs(dx) < 20 && Math.Abs(dy) < 20)
                        {
                            // Single click candidate - delay to differentiate from double click
                            CancelSingleClickTimer();
                            int dblClickTime = (int)Math.Min(GetDoubleClickTime(), 300);
                            _singleClickTimer = new System.Threading.Timer(_ =>
                            {
                                lock (_clickLock)
                                {
                                    _singleClickTimer?.Dispose();
                                    _singleClickTimer = null;
                                }
                                DispatchGesture(VideoGestureType.Click);
                            }, null, dblClickTime, Timeout.Infinite);
                        }
                    }
                    return true;
            }

            return false;
        }

        private static void CancelSingleClickTimer()
        {
            _singleClickTimer?.Dispose();
            _singleClickTimer = null;
        }

        private static void DispatchGesture(VideoGestureType gesture)
        {
            try
            {
                WeakReferenceMessenger.Default.Send(new VideoGestureMessage(gesture));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error dispatching video gesture: {ex}");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
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
        public delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData);
        public delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

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

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern uint GetDoubleClickTime();

        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        [DllImport("comctl32.dll", SetLastError = true)]
        public static extern bool SetWindowSubclass(
            IntPtr hWnd,
            SubclassProc pfnSubclass,
            UIntPtr uIdSubclass,
            UIntPtr dwRefData);

        [DllImport("comctl32.dll", SetLastError = true)]
        public static extern IntPtr DefSubclassProc(
            IntPtr hWnd,
            uint uMsg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("comctl32.dll", SetLastError = true)]
        public static extern bool RemoveWindowSubclass(
            IntPtr hWnd,
            SubclassProc pfnSubclass,
            UIntPtr uIdSubclass);

        // Returns the title-bar height in raw pixels; non-zero only when content is extended into the title bar.
        private static int GetTitleBarHeightPixels()
        {
            if (_parentHwnd == IntPtr.Zero) return 0;
            try
            {
                var windowId = Win32Interop.GetWindowIdFromWindow(_parentHwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                return appWindow.TitleBar.Height;
            }
            catch { return 0; }
        }

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_SHOWNA = 8;

        private const uint SWP_HIDEWINDOW = 0x0080;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_NOACTIVATE = 0x0010;

        private const uint WS_CHILD = 0x40000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_CLIPSIBLINGS = 0x04000000;

        private const uint CS_DBLCLKS = 0x0008;

        private const uint WM_CREATE = 0x0001;
        private const uint WM_NCDESTROY = 0x0082;
        private const uint WM_PARENTNOTIFY = 0x0210;

        private const uint WM_MOUSEMOVE = 0x0200;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_LBUTTONDBLCLK = 0x0203;
    }
}