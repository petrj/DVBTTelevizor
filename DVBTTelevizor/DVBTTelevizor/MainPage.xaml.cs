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

            this.RunButton.Clicked += RunButton_Clicked;
            this.InitButton.Clicked += InitButton_Clicked;

            MessagingCenter.Subscribe<string>(this, "DVBTDriverConfiguration", (message) =>
            {
                InfoLabel.Text = message;
                _configuration = JsonConvert.DeserializeObject<DVBTDriverConfiguration>(message);
            });
        }

        private void RunButton_Clicked(object sender, EventArgs e)
        {
            TcpClient tcpclnt = new TcpClient();
            InfoLabel.Text += Environment.NewLine + "Connecting...";

            try
            {
                tcpclnt.Connect("127.0.0.1", _configuration.ControlPort);

                InfoLabel.Text += Environment.NewLine + "Connected";

                var nwStream = tcpclnt.GetStream();

                List<byte> bytesToSend = new List<byte>();


                bytesToSend.Add(3); // REQ_GET_STATUS

                //bytesToSend.Add(0); // REQ_PROTOCOL_VERSION

                long payLoad = 0;
                var payLoadAsByteArray = BitConverter.GetBytes(payLoad);

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
                */

                var requestNumber = bytesToRead[0];
                var longsCountInResponse = bytesToRead[1];
                var successFlag = BitConverter.ToInt64(bytesToRead, 2);

                Debug.WriteLine($"Total bytes: {bytesRead}");
                for (var i = 0; i < bytesRead; i++)
                {
                    Debug.WriteLine($"{i}: {bytesToRead[i]}");
                }

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
