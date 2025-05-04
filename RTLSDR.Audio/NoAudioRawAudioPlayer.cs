
using RTLSDR.Common;
using LoggerService;

namespace RTLSDR.Audio
{
    public class NoAudioRawAudioPlayer : IRawAudioPlayer
    {
        public void Init(AudioDataDescription audioDescription, ILoggingService loggingService)
        {
        }

        public bool PCMProcessed
        {
            get
            {
                return true;
            }
        }

        public void Play()
        {
        }

        public void AddPCM(byte[] data)
        {
        }

        public void Stop()
        {
        }
    }
}