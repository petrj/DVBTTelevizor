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
using System.IO;
using System.Runtime.InteropServices;
using MPEGTS;

namespace DVBTTelevizor
{
    public interface IDVBTDriver
    {
        DVBTDriverStateEnum State { get; }

        DVBTDriverConfiguration Configuration { get; set; }

        bool Connected { get; }

        DVBTDriverStreamTypeEnum DVBTDriverStreamType { get; }

        Stream VideoStream { get; }
        string StreamUrl { get; }

        bool Recording { get; }
        bool ReadingStream { get; }
        bool Streaming { get; }
        string RecordFileName { get; }

        string PublicDirectory { get; set; }

        string DataStreamInfo { get; set; }

        long Bitrate { get; }

        long LastTunedFreq { get; }

        bool DriverStreamDataAvailable { get; }

        void Connect();
        Task Disconnect();

        void StartStream();
        void StopStream();

        Task StartRecording();
        void StopRecording();

        Task<bool> Stop();

        Task<bool> CheckStatus();
        Task<DVBTDriverStatus> GetStatus();
        Task<DVBTDriverVersion> GetVersion();
        Task<DVBTDriverResponse> Tune(long frequency, long bandwidth, int deliverySystem);
        Task<DVBTDriverResponse> SetPIDs(List<long> PIDs);

        Task<DVBTDriverTuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning);
        Task<DVBTDriverTuneResult> WaitForSignal(bool fastTuning);
        Task<bool> DriverSendingData(int readMsTimeout = 500);
        Task<DVBTDriverSearchPIDsResult> SetupChannelPIDs(long mapPID, bool fastTuning);

        Task WaitForBufferPIDs(List<long> PIDs, int readMsTimeout = 500, int msTimeout = 6000);
        Task<DVBTDriverCapabilities> GetCapabalities();
        Task<DVBTDriverSearchPIDsResult> SearchProgramPIDs(long mapPID, bool setPIDsAndSync);
        Task<DVBTDriverSearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs);
        Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true);

        Task<EITScanResult> ScanEPG(int msTimeout = 2000);
        Task CheckPIDs();

        //void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);
        event EventHandler StatusChanged;
    }
}
