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
using Java.IO;

namespace DVBTTelevizor
{
    public interface IDVBTDriverManager
    {
        DVBTDriverConfiguration Configuration { get; set; }

        bool Connected { get; }

        DVBTDriverStreamTypeEnum DVBTDriverStreamType { get; }

        Stream VideoStream { get; }
        string StreamUrl { get; }

        bool Recording { get; }
        bool ReadingStream { get; }
        bool Streaming { get; }
        string RecordFileName { get; }

        string DataStreamInfo { get; set; }

        long LastTunedFreq { get; }

        void Connect();
        Task Disconnect();

        void StartStream();
        void StopStream();

        Task StartRecording();
        void StopRecording();

        Task<bool> Stop();

        Task<bool> CheckStatus();
        Task<DVBTStatus> GetStatus();
        Task<DVBTVersion> GetVersion();
        Task<DVBTResponse> Tune(long frequency, long bandwidth, int deliverySystem);
        Task<TuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, List<long> PIDs, bool fastTuning);
        Task<DVBTResponse> SetPIDs(List<long> PIDs);
        Task WaitForBufferPIDs(List<long> PIDs, int msTimeout = 3000);
        Task<DVBTCapabilities> GetCapabalities();
        Task<SearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs);
        Task<SearchMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true);

        Task<EITScanResult> ScanEPG(int msTimeout = 2000);
    }
}
