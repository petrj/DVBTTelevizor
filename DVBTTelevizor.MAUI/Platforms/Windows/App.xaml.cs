using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Notifications;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;
using Microsoft.Graphics.Canvas.Printing;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.UI.Xaml;
using RTLSDR;
using System.Threading;
using Windows.Data.Xml.Dom;
using Windows.Networking.Vpn;
using Windows.UI.Core;
using Windows.UI.Notifications;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            _loggingService = new LoggerProvider().GetLoggingService();
            this.InitializeComponent();

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

            WeakReferenceMessenger.Default.Register<OpenURLMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Browser.OpenAsync(m.Value, BrowserLaunchMode.External);
                });
            });

            WeakReferenceMessenger.Default.Register<OpenMailMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Email.Default.ComposeAsync(new EmailMessage
                    {
                        To = new List<string> { "petrjanousek.net@gmail.com" }
                    });
                });
            });

            WeakReferenceMessenger.Default.Register<ToastMessage>(this, (r, m) =>
            {
                ShowToastMessage(m.Value);
            });

            WeakReferenceMessenger.Default.Register<SetUDPLoggingIPMessage>(this, (r, m) =>
            {
                if (_loggingService != null &&
                _loggingService is NLogLoggingService nlogService)
                {
                    nlogService.GetConfiguration().FindTargetByName<NLog.Targets.NetworkTarget>("udp").Address = m.Value;
                }
            });

            WeakReferenceMessenger.Default.Register<RTLSDRDriverConnectMessage>(this, (sender, obj) =>
            {
                if (obj.Value is DriverSettings settings)
                {
                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectedMessage(new DVBTDriverConfiguration()
                    {
                        DeviceName = "rtl_sdr bin",
                        ControlPort = 1234,
                        TransferPort = 1235,
                        PublicDirectory = new PublicDirectoryProvider().GetPublicDirectoryPath()
                    }));
                }
            });

            UnhandledException += App_UnhandledException;
        }

        private void ShowToastMessage(string msg)
        {
            new ToastContentBuilder()
                .AddText("DVBT Televizor")
                .AddText(msg)
                .Show();
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            _loggingService.Error(e.Exception);
        }

        protected override MauiApp CreateMauiApp()
        {
            var app = MauiProgram.CreateMauiApp();

            return app;
        }
    }

}
