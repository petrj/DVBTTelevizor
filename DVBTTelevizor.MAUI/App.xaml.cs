using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;

namespace DVBTTelevizor.MAUI
{
    public partial class App : Application
    {
        public App(MainPage mp)
        {
            InitializeComponent();

            Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping("MyCustomWindowMapping", (handler, window) =>
            {
#if WINDOWS
                var nativeWindow = handler.PlatformView as Microsoft.UI.Xaml.Window;
                if (nativeWindow != null)
                {
                    // Access the AppWindow which has more comprehensive events
                    var appWindow = nativeWindow.AppWindow;
                    if (appWindow != null)
                    {
                        appWindow.Changed += OnAppWindowChanged;
                    }
                }
#endif
            });

            MainPage = new NavigationPage(mp);
        }

#if WINDOWS
        private void OnAppWindowChanged(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            if (args.DidPositionChange)
            {
                WeakReferenceMessenger.Default.Send(
                    new ChangedWindowPositionMessage(
                        new System.Drawing.Point(sender.Position.X, sender.Position.Y)));
            }

            if (args.DidSizeChange)
            {
                //
            }
        }
#endif

    }
}
