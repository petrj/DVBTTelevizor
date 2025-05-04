using System.Threading;
using RTLSDR.Common;
using System.Collections.Generic;
using System.Collections.Concurrent;
using LoggerService;
using System.Runtime.CompilerServices;

namespace RTLSDR.Audio;

public class BalanceBuffer
{    
    private Thread _thread = null;
    private bool _running = false;
    //private int _cycleMSDelay = 100;
    private Action<byte[]> _actionPlay = null;
    private DateTime _timeStarted = DateTime.Now;
    private ConcurrentQueue<byte[]> _queue = new ConcurrentQueue<byte[]>();
            
    private const int MinThreadNoDataMSDelay = 25;
    private const int CycleMSDelay = 100; // 10x per sec

    private const int PreliminaryOutputBufferMS = 1000;

    private long _pcmBytesInput = 0;
    private long _pcmBytesOutput = 0;

    private ILoggingService _loggingService;

    private AudioDataDescription _audioDescription;

    public int BufferReadMS { get; set; } = 100;

    public BalanceBuffer(ILoggingService loggingService, Action<byte[]> actionPlay)
    {
        _loggingService = loggingService;

        _actionPlay = actionPlay;

        _loggingService.Info("Starting Balance buffer");        
        _timeStarted = DateTime.Now;

        _queue = new ConcurrentQueue<byte[]>();

        _audioDescription = new AudioDataDescription();

        _running = true;
        _thread = new Thread(ThreadLoop);
        _thread.Start();
    }

    public void SetAudioDataDescription(AudioDataDescription audioDescription)
    {
        _audioDescription = audioDescription;

        _loggingService.Info($"Adio Balance      : Samplerate: {_audioDescription.SampleRate}");
        _loggingService.Info($"Adio Channels     : Channels: {_audioDescription.Channels}");
        _loggingService.Info($"Adio BitsPerSample: BitsPerSample: {_audioDescription.BitsPerSample}");
    }

    public void AddData(byte[] data)
    {
        //_loggingService.Info($"Adding {data.Length} bytes to balance buffer");        
        _queue.Enqueue(data);
    }

    public void Stop()
    {
        _running = false;
    }

    private void ThreadLoop()
    {
        _loggingService.Info("Starting Balance thread");        

        DateTime cycleStartTime = DateTime.MinValue;
        DateTime lastNotifiTime = DateTime.MinValue;
        List<byte> _audioBuffer = new List<byte>();
        byte[] data = null;

        var loopStartTime = DateTime.Now;

        try
        {
            while (_running)
            {
                cycleStartTime = DateTime.Now; // start of next cycle

                var totalBytesRead = 0;

                // wait for data
                while ((DateTime.Now-cycleStartTime).TotalMilliseconds<CycleMSDelay)
                {
                    // fill buffer;
                    var ok = _queue.TryDequeue(out data);
            
                    if (data != null && data.Length > 0)
                    {
                        totalBytesRead+=data.Length;
                        _audioBuffer.AddRange(data);
                        _pcmBytesInput += data.Length;
                    } 

                    Thread.Sleep(MinThreadNoDataMSDelay);
                } 

                var bytesPerSample = (_audioDescription.BitsPerSample/8)*_audioDescription.Channels;
                var bytesPerSec = _audioDescription.SampleRate*bytesPerSample;
                var secsFromLastCycle = (DateTime.Now - cycleStartTime).TotalSeconds;                

                var cycleBytes = (Convert.ToInt32(secsFromLastCycle * bytesPerSec)/bytesPerSample)*(bytesPerSample)-bytesPerSample;

                //cycleBytes += cycleBytes;

                var preliminaryOutputBufferBytes = bytesPerSec*PreliminaryOutputBufferMS/1000;

                var missingBytes = "";
                if ((_pcmBytesInput-_pcmBytesOutput)<preliminaryOutputBufferBytes)
                {
                    missingBytes = $"missing {(preliminaryOutputBufferBytes - cycleBytes)/1000} kB ";
                    cycleBytes = preliminaryOutputBufferBytes - cycleBytes;
                }

                // deque bytesFromLastCycle bytes:
                var bufferState = "OK";
                if ((cycleBytes > 0 ) && (_audioBuffer.Count>0))
                {
                    if (cycleBytes > _audioBuffer.Count)
                    {
                        cycleBytes = _audioBuffer.Count;
                    }

                    //_loggingService.Debug($"Dequeue {bytesFromLastCycle} bytes");
                    var thisCycleBytes = _audioBuffer.GetRange(0, Convert.ToInt32(cycleBytes));
                    _audioBuffer.RemoveRange(0, Convert.ToInt32(cycleBytes));

                    _actionPlay(thisCycleBytes.ToArray());
                    _pcmBytesOutput += cycleBytes;
                } else
                {
                  // no data in buffer! Notify slow CPU
                    bufferState = "Empty!";
                }

                if ((DateTime.Now-lastNotifiTime).TotalSeconds>2)
                {
                    _loggingService.Debug($" Audio buffer: {bufferState}  (processed {_pcmBytesOutput/1000} kB)");
                    lastNotifiTime = DateTime.Now;
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex);
        }

        _loggingService.Info("Balance thread stopped");        
    }

    private void ReceiveData(byte[] AUData)
    {    
        if (AUData == null)
        {
            _loggingService.Info("No data");
            return;
        }

        _loggingService.Info($"Received: {AUData.Length} bytes");     
    }
}
