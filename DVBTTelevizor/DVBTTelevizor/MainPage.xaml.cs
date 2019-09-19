using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        DVBTDriverConfiguration _configuration;
        TcpClient tcpClient;

        public MainPage()
        {
            InitializeComponent();

            this.GetStatusButton.Clicked += GetStatusButton_Clicked;
            this.GetVersionButton.Clicked += GetVersionButton_Clicked;
            this.InitButton.Clicked += InitButton_Clicked;
            this.GoButton.Clicked += GoButton_Clicked;
            this.StopButton.Clicked += StopButton_Clicked;

            MessagingCenter.Subscribe<string>(this, "DVBTDriverConfiguration", (message) =>
            {
                InfoLabel.Text = message;
                _configuration = JsonConvert.DeserializeObject<DVBTDriverConfiguration>(message);
                tcpClient = new TcpClient();
                tcpClient.Connect("127.0.0.1", _configuration.ControlPort);
            });
        }


        private long GetBigEndianLongFromByteArray(byte[] ba, int offset)
        {
            var reversedArray = new List<byte>();
            for (var i=offset+8-1;i>=offset;i--)
            {
                reversedArray.Add(ba[i]);
            }

            return BitConverter.ToInt64(reversedArray.ToArray(), 0);
        }

        private byte[] GetByteArrayFromBigEndianLong(long l)
        {
            var reversedArray = BitConverter.GetBytes(l);
            return reversedArray.Reverse().ToArray();
        } 

        private void GetStatusButton_Clicked(object sender, EventArgs e)
        {
            InfoLabel.Text = Environment.NewLine + "Getting status ...";

            try
            {
                //using (var tcpClient = new TcpClient())
                //{
                    //tcpClient.Connect("127.0.0.1", _configuration.ControlPort);

                    List<byte> bytesToSend = new List<byte>();

                    using (var nwStream = tcpClient.GetStream())
                    {
                        bytesToSend.Add(3); // REQ_GET_STATUS

                        var payLoadAsByteArray = GetByteArrayFromBigEndianLong(0);

                        bytesToSend.AddRange(payLoadAsByteArray);

                        nwStream.Write(bytesToSend.ToArray(), 0, 9);

                        //---read back the text---
                        byte[] bytesToRead = new byte[tcpClient.ReceiveBufferSize];
                        int bytesRead = nwStream.Read(bytesToRead, 0, tcpClient.ReceiveBufferSize);

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
                        var successFlag = GetBigEndianLongFromByteArray(bytesToRead, 2);

                        InfoLabel.Text += Environment.NewLine + $"BytesSuccessFlag: {successFlag}";

                        var snr = GetBigEndianLongFromByteArray(bytesToRead, 10);
                        var bitErrorRate = GetBigEndianLongFromByteArray(bytesToRead, 18);
                        var droppedUsbFps = GetBigEndianLongFromByteArray(bytesToRead, 26);
                        var rfStrengthPercentage = GetBigEndianLongFromByteArray(bytesToRead, 34);
                        var hasSignal = GetBigEndianLongFromByteArray(bytesToRead, 42);
                        var hasCarrier = GetBigEndianLongFromByteArray(bytesToRead, 50);
                        var hasSync = GetBigEndianLongFromByteArray(bytesToRead, 58);
                        var hasLock = GetBigEndianLongFromByteArray(bytesToRead, 66);

                        InfoLabel.Text += Environment.NewLine + $"snr: {snr}";
                        InfoLabel.Text += Environment.NewLine + $"bitErrorRate: {bitErrorRate}";
                        InfoLabel.Text += Environment.NewLine + $"droppedUsbFps: {droppedUsbFps}";
                        InfoLabel.Text += Environment.NewLine + $"rfStrengthPercentage: {rfStrengthPercentage}";
                        InfoLabel.Text += Environment.NewLine + $"hasSignal: {hasSignal}";
                        InfoLabel.Text += Environment.NewLine + $"hasCarrier: {hasCarrier}";
                        InfoLabel.Text += Environment.NewLine + $"hasSync: {hasSync}";
                        InfoLabel.Text += Environment.NewLine + $"hasLock: {hasLock}";

                        nwStream.Close();
                    }
                 //   tcpClient.Close();
                //}

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
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }

        private void GetVersionButton_Clicked(object sender, EventArgs e)
        {
  
            InfoLabel.Text = Environment.NewLine + "Getting version ...";

            try
            {
                List<byte> bytesToSend = new List<byte>();

                var nwStream = tcpClient.GetStream();

                bytesToSend.Add(0); // REQ_PROTOCOL_VERSION
                var payLoadAsByteArray = GetByteArrayFromBigEndianLong(0);

                bytesToSend.AddRange(payLoadAsByteArray);

                nwStream.Write(bytesToSend.ToArray(), 0, 9);

                byte[] bytesToRead = new byte[tcpClient.ReceiveBufferSize];
                int bytesRead = nwStream.Read(bytesToRead, 0, tcpClient.ReceiveBufferSize);

                Debug.WriteLine($"Total bytes: {bytesRead}");
                for (var i = 0; i < bytesRead; i++)
                {
                    Debug.WriteLine($"{i}: {bytesToRead[i]}");
                }

                InfoLabel.Text += Environment.NewLine + $"Bytes received: {bytesRead}";

                
                 //* byte 0 will be the Request.ordinal of the Request
                 //* byte 1 will be N the number of longs in the payload
                 //* byte 3 to byte 6 will be the success flag (0 or 1). This indicates whether the request was succesful.
                 //* byte 7 till the end the rest of the longs in the payload follow
                 //*  *
                 //* Basically the success flag is always part of the payload, so the payload
                 //* always consists of at least one value.
              

                var requestNumber = bytesToRead[0];
                var longsCountInResponse = bytesToRead[1];
                var successFlag = GetBigEndianLongFromByteArray(bytesToRead, 2);

                InfoLabel.Text += Environment.NewLine + $"BytesSuccessFlag: {successFlag}";
                InfoLabel.Text += Environment.NewLine + $"longsCountInResponse: {longsCountInResponse}";

                var allRequestsLength = GetBigEndianLongFromByteArray(bytesToRead, 10);

                InfoLabel.Text += Environment.NewLine + $"allRequestsLength: {allRequestsLength}";

                nwStream.Close();
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

        private void StopButton_Clicked(object sender, EventArgs e)
        {
            //Disconnect();
        }

        private void GoButton_Clicked(object sender, EventArgs e)
        {           
            InfoLabel.Text = Environment.NewLine + "Tuning 490 Mhz, 8 Mhz bandwith ...";

            try
            {          
                List<byte> bytesToSend = new List<byte>();

                var nwStream = tcpClient.GetStream();

                bytesToSend.Add(2); // REQ_TUNE
                bytesToSend.AddRange(GetByteArrayFromBigEndianLong(3)); // Payload for 3 longs

                bytesToSend.AddRange(GetByteArrayFromBigEndianLong(490000000)); // Payload[0] => frequency
                bytesToSend.AddRange(GetByteArrayFromBigEndianLong(  8000000)); // Payload[1] => bandWidth
                bytesToSend.AddRange(GetByteArrayFromBigEndianLong(1));         // Payload[1] => DeliverySystem DVBT

                nwStream.Write(bytesToSend.ToArray(), 0, 1+8+3*8);

                byte[] bytesToRead = new byte[tcpClient.ReceiveBufferSize];
                int bytesRead = nwStream.Read(bytesToRead, 0, tcpClient.ReceiveBufferSize);

                Debug.WriteLine($"Total bytes: {bytesRead}");
                for (var i = 0; i < bytesRead; i++)
                {
                    Debug.WriteLine($"{i}: {bytesToRead[i]}");
                }

                InfoLabel.Text += Environment.NewLine + $"Bytes received: {bytesRead}";

               
                 //* byte 0 will be the Request.ordinal of the Request
                 //* byte 1 will be N the number of longs in the payload
                 //* byte 3 to byte 6 will be the success flag (0 or 1). This indicates whether the request was succesful.
                 //* byte 7 till the end the rest of the longs in the payload follow
                 //*  *
                 //* Basically the success flag is always part of the payload, so the payload
                 //* always consists of at least one value.
              

                var requestNumber = bytesToRead[0];
                var longsCountInResponse = bytesToRead[1];
                var successFlag = GetBigEndianLongFromByteArray(bytesToRead, 2);

                InfoLabel.Text += Environment.NewLine + $"BytesSuccessFlag: {successFlag}";
                InfoLabel.Text += Environment.NewLine + $"longsCountInResponse: {longsCountInResponse}";

                nwStream.Close();
            }
            catch (Exception ex)
            {
                InfoLabel.Text = Environment.NewLine + $"Request failed ({ex.Message})";
            }
        }        
    }
}
