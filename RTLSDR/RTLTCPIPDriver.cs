using LoggerService;
using RTLSDR.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RTLSDR
{

    public class RTLTCPIPDriver : ISDR
    {
        private ILoggingService _loggingService;

        public RTLTCPIPDriver(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        private int _frequency = 192352000;
        private int _sampleRate = 2048000;

        private double _bitrate = 0;

        public DriverStateEnum State { get; private set; } = DriverStateEnum.NotInitialized;

        public int _gain = 0;

        public string DeviceName
        {
            get
            {
                return "RTL SDR TCPIP driver";
            }
        }

        public DriverSettings Settings { get; private set; } = new DriverSettings();

        public bool? Installed { get; set; } = true;

        public int Frequency
        {
            get
            {
                return _frequency;
            }
        }

        public TunerTypeEnum TunerType
        {
            get
            {
                return TunerTypeEnum.RTLSDR_TUNER_UNKNOWN;
            }
        }

        public long RTLBitrate
        {
            get
            {
                return Convert.ToInt64(_bitrate);
            }
        }

        public double PowerPercent
        {
            get
            {
                return 100;
            }
        }

        public double Power
        {
            get
            {
                return 1;
            }
        }

        public event EventHandler<OnDataReceivedEventArgs> OnDataReceived;

        public void Disconnect()
        {
            _loggingService.Info($"Disconnecting driver");

            try
            {
                State = DriverStateEnum.DisConnected;

                Task.Run(() =>
                {
                   Process[] workers = Process.GetProcessesByName("rtl_tcp");
                    foreach (Process worker in workers)
                    {
                        worker.Kill();
                        worker.WaitForExit();
                        worker.Dispose();
                    }
                });
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            };
        }

        public void SetErrorState()
        {
            _loggingService.Info($"Setting manually error state");
            State = DriverStateEnum.Error;
        }

        public static void Run(string command, string args, ILoggingService loggerService, string workingDir=null)
        {
            try
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();

                p.OutputDataReceived += (sender, a) =>
                {
                    loggerService.Info($"rtl_tcp: {a.Data}");
                };

                p.StartInfo.FileName = workingDir == null ? command : Path.Combine(workingDir, command);
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.Arguments = args;
                //p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                if (workingDir != null)
                {
                    p.StartInfo.WorkingDirectory = workingDir;
                }
                //p.EnableRaisingEvents = true;
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();
                p.CancelOutputRead();
            }
            catch (Exception ex)
            {
                loggerService.Error(ex);
            }
        }

        private void RunDriverMainLoop()
        {
            Task.Run(() =>
            {
                try
                {
                    Task.Run(() =>
                    {
                        State = DriverStateEnum.Connected;
                        Run("rtl_tcp", $"-f {Frequency} -s {_sampleRate} -g {_gain}", _loggingService);
                    });

                    _loggingService.Info("Waiting 5 secs for init driver");
                    Thread.Sleep(5000);

                    var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234);

                    var bufferSize = 65535;
                    var buffer = new byte[bufferSize];

                    using (var client = new TcpClient())
                    {
                        client.Connect(ipEndPoint);

                        using (var stream = client.GetStream())
                        {
                            while (true)
                            {
                                int received = stream.Read(buffer, 0, bufferSize);
                                //_loggingService.Info($"received: {received} bytes");

                                if (OnDataReceived != null)
                                {
                                    OnDataReceived(this, new OnDataReceivedEventArgs()
                                    {
                                        Data = buffer,
                                        Size = received
                                    });
                                }
                            }
                        }
                    }
                }
                catch (IOException s)
                {
                    if (State == DriverStateEnum.DisConnected)
                    {
                        return;  // stream.Read when disconnected?
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    State = DriverStateEnum.DisConnected;
                    _loggingService.Error(ex);
                }
            });
        }

        public void Init(DriverInitializationResult driverInitializationResult)
        {
            _loggingService.Info($"Starting {DeviceName}");

            RunDriverMainLoop();
        }

        public void SendCommand(Command command)
        {

        }

        public void SetAGCMode(bool automatic)
        {

        }

        public void SetDirectSampling(int value)
        {

        }

        public void SetFrequency(int freq)
        {
            if (_frequency != freq)
            {
                _frequency = freq;

                Disconnect();
                RunDriverMainLoop();
            }
        }

        public void SetFrequencyCorrection(int correction)
        {

        }

        public void SetGain(int gain)
        {
            _gain = gain;
        }

        public void SetGainMode(bool manual)
        {

        }

        public void SetIfGain(bool ifGain)
        {

        }

        public void SetSampleRate(int sampleRate)
        {
            _sampleRate = sampleRate;
        }
    }

}