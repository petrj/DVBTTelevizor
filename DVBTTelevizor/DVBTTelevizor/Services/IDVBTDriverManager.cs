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
using System.IO;
using LoggerService;
using System.Runtime.InteropServices;
using MPEGTS;
using DVBTTelevizor.Models;


namespace DVBTTelevizor
{
    public interface IDVBTDriverManager
    {
        DVBTDriverConfiguration Configuration { get; set; }

        bool Started { get; }

        Stream VideoStream { get; }

        bool Recording { get; }
        bool ReadingStream { get; }
        string RecordFileName { get; }

        string DataStreamInfo { get; set; }
        List<byte> Buffer { get; }

        void Start();
        Task Disconnect();

        void StopReadStream();

        Task StartRecording();
        void StopRecording();

        Task<PlayResult> Play(long frequency, long bandwidth, int deliverySystem, List<long> PIDs);
        Task<bool> Stop();

        Task<bool> CheckStatus();
        Task<DVBTStatus> GetStatus();
        Task<DVBTVersion> GetVersion();
        Task<DVBTResponse> Tune(long frequency, long bandwidth, int deliverySystem);
        Task<DVBTResponse> SetPIDs(List<long> PIDs);
        Task WaitForBufferPIDs(List<long> PIDs, int msTimeout = 3000);
        Task<DVBTCapabilities> GetCapabalities();
        Task<SearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs);
        Task<TuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning);
        Task<EITScanResult> ScanEPGForChannel(long freq, int programMapPID, int msTimeout = 2000);
        Task<EITScanResult> ScanEPG(long freq, int msTimeout = 2000);
        Task<SearchMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true);

        EITManager GetEITManager(long freq);
    }
}
