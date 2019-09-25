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

namespace DVBTTelevizor
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        DVBTDriverManager _driver;

        public MainPage()
        {
            InitializeComponent();

            this.GetStatusButton.Clicked += GetStatusButton_Clicked;
            this.GetVersionButton.Clicked += GetVersionButton_Clicked;
            this.InitButton.Clicked += InitButton_Clicked;
            this.TuneButton.Clicked += TuneButton_Clicked;
            this.StopButton.Clicked += StopButton_Clicked;
            this.PlayButton.Clicked += PlayButton_Clicked;
            this.RecordButton.Clicked += RecordButton_Clicked;
            this.SetPIDsButton.Clicked += SetPIDsButton_Clicked;

            DeliverySystemPicker.SelectedIndex = 0;

            _driver = new DVBTDriverManager();

            MessagingCenter.Subscribe<string>(this, "DVBTDriverConfiguration", (message) =>
            {
                PortsLabel.Text = message;

                _driver.Configuration.Driver = JsonConvert.DeserializeObject<DVBTDriverConfiguration>(message);

                PortsLabel.Text = $"Control port: {_driver.Configuration.Driver.ControlPort}, Transfer port: {_driver.Configuration.Driver.TransferPort}";

                _driver.Connect();
            });
        }

        private void GetStatusButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = Environment.NewLine + "Getting status ...";

            try
            {
                var status = _driver.GetStatus();

                StatusLabel.Text = Environment.NewLine + $"Success: {status.SuccessFlag}";
                StatusLabel.Text += Environment.NewLine + $"snr: {status.snr}";
                StatusLabel.Text += Environment.NewLine + $"bitErrorRate: {status.bitErrorRate}";
                StatusLabel.Text += Environment.NewLine + $"droppedUsbFps: {status.droppedUsbFps}";
                StatusLabel.Text += Environment.NewLine + $"rfStrengthPercentage: {status.rfStrengthPercentage}";
                StatusLabel.Text += Environment.NewLine + $"hasSignal: {status.hasSignal}";
                StatusLabel.Text += Environment.NewLine + $"hasCarrier: {status.hasCarrier}";
                StatusLabel.Text += Environment.NewLine + $"hasSync: {status.hasSync}";
                StatusLabel.Text += Environment.NewLine + $"hasLock: {status.hasLock}";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }

        private void GetVersionButton_Clicked(object sender, EventArgs e)
        {
            VersionLabel.Text = Environment.NewLine + "Getting Version ...";

            try
            {
                var version = _driver.GetVersion();

                VersionLabel.Text = $"Version: {version.ToString()}";
            }
            catch (Exception ex)
            {
                VersionLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }

        private void InitButton_Clicked(object sender, EventArgs e)
        {
            InfoLabel.Text = Environment.NewLine + "Initializing ...";
            MessagingCenter.Send("", "Init");
        }

        private void PlayButton_Clicked(object sender, EventArgs e)
        {
            var url = $"http://127.0.0.1:{_driver.Configuration.Driver.TransferPort}";
            MessagingCenter.Send(url, "PlayUrl");
            InfoLabel.Text = Environment.NewLine + $"Playing url: {url}";
        }

        private void StopButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var res = _driver.SendCloseConnection();

                InfoLabel.Text = Environment.NewLine + $"Stop result: {res}";
            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }

        private void RecordButton_Clicked(object sender, EventArgs e)
        {
            RunWithPermission(Permission.Storage, async () => await Record());
        }

        private void SetPIDsButton_Clicked(object sender, EventArgs e)
        {
            InfoLabel.Text = Environment.NewLine + "Settting PIDs ...";

            try
            {
                var pids = new List<long>();

                if (!String.IsNullOrEmpty(EntryPID1.Text))
                    pids.Add(Convert.ToInt64(EntryPID1.Text));

                if (!String.IsNullOrEmpty(EntryPID2.Text))
                    pids.Add(Convert.ToInt64(EntryPID2.Text));

                if (!String.IsNullOrEmpty(EntryPID3.Text))
                    pids.Add(Convert.ToInt64(EntryPID3.Text));

                var pidRes = _driver.SetPIDs(pids);

                InfoLabel.Text += Environment.NewLine + $"PIDs Set result: {pidRes}";
            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }

        private async Task Record()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect("127.0.0.1", _driver.Configuration.Driver.TransferPort);
                    client.ReceiveBufferSize = 1000000; // 1 MB
                    client.SendTimeout = 5000;
                    client.ReceiveTimeout = 50000;
                    using (var nwStream = client.GetStream())
                    {
                        byte[] bytesToReadPart = new byte[client.ReceiveBufferSize];
                        using (var fs = new FileStream("/storage/emulated/0/Download/mux.ts", FileMode.Create, FileAccess.Write))
                        {
                            do
                            {
                                var bytesRead = nwStream.Read(bytesToReadPart, 0, client.ReceiveBufferSize);
                                fs.Write(bytesToReadPart, 0, bytesRead);
                                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                            }
                            while (client.Connected);
                        }
                    }
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }

        private void TuneButton_Clicked(object sender, EventArgs e)
        {
            InfoLabel.Text = Environment.NewLine + "Tuning  ...";

            try
            {
                var freq = Convert.ToInt64(EntryFrequency.Text) * 1000000;
                var bandWidth = Convert.ToInt64(EntryBandWidth.Text) * 1000000;

                var type = DeliverySystemPicker.SelectedIndex;

                var tuneRes = _driver.Tune(freq, bandWidth, type);

                InfoLabel.Text += Environment.NewLine + $"Tune result: {tuneRes}";
            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
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
                    //if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(perm))
                    //{
                    //    await _dlgService.Information("Aplikace vyžaduje potvrzení k oprávnění.", "Informace");
                    //}

                    var results = await CrossPermissions.Current.RequestPermissionsAsync(perm);

                    if (results.ContainsKey(perm))
                        status = results[perm];
                }

                if (status == PermissionStatus.Granted)
                {
                    await action();
                }
                else if (status != PermissionStatus.Unknown)
                {
                    //await _dlgService.Error("Nebylo uděleno oprávnění", "Chyba");
                }
            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }

    }
}
