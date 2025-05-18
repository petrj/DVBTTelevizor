using System;
using System.Net.Sockets;
using System.Net;
using NAudio.Wave;
using RTLSDR.Common;
using LoggerService;
using System.Collections.Concurrent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RTLSDR.Audio
{
    public class NAudioRawAudioPlayer : IRawAudioPlayer
    {
        private ILoggingService _loggingService;

        public static WaveOutEvent _outputDevice;
        public static BufferedWaveProvider _bufferedWaveProvider;

        private DateTime _lastAudioProcessTime = DateTime.MinValue;
        private double _inputBitRate = 0;

        private AudioDataDescription _audioDescription;

        private BitRateCalculation _bitrateCalculation;

        private BalanceBuffer _ballanceBuffer = null;

        public NAudioRawAudioPlayer(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            if (_loggingService == null)
            {
                _loggingService = new DummyLoggingService();
            }

            _bitrateCalculation = new BitRateCalculation(_loggingService, "NAudio BitRate");
            _audioDescription = new AudioDataDescription()
            {
                BitsPerSample = 16,
                Channels = 2,
                SampleRate = 48000
            };
        }

        public bool PCMProcessed
        {
            get
            {
                return false; // no Balance buffer
            }
        }

        public void Init(AudioDataDescription audioDescription, ILoggingService loggingService)
        {
            _audioDescription = audioDescription;
            _outputDevice = new WaveOutEvent();
            var waveFormat = new WaveFormat(audioDescription.SampleRate, audioDescription.BitsPerSample, audioDescription.Channels);
            _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
            //_bufferedWaveProvider.BufferDuration = new TimeSpan(0,0,10);
            //_bufferedWaveProvider.BufferLength = 10 * (audioDescription.SampleRate * audioDescription.Channels * audioDescription.BitsPerSample / 8);

            _outputDevice.Init(_bufferedWaveProvider);

            _ballanceBuffer = new BalanceBuffer(_loggingService, (data) =>
            {
                if (data == null)
                return;

                _inputBitRate = _bitrateCalculation.UpdateBitRate(data.Length);

                _bufferedWaveProvider.AddSamples(data, 0, data.Length);
            });

            _ballanceBuffer.SetAudioDataDescription(audioDescription);
        }

        public void Play()
        {
            if (_outputDevice != null)
            {
                _outputDevice.Play();
            }
        }

        public void AddPCM(byte[] data)
        {
            _ballanceBuffer.AddData(data);
        }

        public void Stop()
        {
            if (_outputDevice != null)
            {
                _outputDevice.Stop();
            }
            if (_bufferedWaveProvider != null)
            {
                _bufferedWaveProvider.ClearBuffer();
            }
        }
    }
}