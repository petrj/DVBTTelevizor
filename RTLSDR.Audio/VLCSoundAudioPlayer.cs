using NAudio.Wave;
using LoggerService;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using System.IO;

namespace RTLSDR.Audio
{
    public class VLCSoundAudioPlayer : IRawAudioPlayer
    {
        private MemoryStream _stream;
        private MediaPlayer _mediaPlayer;
        private Media _media;
        private LibVLC _libVLC;

        public void Init(AudioDataDescription audioDescription, ILoggingService loggingService)
        {
            Core.Initialize();
            _libVLC = new LibVLC(enableDebugLogs: true);
            _stream = new MemoryStream();

            var mediaOptions = new[] {
                ":demux=rawaud",
                $":rawaud-channels={audioDescription.Channels}",
                $":rawaud-samplerate={48000}",
                ":rawaud-fourcc=s16l"
            };
            _media = new Media(_libVLC, new StreamMediaInput(_stream), mediaOptions);

            _mediaPlayer = new MediaPlayer(_media);
            _mediaPlayer.Volume = 100;
        }

        public bool PCMProcessed
        {
            get
            {
                return true; // no Balance buffer
            }
        }

        public void Play()
        {
            _mediaPlayer.Play();
        }

        public void AddPCM(byte[] data)
        {
            _stream.Write(data, 0, data.Length);
        }

        public void Stop()
        {
            _mediaPlayer.Stop();
        }
    }
}

