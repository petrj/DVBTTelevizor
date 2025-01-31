using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;
using Microsoft.Graphics.Canvas.Printing;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.UI.Xaml;
using SharpHook;
using System.Threading;
using Windows.Networking.Vpn;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DVBTTelevizor.MAUI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        private ILoggingService _loggingService;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // TODO: add NLOG - app cannot have any argument
            _loggingService = new BasicLoggingService();
            this.InitializeComponent();

            var hook = new SharpHook.TaskPoolGlobalHook();
            hook.KeyPressed += Hook_KeyPressed; ;       // EventHandler<KeyboardHookEventArgs>

            Task.Run(async () =>
            {
            //    await hook.RunAsync();
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverTestConnectMessage>(this, (r, m) =>
            {
                var testDVBTDriver = new TestDVBTDriver(_loggingService);
                testDVBTDriver.PublicDirectory = new PublicDirectoryProvider().GetPublicDirectoryPath();
                testDVBTDriver.Connect();

                WeakReferenceMessenger.Default.Send(new DVBTDriverConnectedMessage(
                    new DVBTDriverConfiguration()
                    {
                        DeviceName = "Testing device",
                        ControlPort = testDVBTDriver.ControlIPEndPoint.Port,
                        TransferPort = testDVBTDriver.TransferIPEndPoint.Port
                    }));
            });

            WeakReferenceMessenger.Default.Register<RemoteKeyPlatformActionMessage>(this, (r, m) =>
            {
                WeakReferenceMessenger.Default.Send(new KeyDownMessage(m.Value));
            });

        }

        private void Hook_KeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            var code = e.Data.KeyCode.ToString();
            if (code.StartsWith("Vc"))
            {
                code = code.Substring(2);
            }
            var keyAction = KeyboardDeterminer.GetKeyAction(code);

            WeakReferenceMessenger.Default.Send(new KeyDownMessage(code));
        }

        protected override MauiApp CreateMauiApp()
        {
            var app = MauiProgram.CreateMauiApp();

            return app;
        }
    }

}
