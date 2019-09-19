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

        public MainPage()
        {
            InitializeComponent();

            this.GetStatusButton.Clicked += GetStatusButton_Clicked;
            this.GetVersionButton.Clicked += GetVersionButton_Clicked;
            this.InitButton.Clicked += InitButton_Clicked;

            MessagingCenter.Subscribe<string>(this, "DVBTDriverConfiguration", (message) =>
            {
                InfoLabel.Text = message;
                _configuration = JsonConvert.DeserializeObject<DVBTDriverConfiguration>(message);
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

        private void GetStatus(TcpClient tcpclnt)
        {
            List<byte> bytesToSend = new List<byte>();

            var nwStream = tcpclnt.GetStream();

            bytesToSend.Add(3); // REQ_GET_STATUS

            var payLoadAsByteArray = GetByteArrayFromBigEndianLong(0);

            bytesToSend.AddRange(payLoadAsByteArray);

            nwStream.Write(bytesToSend.ToArray(), 0, 9);


            //---read back the text---
            byte[] bytesToRead = new byte[tcpclnt.ReceiveBufferSize];
            int bytesRead = nwStream.Read(bytesToRead, 0, tcpclnt.ReceiveBufferSize);

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

        private void GetProtocolVersion(TcpClient tcpclnt)
        {
            List<byte> bytesToSend = new List<byte>();

            var nwStream = tcpclnt.GetStream();

            bytesToSend.Add(0); // REQ_PROTOCOL_VERSION
            var payLoadAsByteArray = GetByteArrayFromBigEndianLong(0);

            bytesToSend.AddRange(payLoadAsByteArray);

            nwStream.Write(bytesToSend.ToArray(), 0, 9);

            byte[] bytesToRead = new byte[tcpclnt.ReceiveBufferSize];
            int bytesRead = nwStream.Read(bytesToRead, 0, tcpclnt.ReceiveBufferSize);

            Debug.WriteLine($"Total bytes: {bytesRead}");
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
            var successFlag = GetBigEndianLongFromByteArray(bytesToRead, 2);

            InfoLabel.Text += Environment.NewLine + $"BytesSuccessFlag: {successFlag}";
            InfoLabel.Text += Environment.NewLine + $"longsCountInResponse: {longsCountInResponse}";

            var allRequestsLength = GetBigEndianLongFromByteArray(bytesToRead, 10);

            InfoLabel.Text += Environment.NewLine + $"allRequestsLength: {allRequestsLength}";
        }

        private void GetStatusButton_Clicked(object sender, EventArgs e)
        {
            TcpClient tcpclnt = new TcpClient();
            InfoLabel.Text += Environment.NewLine + "Connecting...";

            try
            {
                tcpclnt.Connect("127.0.0.1", _configuration.ControlPort);

                InfoLabel.Text += Environment.NewLine + "Connected";

                GetStatus(tcpclnt);

                tcpclnt.Close();
            }
            catch
            {
                InfoLabel.Text += Environment.NewLine + "Connection failed";
            }
        }

        private void GetVersionButton_Clicked(object sender, EventArgs e)
        {
            TcpClient tcpclnt = new TcpClient();
            InfoLabel.Text += Environment.NewLine + "Connecting...";

            try
            {
                tcpclnt.Connect("127.0.0.1", _configuration.ControlPort);

                InfoLabel.Text += Environment.NewLine + "Connected";

                GetProtocolVersion(tcpclnt);

                tcpclnt.Close();
            }
            catch
            {
                InfoLabel.Text += Environment.NewLine + "Connection failed";
            }
        }

        private void InitButton_Clicked(object sender, EventArgs e)
        {
            MessagingCenter.Send("", "Init");
        }
    }
}
