namespace DVBTelevizor;

using System;
using RTLSDR.DAB;
using System.Collections.Concurrent;
using RTLSDR.Common;
using LoggerService;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Drawing;
using System.Runtime.ExceptionServices;

/// <summary>
/// The spectrum worker.
/// </summary>
public class SpectrumWorker
{
    private readonly int _fftSize;
    private readonly float _sampleRate;

    private readonly float[] _window;
    private readonly FComplex[] _fftBuffer;

    private readonly ThreadWorker<byte[]> _spectrumThreadWorker;
    private readonly ConcurrentQueue<byte[]> _spectrumQueue = new ConcurrentQueue<byte[]>();
    private readonly ILoggingService? _loggingService = null;
    private int _queueSize = 0;
    private readonly System.Drawing.Point[] _spectrum;

    private readonly object _spectrumLock = new object();

    public SpectrumWorker(ILoggingService? loggingService, int fftSize, float sampleRate)
    {
        if ((fftSize & (fftSize - 1)) != 0)
            throw new ArgumentException("FFT size must be power of two");

        _fftSize = fftSize;
        _sampleRate = sampleRate;
        _loggingService = loggingService;

        _window = CreateHannWindow(fftSize);
        _fftBuffer = new FComplex[fftSize];
        _spectrum = new System.Drawing.Point[_fftSize];

        _spectrumThreadWorker = new ThreadWorker<byte[]>(loggingService, "SPECTRUM");
        _spectrumThreadWorker.SetThreadMethod(SpectrumThreadWorkerGo, 500);
        _spectrumThreadWorker.Start();
    }

    public System.Drawing.Point[] Spectrum
    {
        get
        {
            return _spectrum;
        }
    }

    public int[] GetScaledSpectrum(int width=1638, int height=20)
    {
        double xFactor = _fftSize / width;

        float epsilon = 0.0001f;
        var res = new int[width];
        var k=0;
        var j = 0;
        long sum = 0;
        var min = int.MaxValue;
        var max = int.MinValue;

        var localMax = int.MinValue;

        for (var i= 0;i<_fftSize;i++)
        {
            sum += Spectrum[i].Y;
            j++;

            if (Spectrum[i].Y>localMax)
            {
                localMax = Spectrum[i].Y;
            }

            if (Math.Abs(j-xFactor) < epsilon)
            {
                res[k] = Math.Abs(localMax);
                if (min>res[k])
                {
                    min = res[k];
                }
                if (max<res[k])
                {
                    max = res[k];
                }
                j=0;
                sum = 0;
                localMax = int.MinValue;
                k++;

                if (k>=width-1)
                {
                    break;
                }
            }
        }

        var spectrumHeight = Math.Abs(max);
        if (spectrumHeight<height)
        {
            spectrumHeight = height;
        }
        double yFactor = (double)height /spectrumHeight;

        for (var i= 0;i<width;i++)
        {
            res[i] = Convert.ToInt32(yFactor * res[i]);
        }

        return res;
    }

    public void Stop()
    {
        _spectrumThreadWorker.Stop();
    }

    private void SpectrumThreadWorkerGo(object? data = null)
    {
        try
        {
            if (_queueSize < 2*_fftSize)
            {
                return; // buffer is not filled yet
            }

            var buff = new byte[2*_fftSize];
            int size = 0;

            while (size < 2 * _fftSize)
            {
                _spectrumQueue.TryDequeue(out byte[]? b);
                if (b == null)
                {
                    break; // no data ?
                }
                Buffer.BlockCopy(b, 0, buff, size, b.Length + size > 2 * _fftSize ? 2 * _fftSize - size : b.Length);
                size += b.Length;
            }

            if (size < 2*_fftSize)
            {
                throw new NoSamplesException();
            }

            // clear queue
            _spectrumQueue.Clear();
            _queueSize = 0;

            PrepareBufferFromBytes(buff);

            lock (_spectrumLock)
            {
                UpdateSpectrum();
            }
        }
        catch (Exception ex)
        {
            _loggingService?.Error(ex, "Error while computing spectrum");
        }
    }

    public void AddData(byte[] data, int size)
    {
        if (_queueSize >= 2*_fftSize)
        {
            return; // buffer is full
        }

        // resize data[] to its size (trim data)
        var buff = new byte[size];
        Buffer.BlockCopy(data, 0, buff, 0, size);

        _queueSize += size;
        _spectrumQueue.Enqueue(buff);
    }

    private void PrepareBufferFromBytes(byte[] raw)
    {
        for (int i = 0; i < _fftSize; i++)
        {
            float iVal = (raw[2 * i] - 128) / 128.0f;
            float qVal = (raw[2 * i + 1] - 128) / 128.0f;

            _fftBuffer[i].Real = iVal * _window[i];
            _fftBuffer[i].Imaginary = qVal * _window[i];
        }
    }

    private void UpdateSpectrum()
    {
        // FFT
        Fourier.FFTBackward(_fftBuffer);

        // Shift DC
        FFTShift(_fftBuffer);

        // Magnitude → dB
        for (int i = 0; i < _fftSize; i++)
        {
            float re = _fftBuffer[i].Real;
            float im = _fftBuffer[i].Imaginary;

            float mag = MathF.Sqrt(re * re + im * im);
            _spectrum[i].Y = Convert.ToInt32(20.0f * MathF.Log10(mag + 1e-12f));
        }

        // Frequency axis
        for (int i = 0; i < _fftSize; i++)
        {
            _spectrum[i].X = Convert.ToInt32( ((float)i / _fftSize - 0.5f) * _sampleRate);
        }
    }

    private static float[] CreateHannWindow(int n)
    {
        float[] w = new float[n];

        for (int i = 0; i < n; i++)
        {
            w[i] = 0.5f *
                (1.0f - MathF.Cos(2.0f * MathF.PI * i / (n - 1)));
        }

        return w;
    }

    private static void FFTShift(FComplex[] data)
    {
        int half = data.Length / 2;

        for (int i = 0; i < half; i++)
        {
            FComplex tmp = data[i];
            data[i] = data[i + half];
            data[i + half] = tmp;
        }
    }
}
