using System;

namespace RTLSDR.Common
{
    public interface IDemodulator
    {
        int Samplerate { get; set; }
        double AudioBitrate { get; }
        void AddSamples(byte[] IQData, int length);

        /// <summary>
        /// Inform that all data from input has been processed
        /// </summary>
        void Finish();
        void Stop();

        double PercentSignalPower { get; }

        event EventHandler OnDemodulated;
        event EventHandler OnFinished;
        event EventHandler OnServiceFound;
        event EventHandler OnSpectrumDataUpdated;
    }
}
