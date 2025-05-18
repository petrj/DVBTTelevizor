using LoggerService;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR.Common
{
    public class BitRateCalculation
    {
        private ILoggingService _loggingService;
        private DateTime _lastSpeedCalculationTime;
        private int _bytesReadFromLastSpeedCalculationTime;
        private double _bitRate;
        private string _description;

        public BitRateCalculation(ILoggingService loggingService, string description)
        {
            _loggingService = loggingService;

            _bytesReadFromLastSpeedCalculationTime = 0;
            _lastSpeedCalculationTime = DateTime.Now;
            _bitRate = 0;
            _description = description;
        }

        public string BitRateAsString
        {
            get
            {
                if (_bitRate > 1000000)
                {
                    return $"{(_bitRate / 1000000).ToString("N2").PadLeft(20)}  Mb/s";
                }
                else
                {
                    return $"{(_bitRate / 1000).ToString("N0").PadLeft(20)}  Kb/s";
                }
            }
        }

        public double UpdateBitRate(int bytesRead)
        {
            var now = DateTime.Now;

            var totalSeconds = (now - _lastSpeedCalculationTime).TotalSeconds;
            if (totalSeconds > 1)
            {
                _bitRate = _bytesReadFromLastSpeedCalculationTime * 8 / totalSeconds;
                _lastSpeedCalculationTime = now;
                _bytesReadFromLastSpeedCalculationTime = 0;
            }
            else
            {
                _bytesReadFromLastSpeedCalculationTime += bytesRead;
            }

            return _bitRate;
        }
    }
}
