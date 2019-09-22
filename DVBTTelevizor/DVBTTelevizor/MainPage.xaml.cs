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
        //DVBTDriverConfiguration _driverConfiguration;
        DVBTTelevizorConfiguration _configuration;
        TcpClient client;
        NetworkStream nwStream;

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


            _configuration = new DVBTTelevizorConfiguration();   

            MessagingCenter.Subscribe<string>(this, "DVBTDriverConfiguration", (message) =>
            {
                InfoLabel.Text = message;
                _configuration.Driver = JsonConvert.DeserializeObject<DVBTDriverConfiguration>(message);

                client = new TcpClient();
                client.Connect("127.0.0.1", _configuration.Driver.ControlPort);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                client.SendTimeout = 5000;
                client.ReceiveTimeout = 50000;
                nwStream = client.GetStream();
            });
        }                       

        public DVBTStatus GetStatus(TcpClient client, NetworkStream nwStream)
        {
            var status = new DVBTStatus();

                List<byte> bytesToSend = new List<byte>();

                bytesToSend.Add(3); // REQ_GET_STATUS

                /*
                var payLoadAsByteArray = DVBTStatus.GetByteArrayFromBigEndianLong(0);
                bytesToSend.AddRange(payLoadAsByteArray);

                nwStream.Write(bytesToSend.ToArray(), 0, 9);
                nwStream.Flush();
                */

                bytesToSend.Add(0); // no payload
                nwStream.Write(bytesToSend.ToArray(), 0, bytesToSend.Count);


                var responseSize = 9 * 8 + 2;
            //var responseSize = client.ReceiveBufferSize;

            /*
                byte[] bytesToRead = new byte[responseSize];
                int bytesRead = nwStream.Read(bytesToRead, 0, responseSize);

                //while (nwStream.DataAvailable);

                nwStream.Flush();

                for (var i = 0; i < bytesRead; i++)
                {
                    Debug.WriteLine($"{i}: {bytesToRead[i]}");
                }
                */
                List<byte> bytesRead = new List<byte>();

                int totalBytesRead = 0;

                var startTime = DateTime.Now;

                do
                {
                    byte[] bytesToReadPart = new byte[responseSize];
                    var bytes = nwStream.Read(bytesToReadPart, 0, responseSize - totalBytesRead);
                    totalBytesRead += bytes;
                    for (var i = 0; i < bytes; i++) bytesRead.Add(bytesToReadPart[i]);

                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    System.Threading.Thread.Sleep(500);
                    if (Math.Abs((DateTime.Now - startTime).TotalSeconds) > 60)
                    {
                        break;
                    }
                }
                while (totalBytesRead < responseSize);

                InfoLabel.Text += Environment.NewLine + $"Bytes received: {bytesRead}";

                /*
                 * byte 0 will be the Request.ordinal of the Request
                 * byte 1 will be N the number of longs in the payload
                 * byte 3 to byte 6 will be the success flag (0 or 1). This indicates whether the request was succesful.
                 * byte 7 till the end the rest of the longs in the payload follow
                 *  *
                 * Basically the success flag is always part of the payload, so the payload
                 * always consists of at least one value.
                */

                var requestNumber = bytesRead[0];
                var longsCountInResponse = bytesRead[1];                

                status.ParseFromByteArray(bytesRead.ToArray(), 2);

                return status;            
        }

        public void SendCloseConnection(TcpClient client, NetworkStream nwStream)
        {
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(1); // REQ_EXIT
            bytesToSend.Add(0); // REQ_EXIT

            var bytesRead = Send(client, nwStream, bytesToSend.ToArray(), 10);
         
            InfoLabel.Text += Environment.NewLine + $"Bytes received: {bytesRead.Length}";             

            var requestNumber = bytesRead[0];
            var longsCountInResponse = bytesRead[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 2);    
        }

        private byte[] Send(TcpClient client, NetworkStream nwStream, byte[] bytesToSend, int responseSize, int secondsTimeout = 10)
        {
            nwStream.Write(bytesToSend.ToArray(), 0, bytesToSend.Length);

            List<byte> bytesRead = new List<byte>();

            int totalBytesRead = 0;

            var startTime = DateTime.Now;

            //* byte 0 will be the Request.ordinal of the Request
            //* byte 1 will be N the number of longs in the payload
            //* byte 3 to byte 6 will be the success flag (0 or 1). This indicates whether the request was succesful.
            //* byte 7 till the end the rest of the longs in the payload follow
            //*  *
            //* Basically the success flag is always part of the payload, so the payload
            //* always consists of at least one value.

            do
            {
                byte[] bytesToReadPart = new byte[responseSize];
                var bytes = nwStream.Read(bytesToReadPart, 0, responseSize - totalBytesRead);
                totalBytesRead += bytes;
                for (var i = 0; i < bytes; i++) bytesRead.Add(bytesToReadPart[i]);

                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                System.Threading.Thread.Sleep(500);
                if (Math.Abs((DateTime.Now - startTime).TotalSeconds) > secondsTimeout)
                {
                    break;
                }
            }
            while (totalBytesRead < responseSize);

            //for (var i = 0; i < bytesRead.Count; i++)
            //{
            //    Debug.WriteLine($"{i}: {bytesRead[i]}");
            //}

            return bytesRead.ToArray();
        }

        private bool Tune(TcpClient client, NetworkStream nwStream, long frequency, long bandwidth, int deliverySyetem)
        {         
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(2); // REQ_TUNE
                                //bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(3)); // Payload for 3 longs
            bytesToSend.Add(3); // Payload for 3 longs

            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(frequency)); // Payload[0] => frequency
            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(bandwidth)); // Payload[1] => bandWidth
            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(deliverySyetem));         // Payload[2] => DeliverySystem DVBT

            nwStream.Write(bytesToSend.ToArray(), 0, bytesToSend.Count);
        //nwStream.Flush();

            var responseSize = 10;
            //var responseSize = client.ReceiveBufferSize;

            List<byte> bytesRead = new List<byte>();

            int totalBytesRead = 0;

            var startTime = DateTime.Now;

            do
            {
                byte[] bytesToReadPart = new byte[responseSize];
                var bytes = nwStream.Read(bytesToReadPart, 0, 10 - totalBytesRead);
                totalBytesRead += bytes;
                for (var i = 0; i < bytes; i++) bytesRead.Add(bytesToReadPart[i]);

                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                System.Threading.Thread.Sleep(500);
                if (Math.Abs((DateTime.Now-startTime).TotalSeconds) > 5)
                {
                    break;
                }
            }
            while (totalBytesRead < responseSize);
            
            //Debug.WriteLine($"Total bytes: {bytesRead}");
            //for (var i = 0; i < bytesRead; i++)
            //{
            //    Debug.WriteLine($"{i}: {bytesToRead[i]}");
            //}

            InfoLabel.Text += Environment.NewLine + $"Bytes received: {totalBytesRead}";

            var requestNumber = bytesRead[0];
            var longsCountInResponse = bytesRead[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead.ToArray(), 2);

            InfoLabel.Text += Environment.NewLine + $"BytesSuccessFlag: {successFlag}";
            InfoLabel.Text += Environment.NewLine + $"longsCountInResponse: {longsCountInResponse}";

            return successFlag == 1;
        }
        
        private DVBTVersion GetVersion(TcpClient client, NetworkStream nwStream)
        {
            var version = new DVBTVersion();

                List<byte> bytesToSend = new List<byte>();

                bytesToSend.Add(0); // REQ_PROTOCOL_VERSION
                bytesToSend.Add(0); // Payload size

                var bytesRead = Send(client, nwStream, bytesToSend.ToArray(), 26);
                
                InfoLabel.Text += Environment.NewLine + $"Bytes received: {bytesRead.Length}";

                var requestNumber = bytesRead[0];
                var longsCountInResponse = bytesRead[1];
                version.SuccessFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 2);
                version.Version = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 10);
                version.AllRequestsLength = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 18);

            return version;
        }
     
        private void GetStatusButton_Clicked(object sender, EventArgs e)
        {
            InfoLabel.Text = Environment.NewLine + "Getting status ...";

            try
            {
                //using (var client = new TcpClient("127.0.0.1", _configuration.Driver.ControlPort))
                //{
                //    using (var nwStream = client.GetStream())
                //    {

                        //client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        //client.SendTimeout = 5000;
                        //client.ReceiveTimeout = 50000;

                        var status = GetStatus(client, nwStream);

                            InfoLabel.Text += Environment.NewLine + $"Success: {status.SuccessFlag}";

                            InfoLabel.Text += Environment.NewLine + $"snr: {status.snr}";
                            InfoLabel.Text += Environment.NewLine + $"bitErrorRate: {status.bitErrorRate}";
                            InfoLabel.Text += Environment.NewLine + $"droppedUsbFps: {status.droppedUsbFps}";
                            InfoLabel.Text += Environment.NewLine + $"rfStrengthPercentage: {status.rfStrengthPercentage}";
                            InfoLabel.Text += Environment.NewLine + $"hasSignal: {status.hasSignal}";
                            InfoLabel.Text += Environment.NewLine + $"hasCarrier: {status.hasCarrier}";
                            InfoLabel.Text += Environment.NewLine + $"hasSync: {status.hasSync}";
                            InfoLabel.Text += Environment.NewLine + $"hasLock: {status.hasLock}";

                            //client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                            /*
                            (long)snr, // parameter 1
                                (long)bitErrorRate, // parameter 2
                                (long)droppedUsbFps, // parameter 3
                                (long)rfStrengthPercentage, // parameter 4
                                hasSignal ? 1L : 0L, // parameter 5
                                hasCarrier ? 1L : 0L, // parameter 6
                                hasSync ? 1L : 0L, // parameter 7
                                hasLock ? 1L : 0L // parameter 8
                            */

                            //Debug.WriteLine($"Total bytes: {bytesRead}");
                            //for (var i = 0; i < bytesRead; i++)
                            //{
                            //    Debug.WriteLine($"{i}: {bytesToRead[i]}");
                            //}               
                    //    }
                    //    client.Close();
                    //}

            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }
               
        private void GetVersionButton_Clicked(object sender, EventArgs e)
        {
            InfoLabel.Text = Environment.NewLine + "Getting Version ...";

            try
            {
                //using (var client = new TcpClient("127.0.0.1", _configuration.Driver.ControlPort))
                //{
                //    using (var nwStream = client.GetStream())
                //    {
                        var version = GetVersion(client, nwStream);

                        InfoLabel.Text += version.ToString();
                //    }
                //    client.Close();
                //}

            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }

        private void InitButton_Clicked(object sender, EventArgs e)
        {
            InfoLabel.Text = Environment.NewLine + "Initializing ...";
            MessagingCenter.Send("", "Init");
        }

        private void PlayButton_Clicked(object sender, EventArgs e)
        {
            //var url = $"http://127.0.0.1:{_configuration.Driver.TransferPort}";
            //var url = $"rtsp://127.0.0.1:{_configuration.Driver.TransferPort}";
            var url = $"udp://127.0.0.1:{_configuration.Driver.TransferPort}";
            MessagingCenter.Send(url, "PlayUrl");
            InfoLabel.Text = Environment.NewLine + $"Playing url: {url}";
        }


        private void StopButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                SendCloseConnection(client, nwStream);

                InfoLabel.Text += "Stopped";
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

        private async Task Record()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect("127.0.0.1", _configuration.Driver.TransferPort);
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
            InfoLabel.Text = Environment.NewLine + "Tuning 490 Mhz, 8 Mhz bandwith ...";

            try
            {
                //using (var client = new TcpClient("127.0.0.1", _configuration.Driver.ControlPort))
                //{
                //    using (var nwStream = client.GetStream())
                //    {
                        Tune(client, nwStream, 490000000, 8000000, 0);
                //    }
                //    client.Close();
                //}

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
