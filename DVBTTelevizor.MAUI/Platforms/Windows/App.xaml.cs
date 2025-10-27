using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Notifications;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;
using Microsoft.Graphics.Canvas.Printing;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.UI.Xaml;
using NAudio.Wave;
using RTLSDR;
using RTLSDR.Common;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Speech.AudioFormat;
using System.Threading;
using Windows.Data.Xml.Dom;
using Windows.Networking.Vpn;
using Windows.UI.Core;
using Windows.UI.Notifications;

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
        private bool _audioThreadRunning = false;

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

                    WeakReferenceMessenger.Default.Send(new PlayRawAdioMessage(System.String.Empty));
                    PlayRawAudio();
                }
            });

            UnhandledException += App_UnhandledException;
        }

        private void PlayRawAudio()
        {
            var audioDescription = new AudioDataDescription()
            {
                BitsPerSample = 16,
                Channels = 2,
                SampleRate = 96000
            };

            var outputDevice = new WaveOutEvent();
            var waveFormat = new WaveFormat(audioDescription.SampleRate, audioDescription.BitsPerSample, audioDescription.Channels);
            var bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
            //_bufferedWaveProvider.BufferDuration = new TimeSpan(0,0,10);
            //_bufferedWaveProvider.BufferLength = 10 * (audioDescription.SampleRate * audioDescription.Channels * audioDescription.BitsPerSample / 8);

            outputDevice.Init(bufferedWaveProvider);

            Task.Run(() =>
            {
                try
                {
                    var remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8012);
                    using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        client.Bind(remoteEP);

                        var packetBuffer = new byte[UDPStreamer.MaxPacketSize];

                        _audioThreadRunning = true;

                        while (_audioThreadRunning)
                        {
                            if (client.Available > 0)
                            {
                                var bytesRead = client.Receive(packetBuffer);
                                bufferedWaveProvider.AddSamples(packetBuffer, 0, bytesRead);
                            }
                            else
                            {
                                Thread.Sleep(18);
                            }
                        }

                        //_audioPlayer.Stop();
                        //_audioPlayer = null;
                        client.Close();
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
            });
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
