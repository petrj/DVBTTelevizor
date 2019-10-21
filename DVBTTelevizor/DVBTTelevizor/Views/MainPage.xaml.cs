using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.IO;
using System.Threading;
using LoggerService;
using Octane.Xamarin.Forms.VideoPlayer;

namespace DVBTTelevizor
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;

        DVBTDriverManager _driver;
        DialogService _dlgService;       
        ILoggingService _log;

        public MainPage()
        {
            InitializeComponent();

            this.GetStatusButton.Clicked += GetStatusButton_Clicked;
            this.GetVersionButton.Clicked += GetVersionButton_Clicked;
            this.GetCapButton.Clicked += GetCapButton_Clicked;
            this.InitButton.Clicked += InitButton_Clicked;
            this.TuneButton.Clicked += TuneButton_Clicked;
            this.StopButton.Clicked += StopButton_Clicked;
            this.PlayButton.Clicked += PlayButton_Clicked;
            this.RecordButton.Clicked += RecordButton_Clicked;
            this.StopRecordButton.Clicked += StopRecordButton_Clicked;
            this.SetPIDsButton.Clicked += SetPIDsButton_Clicked;
            this.SearchchannelsButton.Clicked += AutomaticTune_Clicked;
            this.TestButton.Clicked += TestButton_Clicked;

            DeliverySystemPicker.SelectedIndex = 0;

            _dlgService = new DialogService(this);
            _log = new BasicLoggingService();
            _driver = new DVBTDriverManager(_log);
           

            BindingContext = _viewModel = new MainPageViewModel(_log, _dlgService, _driver);

            MessagingCenter.Subscribe<string>(this, "DVBTDriverConfiguration", (message) =>
            {
                PortsLabel.Text = message;

                _driver.Configuration.Driver = JsonConvert.DeserializeObject<DVBTDriverConfiguration>(message);

                PortsLabel.Text = $"Control port: {_driver.Configuration.Driver.ControlPort}, Transfer port: {_driver.Configuration.Driver.TransferPort}";

                _driver.Start();
            });


            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                do
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(
                        new Action(
                            delegate
                            {
                                DataStreamInfoLabel.Text = _driver.DataStreamInfo;
                            }));

                    // 2 secs delay
                    Thread.Sleep(2 * 1000);

                } while (true);
            }).Start();
        }

        private void TestButton_Clicked(object sender, EventArgs e)
        {
           
            var channel1 = new Channel()
            {
                Number = 1,
                Name = "CT 1",
                ProviderName = "Ceska televize",
                PIDs = "0, 16, 17, 258"
            };

            var channel2 = new Channel()
            {
                Number = 2,
                Name = "CT 24",
                ProviderName = "Ceska televize",
                PIDs = "0, 16, 17, 258"
            };

            var channelsService = new ChannelsService(_log, _driver);

            channelsService.Channels.Add(channel1);
            channelsService.Channels.Add(channel2);

            _viewModel.RunWithPermission(Permission.Storage, async () => await channelsService.Save());
           
        }

        private void AutomaticTune_Clicked(object sender, EventArgs e)
        {
            _viewModel.AutomaticTuneCommand.Execute(null);            
        }

        private void GetStatusButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = Environment.NewLine + "Getting status ...";

            Task.Run( async () =>
            {
                try
                {
                    var status = await _driver.GetStatus();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = status.ToString();
                    });
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
                    });
                }
            });
        }

        private void GetCapButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Getting capabilities ...";

            Task.Run(async () =>
            {
                try
                {
                    var capabalities = await _driver.GetCapabalities();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = $"Capabalities: {capabalities.ToString()}";
                    });
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = $"Request failed ({ex.Message})";
                    });
                }
            });

        }

        private void GetVersionButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Getting Version ...";

            Task.Run(async () =>
            {
                try
                {
                    var version = await _driver.GetVersion();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = $"Version: {version.ToString()}";
                    });
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text =  $"Request failed ({ex.Message})";
                    });
                }
            });
        }

        private void InitButton_Clicked(object sender, EventArgs e)
        {
            MessagingCenter.Send("", "Init");
        }

        private void PlayButton_Clicked(object sender, EventArgs e)
        {
            VideoPlayer.AutoPlay = true;

            VideoPlayer.Source = StreamVideoSource.FromStream(() =>
            {
                return new FileStream("/storage/emulated/0/Download/stream.ts", FileMode.Open);
            }, ".m2t");
            

            //VideoPlayer.Source = FileVideoSource.FromFile("/storage/emulated/0/Download/stream.ts");
            //VideoPlayer.Source = UriVideoSource.FromUri("http://vjs.zencdn.net/v/oceans.mp4");
        }

        private void StopButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "stopping driver  ...";

            Task.Run(async () =>
            {
                try
                {
                    await _driver.Stop();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = Environment.NewLine + $"Stopped";
                    });
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = $"Request failed ({ex.Message})";
                    });
                }
            });
        }

        private void RecordButton_Clicked(object sender, EventArgs e)
        {
            _viewModel.RunWithPermission(Permission.Storage, async () => await _driver.StartRecording());
        }

        private void StopRecordButton_Clicked(object sender, EventArgs e)
        {
            _driver.StopRecording();
        }

        private void SetPIDsButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Settting PIDs  ...";

            Task.Run(async () =>
            {
                try
                {
                    var pids = new List<long>();

                    foreach (var PIDAsString in EntryPIDs.Text.Split(','))
                    {
                        pids.Add(Convert.ToInt64(PIDAsString));
                    }

                    var pidRes = await _driver.SetPIDs(pids);

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = Environment.NewLine + $"PIDs Set result: {pidRes}";
                    });
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = $"Request failed ({ex.Message})";
                    });
                }
            });
        }

        private void TuneButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Tuning  ...";

            Task.Run(async () =>
            {
                try
                {
                    var freq = Convert.ToInt64(EntryFrequency.Text) * 1000000;
                    var bandWidth = Convert.ToInt64(EntryBandWidth.Text) * 1000000;

                    var type = DeliverySystemPicker.SelectedIndex;

                    var tuneRes = await _driver.Tune(freq, bandWidth, type);

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text += Environment.NewLine + $"Tune result: {tuneRes}";
                    });
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = $"Request failed ({ex.Message})";
                    });
                }
            });
        }

    

    }
}
