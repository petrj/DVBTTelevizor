using LoggerService;
using MPEGTS;
using RTLSDR;
using RTLSDR.Common;
using RTLSDR.DAB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace DVBTTelevizor.TV
{
    public class RTLSDRDABDriverConnector : RTLSDRDriverConnector
    {
        private DateTime _lastStationTest = DateTime.MinValue;
        private Dictionary<long, bool> _stationOnFrequency = new Dictionary<long, bool>();
        private ILoggingService _loggingService;

        public RTLSDRDABDriverConnector(ILoggingService loggingService, ISDR driver, IDemodulator demodulator, int startupFrequency)
            : base(loggingService, driver, demodulator, startupFrequency)
        {
            _loggingService = loggingService;
        }

        public override AppDriverTypeEnum DriverType => AppDriverTypeEnum.DAB;

        public override DriverStreamTypeEnum DVBTDriverStreamType
        {
            get
            {
                return DriverStreamTypeEnum.RAWAACAudio;
            }
        }

        public override Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverCapabilities()
                {
                    supportedDeliverySystems = 0,
                    minFrequency = 174928000,
                    maxFrequency = 239200000,
                    frequencyStepSize = 1712000,
                    SuccessFlag = true
                };
            });
        }

        public override void Connect()
        {
            _stationOnFrequency.Clear();
            _driver.Settings.SDRSampleRate = AudioTools.DABSampleRate;
            base.Connect();
        }

        public override Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            return Task.Run(() =>
            {
                return new DVBTDriverSearchProgramMapPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.NoProgramFound
                };
            });
        }
    }
}
