using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace RTLSDR.Common
{
    public class PowerCalculation
    {
        // https://www.tek.com/en/blog/calculating-rf-power-iq-samples

        private DateTime _lastCalculationTime;
        private double _lastPower;
        private double _maxPower;

        public PowerCalculation()
        {
            _lastCalculationTime = DateTime.MinValue;
            _lastPower = 0;
            _maxPower = MaxPower;
        }

        public static double MaxPower
        {
            get { return 238; }  // 10*ln(x) => 0 .. 238
        }

        public double GetPowerPercent(byte[] IQData, int bytesRead)
        {
            var now = DateTime.Now;

            var totalSeconds = (now - _lastCalculationTime).TotalSeconds;
            if (totalSeconds > 1)
            {
                if (IQData.Length > 0)
                {
                    _lastPower = GetAvgPower(IQData, bytesRead, 100);
                }
                else
                {
                    _lastPower = 0;
                }

                _lastCalculationTime = now;
            }

            return _lastPower / (_maxPower / 100);
        }

        public double GetPowerPercent(short[] IQData, int count)
        {
            var now = DateTime.Now;

            var c = count >= 100 ? 100 : count;

            var totalSeconds = (now - _lastCalculationTime).TotalSeconds;
            if (totalSeconds > 1)
            {
                if (IQData.Length > 0)
                {
                    _lastPower = GetAvgPower(IQData, c);
                }
                else
                {
                    _lastPower = 0;
                }

                _lastCalculationTime = now;
            }

            return _lastPower / (_maxPower / c);
        }

        public double GetPowerPercent(short[] IQData)
        {
            return GetPowerPercent(IQData, IQData.Length);
        }

        public static double GetCurrentPower(int I, int Q)
        {
            if (I == 0 && Q == 0) return 0;

            return 10 * Math.Log(10 * (Math.Pow(I, 2) + Math.Pow(Q, 2)));
        }

        public static double GetAvgPower(byte[] IQData, int bytesRead, int valuesCount = 100)
        {
            // first 100 numbers:

            if (valuesCount > IQData.Length / 2)
            {
                valuesCount = IQData.Length / 2;
            }

            if (valuesCount > bytesRead * 2)
            {
                valuesCount = bytesRead * 2;
            }

            double avgPower = 0;

            for (var i = 0; i < valuesCount * 2; i = i + 2)
            {
                var power = GetCurrentPower(IQData[i + 0] - 127, IQData[i + 1] - 127);

                avgPower += power / valuesCount;
            }

            return avgPower;
        }

        public static double GetAvgPower(short[] IQData, int valuesCount = 100)
        {
            // first 100 numbers:

            if (valuesCount > IQData.Length / 2)
            {
                valuesCount = IQData.Length / 2;
            }

            double avgPower = 0;

            for (var i = 0; i < valuesCount * 2; i = i + 2)
            {
                var power = GetCurrentPower(IQData[i + 0], IQData[i + 1]);

                avgPower += power / valuesCount;
            }

            return avgPower;
        }
    }
}
