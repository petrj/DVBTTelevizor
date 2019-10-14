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

namespace DVBTTelevizor
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        DVBTDriverManager _driver;
        DialogService _dlgService;

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
            this.SearchchannelsButton.Clicked += SearchchannelsButton_Clicked;


            DeliverySystemPicker.SelectedIndex = 0;

            _driver = new DVBTDriverManager();
            _dlgService = new DialogService(this);

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

        private void SearchchannelsButton_Clicked(object sender, EventArgs e)
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

                    if (tuneRes.SuccessFlag)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            StatusLabel.Text += $"Tuned freq {EntryFrequency.Text} Mhz (Bandwidth {EntryBandWidth.Text}, {DeliverySystemPicker.SelectedItem})";
                            StatusLabel.Text += Environment.NewLine;
                        });

                        // waiting
                        System.Threading.Thread.Sleep(1000);

                        var status = await _driver.GetStatus();

                        if (status.SuccessFlag)
                        {
                            if (status.hasSignal == 1 && status.hasSync == 1 && status.hasLock == 1)
                            {
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    StatusLabel.Text += "Signal locked, frequency tuned successfully";
                                    StatusLabel.Text += "Setting PID filter 0,16,17 ....";
                                    StatusLabel.Text += Environment.NewLine;
                                });

                                // waiting 
                                System.Threading.Thread.Sleep(1000);

                                var pids = new List<long>() { 0, 16, 17 };
                                var pidRes = await _driver.SetPIDs(pids);

                                if (pidRes.SuccessFlag)
                                {
                                    Device.BeginInvokeOnMainThread(() =>
                                    {
                                        StatusLabel.Text += "Scanning SDT and PSI tables (10s)....";
                                        StatusLabel.Text += Environment.NewLine;
                                    });

                                     _driver.StartReadBuffer();

                                    // waiting 
                                    System.Threading.Thread.Sleep(10000);

                                     _driver.StopReadBuffer();

                                    if (_driver.Buffer.Count > 0 )
                                    {
                                        Device.BeginInvokeOnMainThread(() =>
                                        {
                                            StatusLabel.Text += $"Bytes read: {_driver.Buffer.Count}";
                                            StatusLabel.Text += Environment.NewLine;
                                        });

                                        // TODO : analyze SDT a PSI table, then set PMT PID and analyze bytes again .......

                                    } else
                                    {
                                        Device.BeginInvokeOnMainThread(() =>
                                        {
                                            StatusLabel.Text += "Error, No data read";
                                            StatusLabel.Text += Environment.NewLine;
                                        });
                                    }

                                } else
                                {
                                    Device.BeginInvokeOnMainThread(() =>
                                    {
                                        StatusLabel.Text += "PID filter request failed";
                                        StatusLabel.Text += Environment.NewLine;
                                    });
                                }
                            }
                            else
                            {
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    StatusLabel.Text += "Status request failed";
                                    StatusLabel.Text += Environment.NewLine;
                                });
                            }
                        } else
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                StatusLabel.Text += "Status request failed";
                                StatusLabel.Text += Environment.NewLine;
                            });
                        }
                    } else
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            StatusLabel.Text += "Tune request failed";
                            StatusLabel.Text += Environment.NewLine;
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
            var url = $"http://127.0.0.1:{_driver.Configuration.Driver.TransferPort}";
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
                //_log.Error(ex);
            }
        }

    }
}
