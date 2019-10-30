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
using Android.Media;
using LibVLCSharp.Shared;
using Android.Widget;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ServicePage : ContentPage
    {
        private ServicePageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected DVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;

        public ServicePage(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _config = config;

            BindingContext = _viewModel = new ServicePageViewModel(_loggingService, _dialogService, _driver, _config);

            this.InitButton.Clicked += InitButton_Clicked;
            this.DisconnectButton.Clicked += DisconnectButton_Clicked;
            this.TuneButton.Clicked += TuneButton_Clicked;
            this.GetStatusButton.Clicked += GetStatusButton_Clicked;
            this.GetVersionButton.Clicked += GetVersionButton_Clicked;
            this.GetCapButton.Clicked += GetCapButton_Clicked;
            this.RecordButton.Clicked += RecordButton_Clicked;
            this.StopRecordButton.Clicked += StopRecordButton_Clicked;
            this.SetPIDsButton.Clicked += SetPIDsButton_Clicked;
            this.SearchchannelsButton.Clicked += AutomaticTune_Clicked;
            this.StopReadStreamButton.Clicked += StopReadStreamButton_Clicked;
            this.StartReadStreamButton.Clicked += StartReadStreamButton_Clicked;
            this.AddChannelsButton.Clicked += AddChannelsButton_Clicked;
            this.TestButton.Clicked += TestButton_Clicked;

            DeliverySystemPicker.SelectedIndex = 0;

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

        private void InitButton_Clicked(object sender, EventArgs e)
        {
            MessagingCenter.Send("", "Init");
        }

        private void DisconnectButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Disconnecting driver  ...";

            Task.Run(async () =>
            {
                try
                {
                    await _driver.Disconnect();

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

        private void AutomaticTune_Clicked(object sender, EventArgs e)
        {
            _viewModel.TuneCommand.Execute(null);
        }

        private void GetStatusButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = Environment.NewLine + "Getting status ...";

            Task.Run(async () =>
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
                        StatusLabel.Text = $"Request failed ({ex.Message})";
                    });
                }
            });
        }
        private void RecordButton_Clicked(object sender, EventArgs e)
        {
            _viewModel.RunWithStoragePermission(async () => await _driver.StartRecording());
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

        private void StopReadStreamButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Stop read stream ...";

            Task.Run(async () =>
            {
                try
                {
                    _driver.StopReadStream();
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

        private void StartReadStreamButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Start read stream ...";

            Task.Run(async () =>
            {
                try
                {
                    _driver.StartReadStream();
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

        private void TestButton_Clicked(object sender, EventArgs e)
        {
            _viewModel.RunWithStoragePermission(async () => { _loggingService.Info("test"); });
        }

        private void AddChannelsButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Adding channels ...";

            Task.Run(async () =>
            {
                try
                {
                    var res = await _viewModel.SaveTunedChannels();

                    if (res>0)
                        StatusLabel.Text = $"Channels added ({res})";
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