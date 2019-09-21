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

namespace DVBTTelevizor
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        //DVBTDriverConfiguration _driverConfiguration;
        DVBTTelevizorConfiguration _configuration;
        //TcpClient tcpClient;
        //NetworkStream nwStream;

        public MainPage()
        {
            InitializeComponent();

            this.GetStatusButton.Clicked += GetStatusButton_Clicked;
            this.GetVersionButton.Clicked += GetVersionButton_Clicked;
            this.InitButton.Clicked += InitButton_Clicked;
            this.GoButton.Clicked += GoButton_Clicked;
            this.StopButton.Clicked += StopButton_Clicked;

            _configuration = new DVBTTelevizorConfiguration();
   

            MessagingCenter.Subscribe<string>(this, "DVBTDriverConfiguration", (message) =>
            {
                InfoLabel.Text = message;
                _configuration.Driver = JsonConvert.DeserializeObject<DVBTDriverConfiguration>(message);
                
                //tcpClient.Connect("127.0.0.1", _configuration.Driver.ControlPort);
                //try
                //{
                //    tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                //} catch (Exception ex)
                //{

                //}                
            });
        }
                       

        public DVBTStatus GetStatus(TcpClient client, NetworkStream nwStream )
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
                    nwStream.Write(bytesToSend.ToArray(), 0, 2);


                    //var responseSize = 9 * 8 + 2;
                    var responseSize = client.ReceiveBufferSize;

                    byte[] bytesToRead = new byte[responseSize];
                    int bytesRead = nwStream.Read(bytesToRead, 0, responseSize);
            
                    //while (nwStream.DataAvailable);

                    nwStream.Flush();

            for (var i = 0; i < bytesRead; i++)
            {
                Debug.WriteLine($"{i}: {bytesToRead[i]}");
            }

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

                    var requestNumber = bytesToRead[0];
                    var longsCountInResponse = bytesToRead[1];
                    var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesToRead, 2);

                    status.ParseFromByteArray(bytesToRead, 10);

                    InfoLabel.Text += Environment.NewLine + $"BytesSuccessFlag: {successFlag}";

                    InfoLabel.Text += Environment.NewLine + $"snr: {status.snr}";
                    InfoLabel.Text += Environment.NewLine + $"bitErrorRate: {status.bitErrorRate}";
                    InfoLabel.Text += Environment.NewLine + $"droppedUsbFps: {status.droppedUsbFps}";
                    InfoLabel.Text += Environment.NewLine + $"rfStrengthPercentage: {status.rfStrengthPercentage}";
                    InfoLabel.Text += Environment.NewLine + $"hasSignal: {status.hasSignal}";
                    InfoLabel.Text += Environment.NewLine + $"hasCarrier: {status.hasCarrier}";
                    InfoLabel.Text += Environment.NewLine + $"hasSync: {status.hasSync}";
                    InfoLabel.Text += Environment.NewLine + $"hasLock: {status.hasLock}";

            return status;
        }

        public void SendCloseConnection(TcpClient client, NetworkStream nwStream)
        {
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(1); // REQ_EXIT

            var payLoadAsByteArray = DVBTStatus.GetByteArrayFromBigEndianLong(0);

            bytesToSend.AddRange(payLoadAsByteArray);

            nwStream.Write(bytesToSend.ToArray(), 0, 9);
            nwStream.Flush();

            //var responseSize = 8 + 2;
            var responseSize = client.ReceiveBufferSize;

            byte[] bytesToRead = new byte[responseSize];
            int bytesRead = nwStream.Read(bytesToRead, 0, responseSize);

            //while (nwStream.DataAvailable);

            nwStream.Flush();

            for (var i = 0; i < bytesRead; i++)
            {
                Debug.WriteLine($"{i}: {bytesToRead[i]}");
            }

            InfoLabel.Text += Environment.NewLine + $"Bytes received: {bytesRead}";
             

            var requestNumber = bytesToRead[0];
            var longsCountInResponse = bytesToRead[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesToRead, 2);

    
        }

        private void GetStatusButton_Clicked(object sender, EventArgs e)
        {
            InfoLabel.Text = Environment.NewLine + "Getting status ...";

            try
            {
                using (var client = new TcpClient("127.0.0.1", _configuration.Driver.ControlPort))                
                {
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    client.SendTimeout = 5000;
                    client.ReceiveTimeout = 50000;


                    using (var nwStream = client.GetStream())
                    {

                        var status1 = GetStatus(client, nwStream);

                        // SendCloseConnection(client, nwStream);

                        GetVersion(client, nwStream);

                        System.Threading.Thread.Sleep(500);

                        var status2 = GetStatus(client, nwStream);

                        System.Threading.Thread.Sleep(500);

                        //var ver = GetVersion();
                        

                        Tune(490000000, 8000000, 1);

                        System.Threading.Thread.Sleep(2000);

                        var status3 = GetStatus(client, nwStream);

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


                    }

                    client.Close();
                }
            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }

      
        private long GetVersion(TcpClient client, NetworkStream nwStream)
        {
          
                List<byte> bytesToSend = new List<byte>();

                bytesToSend.Add(0); // REQ_PROTOCOL_VERSION
                                    //var payLoadAsByteArray = DVBTStatus.GetByteArrayFromBigEndianLong(0);
                                    //bytesToSend.AddRange(payLoadAsByteArray);
                bytesToSend.Add(0);

                nwStream.Write(bytesToSend.ToArray(), 0, bytesToSend.Count);

                //var responseSize = 2 * 8 + 2;
                var responseSize = client.ReceiveBufferSize;

                byte[] bytesToRead = new byte[responseSize];
                int bytesRead = nwStream.Read(bytesToRead, 0, responseSize);

                InfoLabel.Text += Environment.NewLine + $"Bytes received: {bytesRead}";

                Debug.WriteLine($"Total bytes: {bytesRead}");
                for (var i = 0; i < bytesRead; i++)
                {
                    Debug.WriteLine($"{i}: {bytesToRead[i]}");
                }

                //* byte 0 will be the Request.ordinal of the Request
                //* byte 1 will be N the number of longs in the payload
                //* byte 3 to byte 6 will be the success flag (0 or 1). This indicates whether the request was succesful.
                //* byte 7 till the end the rest of the longs in the payload follow
                //*  *
                //* Basically the success flag is always part of the payload, so the payload
                //* always consists of at least one value.

                var requestNumber = bytesToRead[0];
                var longsCountInResponse = bytesToRead[1];
                var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesToRead, 2);

                InfoLabel.Text += Environment.NewLine + $"BytesSuccessFlag: {successFlag}";
                InfoLabel.Text += Environment.NewLine + $"longsCountInResponse: {longsCountInResponse}";

                var allRequestsLength = DVBTStatus.GetBigEndianLongFromByteArray(bytesToRead, 10);

                InfoLabel.Text += Environment.NewLine + $"allRequestsLength: {allRequestsLength}";

                nwStream.Flush();

                return allRequestsLength;      
        }
     

        private void GetVersionButton_Clicked(object sender, EventArgs e)
        {
  
            InfoLabel.Text = Environment.NewLine + "Getting version ...";
            /*
            try
            {
                var ver = GetVersion();
            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }*/
        }

        private void InitButton_Clicked(object sender, EventArgs e)
        {
            InfoLabel.Text = Environment.NewLine + "Initializing ...";
            MessagingCenter.Send("", "Init");
        }

        private void StopButton_Clicked(object sender, EventArgs e)
        {
            //Disconnect();
        }

        private void Tune(long frequency, long bandwidth, int deliverySyetem)
        {
            /*
            using (var nwStream = tcpClient.GetStream())
            {
                List<byte> bytesToSend = new List<byte>();

                bytesToSend.Add(2); // REQ_TUNE
                bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(3)); // Payload for 3 longs

                bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(frequency)); // Payload[0] => frequency
                bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(bandwidth)); // Payload[1] => bandWidth
                bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(deliverySyetem));         // Payload[2] => DeliverySystem DVBT

                nwStream.Write(bytesToSend.ToArray(), 0, 1 + 8 + 3 * 8);
                nwStream.Flush();

                var responseSize = 8 + 2;

                byte[] bytesToRead = new byte[responseSize];
                int bytesRead = nwStream.Read(bytesToRead, 0, responseSize);

                
                //Debug.WriteLine($"Total bytes: {bytesRead}");
                //for (var i = 0; i < bytesRead; i++)
                //{
                //    Debug.WriteLine($"{i}: {bytesToRead[i]}");
                //}

                InfoLabel.Text += Environment.NewLine + $"Bytes received: {bytesRead}";

                var requestNumber = bytesToRead[0];
                var longsCountInResponse = bytesToRead[1];
                var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesToRead, 2);

                InfoLabel.Text += Environment.NewLine + $"BytesSuccessFlag: {successFlag}";
                InfoLabel.Text += Environment.NewLine + $"longsCountInResponse: {longsCountInResponse}";
            }*/
        }

        private void GoButton_Clicked(object sender, EventArgs e)
        {           
            InfoLabel.Text = Environment.NewLine + "Tuning 490 Mhz, 8 Mhz bandwith ...";

            try
            {
                Tune(490000000, 8000000, 0);

                //GetStatus();
            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }        
    }
}
