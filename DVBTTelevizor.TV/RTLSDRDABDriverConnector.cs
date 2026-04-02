using LoggerService;
using MPEGTS;
using RTLSDR;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.TV
{
    public class RTLSDRDABDriverConnector : RTLSDRDriverConnector
    {
        private DateTime _lastStationTest = DateTime.MinValue;
        private Dictionary<long, bool> _stationOnFrequency = new Dictionary<long, bool>();

        public RTLSDRDABDriverConnector(ILoggingService loggingService, ISDR driver, IDemodulator demodulator)
            : base(loggingService, driver, demodulator)
        {
        }

        public override AppDriverTypeEnum DriverType => AppDriverTypeEnum.DAB;

        public override Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverCapabilities()
                {
                    supportedDeliverySystems = 0,
                    minFrequency = 174928000,
                    maxFrequency = 239200000,
                    frequencyStepSize = 1712000
                };
            });
        }

        public override void Connect()
        {
            _stationOnFrequency.Clear();
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
