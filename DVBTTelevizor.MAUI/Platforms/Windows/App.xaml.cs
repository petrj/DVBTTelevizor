using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Notifications;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;
using RTLSDR;
using RTLSDR.DAB;

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

                WeakReferenceMessenger.Default.Send(new DriverHasBeenConnectedMessage(
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
#if DEBUG
                    WeakReferenceMessenger.Default.Send(new DriverHasBeenConnectedMessage(
                    new DVBTDriverConfiguration()
                    {
                        DeviceName = "Testing RTLSDR driver",
                        PublicDirectory = new PublicDirectoryProvider().GetPublicDirectoryPath()
                    }));
#else
                    WeakReferenceMessenger.Default.Send(new DriverHasBeenConnectedMessage(new DVBTDriverConfiguration()
                    {
                        DeviceName = "rtl_sdr bin",
                        ControlPort = 1234,
                        TransferPort = 1235,
                        PublicDirectory = new PublicDirectoryProvider().GetPublicDirectoryPath()
                    }));
#endif
                }
            });

            WeakReferenceMessenger.Default.Register<CheckDriversRequestMessage>(this, (r, m) =>
            {
                WeakReferenceMessenger.Default.Send(new CheckDriversResultMessage(new CheckDriversResult()
                {
                    DVBT = false,
                    RTLSDR = true // TODO: check if rtl_sdr is installed
                }));
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
