using NAudio.Wave;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LoggerService;

namespace RTLSDR.Audio
{
    public class AlsaSoundAudioPlayer : IRawAudioPlayer
    {
        [DllImport("libasound", CallingConvention = CallingConvention.Cdecl)]
        private static extern int snd_pcm_open(out IntPtr pcm, string name, int stream, int mode);

        [DllImport("libasound", CallingConvention = CallingConvention.Cdecl)]
        private static extern int snd_pcm_set_params(IntPtr pcm, int format, int access, int channels, int rate, int soft_resample, int latency);

        [DllImport("libasound", CallingConvention = CallingConvention.Cdecl)]
        private static extern int snd_pcm_writei(IntPtr pcm, IntPtr buffer, int size);

        [DllImport("libasound", CallingConvention = CallingConvention.Cdecl)]
        private static extern int snd_pcm_close(IntPtr pcm);

        IntPtr _pcm = IntPtr.Zero;

        const int SND_PCM_STREAM_PLAYBACK = 0;
        const int SND_PCM_FORMAT_U8 = 2;
        const int SND_PCM_FORMAT_U16_LE = 4;

        const int SND_PCM_ACCESS_MMAP_INTERLEAVED = 0;
        const int SND_PCM_ACCESS_MMAP_NONINTERLEAVED = 1;
        const int SND_PCM_ACCESS_MMAP_COMPLEX = 2;
        const int SND_PCM_ACCESS_RW_INTERLEAVED = 3;
        const int SND_PCM_ACCESS_RW_NONINTERLEAVED = 4;

        private ILoggingService _loggingService = null;

        public BalanceBuffer _ballanceBuffer = null;

        AudioDataDescription _audioDescription = null;

        public long _pcmBytesInput = 0;
        public long _pcmBytesOutput = 0;

        public bool PCMProcessed
        {
            get
            {
                return _pcmBytesInput > 0 && _pcmBytesOutput >= _pcmBytesInput;
            }
        }

        private void InitAlsa()
        {
            _loggingService.Info($"Initializing Alsa");

            if (_pcm != IntPtr.Zero)
            {
                snd_pcm_close(_pcm);
            }

            int err;

            //// Open PCM device for playback
            if ((err = snd_pcm_open(out _pcm, "default", SND_PCM_STREAM_PLAYBACK, 0)) < 0)
            {
                _loggingService.Info($"Alsa open error: {err}");
                return;
            }
            //// Set PCM parameters: format = 16-bit little-endian
            if ((err = snd_pcm_set_params(_pcm, SND_PCM_FORMAT_U8, SND_PCM_ACCESS_RW_INTERLEAVED, _audioDescription.Channels, _audioDescription.SampleRate, 0, 500000)) < 0)
            {
                _loggingService.Info($"Alsa set params error: {err}");
                return;
            }
        }

        public void Init(AudioDataDescription audioDescription, ILoggingService loggingService)
        {
            _loggingService = loggingService;
            _audioDescription = audioDescription;

            InitAlsa();

            _ballanceBuffer = new BalanceBuffer(_loggingService, (data) =>
            {
                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    _pcmBytesOutput += data.Length;

                    // Get the pointer to the first element of the array
                    IntPtr ptr = handle.AddrOfPinnedObject();

                    // Cast IntPtr to nint (which is the same as IntPtr in most platforms)
                    nint nativeInt = (nint)ptr;

                    var outputValue = snd_pcm_writei(_pcm, (nint)ptr, data.Length / ((_audioDescription.BitsPerSample / 8) * _audioDescription.Channels));
                    if (outputValue < 0)
                    {
                        _loggingService.Info($"Alsa error: {outputValue}");
                        InitAlsa();
                        return;
                    }
                }
                finally
                {
                    // Release the pinned handle
                    handle.Free();
                }
            });

            _ballanceBuffer.SetAudioDataDescription(_audioDescription);
        }

        public void Play()
        {

        }

        public void AddPCM(byte[] data)
        {
            _ballanceBuffer.AddData(data);
            _pcmBytesInput += data.Length;
        }

        public void Stop()
        {
            if (_ballanceBuffer != null)
            {
                _ballanceBuffer.Stop();
            }

            snd_pcm_close(_pcm);
        }
    }
}
