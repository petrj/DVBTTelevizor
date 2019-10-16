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

namespace DVBTTelevizor
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        DVBTDriverManager _driver;
        DialogService _dlgService;
        ChannelsService _channelsService;
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
            this.StartStreamButton.Clicked += StartStreamButton_Clicked;
            this.StopStreamButton.Clicked += StopStreamButton_Clicked;
            this.TestButton.Clicked += TestButton_Clicked;

            DeliverySystemPicker.SelectedIndex = 0;

            _dlgService = new DialogService(this);
            _log = new BasicLoggingService();
            _driver = new DVBTDriverManager(_log);
            _channelsService = new ChannelsService(_log, _driver);

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
            /*
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

            channelsService.Channels.Add(channel1);
            channelsService.Channels.Add(channel2);

            RunWithPermission(Permission.Storage, async () => await channelsService.Save());
            */

            Task.Run(async () =>
            {
                await RunWithPermission(Permission.Storage, async () =>
                {
                    await _channelsService.Load();
                });
            });
        }

        private void StopStreamButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = Environment.NewLine + "Stoppig streaming ...";

            Task.Run(async () =>
            {
                try
                {
                    _driver.StopStreaming();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = "Streaming stopped";
                    });
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = Environment.NewLine + $"Stoppig streaming failed ({ex.Message})";
                    });
                }
            });
        }

        private void StartStreamButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = Environment.NewLine + "Starting streaming to port 8080 ...";

            Task.Run(async () =>
            {
                try
                {
                    _driver.StartStreaming();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = "Streaming started";
                    });
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = Environment.NewLine + $"Starting streaming to port 8080 failed ({ex.Message})";
                    });
                }
            });
        }

        private void AutomaticTune_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = Environment.NewLine + "Searching channels ...";
            StatusLabel.Text += Environment.NewLine;

            Task.Run(async () =>
            {
                try
                {
                    var freq = Convert.ToInt64(EntryFrequency.Text) * 1000000;
                    var bandWidth = Convert.ToInt64(EntryBandWidth.Text) * 1000000;

                    var type = DeliverySystemPicker.SelectedIndex;

                    var tuneRes = await _driver.Tune(freq, bandWidth, type);

                    var searchMapPIDsResult = await _driver.SearchProgramMapPIDs(freq, bandWidth, type);

                    var status = String.Empty;
                    switch (searchMapPIDsResult.Result)
                    {
                        case SearchProgramResultEnum.Error:
                            status = "Search error";
                            break;
                        case SearchProgramResultEnum.NoSignal:
                            status = "No signal";
                            break;
                        case SearchProgramResultEnum.NoProgramFound:
                            status = "No program found";
                            break;
                        case SearchProgramResultEnum.OK:
                            var mapPIDs = new List<long>();
                            foreach (var sd in searchMapPIDsResult.ServiceDescriptors)
                            {
                                mapPIDs.Add(sd.Value);
                            }
                            status = $"Program MAP PIDs found: {String.Join(",",mapPIDs)}";
                            break;
                    }

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = status;
                        StatusLabel.Text += Environment.NewLine;
                    });

                    if (searchMapPIDsResult.Result != SearchProgramResultEnum.OK)
                    {
                        return;
                    }

                    // searching PIDs

                    foreach (var sDescriptor in searchMapPIDsResult.ServiceDescriptors)
                    {
                        var searchPIDsResult = await _driver.SearchProgramPIDs(Convert.ToInt32(sDescriptor.Value));

                        switch (searchPIDsResult.Result)
                        {
                            case SearchProgramResultEnum.Error:
                                status = $"Error scanning Map PID {sDescriptor.Value}";
                                break;
                            case SearchProgramResultEnum.NoSignal:
                                status = "No signal";
                                break;
                            case SearchProgramResultEnum.NoProgramFound:
                                status = "No program found";
                                break;
                            case SearchProgramResultEnum.OK:
                                var pids = string.Join(",", searchPIDsResult.PIDs);

                                status = $"{sDescriptor.Key.ServiceName}, Map PID: {sDescriptor.Value} PIDs: {pids}";
                                break;
                        }

                        Device.BeginInvokeOnMainThread(() =>
                        {

                            StatusLabel.Text += status;
                            StatusLabel.Text += Environment.NewLine;
                            _log.Info(status);
                        });
                    }

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
            var url = $"http://127.0.0.1:8080";
            MessagingCenter.Send(url, "PlayUrl");
            StatusLabel.Text = $"Playing url: {url}";
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
            RunWithPermission(Permission.Storage, async () => await _driver.StartRecording());
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

        public async Task RunWithPermission(Permission perm, List<Command> commands)
        {
            var f = new Func<Task>(
                 async () =>
                 {
                     foreach (var command in commands)
                     {
                         await Task.Run(() => command.Execute(null));
                     }
                 });

            await RunWithPermission(perm, f);
        }

        public async Task RunWithPermission(Permission perm, Func<Task> action)
        {
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(perm);
                if (status != PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(perm))
                    {
                        await _dlgService.Information("Aplikace vyžaduje potvrzení k oprávnění.", "Informace");
                    }

                    var results = await CrossPermissions.Current.RequestPermissionsAsync(perm);

                    if (results.ContainsKey(perm))
                        status = results[perm];
                }

                if (status == PermissionStatus.Granted)
                {
                    await action();
                }
                else
                {
                    await _dlgService.Error("Missing permissions", "Chyba");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

    }
}
