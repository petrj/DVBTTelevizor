using System;
using System.Collections.Generic;
using System.Linq;
using RTLSDR.DAB;
using System.Collections.Concurrent;
using RTLSDR.Common;
using LoggerService;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Buffers;
using System.Numerics;

namespace DVBTTelevizor.TV;

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

/// <summary>
    /// Vypočítá medián ze spektrálních dat.
    /// Využívá sdílený fond paměti pro vysoký výkon a nízkou alokaci.
    /// </summary>
    /// <param name="spectrum">Pole hodnot výkonového spektra.</param>
    /// <returns>Hodnota mediánu (hladina šumu).</returns>
    public static int GetMedian(int[] spectrum)
    {
        if (spectrum == null || spectrum.Length == 0)
        {
            return 0;
        }

        int length = spectrum.Length;

        // Pronajmeme si pole z ArrayPoolu, abychom nezatěžovali GC alokacemi v každém snímku
        int[] rentedArray = ArrayPool<int>.Shared.Rent(length);

        try
        {
            // Zkopírujeme data do pronajatého pole
            Array.Copy(spectrum, rentedArray, length);

            // Seřadíme pouze reálnou délku dat (rentedArray může být o něco větší)
            Array.Sort(rentedArray, 0, length);

            int mid = length / 2;

            if (length % 2 == 0)
            {
                // Sudý počet prvků - průměr dvou prostředních
                return (rentedArray[mid - 1] + rentedArray[mid]) / 2;
            }
            else
            {
                // Lichý počet prvků - prostřední prvek
                return rentedArray[mid];
            }
        }
        finally
        {
            // Pole musíme vždy vrátit zpět do fondu
            ArrayPool<int>.Shared.Return(rentedArray);
        }
    }

    /// <summary>
    /// Najde ve spektru píky odpovídající širokopásmovému FM signálu.
    /// </summary>
    /// <param name="spectrum">Pole hodnot spektra.</param>
    /// <param name="medianNoise">Předem spočítaný medián (šum) spektra.</param>
    /// <param name="sampleRate">Vzorkovací frekvence SDR (např. 2048000).</param>
    /// <param name="thresholdOffset">O kolik musí pík přečnívat medián (např. 15 pro silný signál).</param>
    /// <returns>Seznam bodů (X = index vzorku, Y = hodnota).</returns>
    public static List<Point> FindFmPeaks(int[] spectrum, int medianNoise, int sampleRate, int thresholdOffset = 15)
    {
        List<Point> peaks = new List<Point>();

        if (spectrum == null || spectrum.Length < 3)
            return peaks;

        int fftSize = spectrum.Length;
        double binBandwidth = (double)sampleRate / fftSize;

        // FM signál má ~200 kHz. Pro kontrolu nám stačí ověřit,
        // že signál zvýšený nad šumem má šířku aspoň 100 kHz celkem (±50 kHz od středu)
        int checkOffsetBins = (int)(50000 / binBandwidth);

        // Ochrana proti přetečení indexů spektra při malém FFT
        if (checkOffsetBins < 1) checkOffsetBins = 1;

        int peakThreshold = medianNoise + thresholdOffset;
        int noiseThreshold = medianNoise + 3; // Hranice, kde už šum začíná růst v signál

        // Cyklus začíná a končí tak, abychom mohli bezpečně kontrolovat okolí
        for (int i = checkOffsetBins; i < spectrum.Length - checkOffsetBins; i++)
        {
            // 1. Je to lokální maximum?
            if (spectrum[i] > spectrum[i - 1] && spectrum[i] > spectrum[i + 1])
            {
                // 2. Je vrchol dostatečně vysoko nad šumem?
                if (spectrum[i] > peakThreshold)
                {
                    // 3. KONTROLA ŠÍŘKY: Je signál široký (FM), nebo je to jen úzká špička šumu?
                    if (spectrum[i - checkOffsetBins] > noiseThreshold &&
                        spectrum[i + checkOffsetBins] > noiseThreshold)
                    {
                        // Splňuje všechny podmínky -> přidáme jako System.Drawing.Point
                        peaks.Add(new Point(i, spectrum[i]));

                        // Přeskočíme kontrolu v šířce pásma tohoto nalezeného signálu,
                        // abychom nenašli více falešných vrcholů uvnitř jedné FM stanice
                        i += checkOffsetBins;
                    }
                }
            }
        }

        return peaks;
    }

/// <summary>
    /// Najde jednoduché píky, které jsou dostatečně široké.
    /// </summary>
    public static Dictionary<System.Drawing.Point,int> GetPeaks(int[] spectrum, int medianNoise, int thresholdOffset = 15)
    {
        var peaks = new Dictionary<System.Drawing.Point,int> ();

        int threshold = medianNoise + thresholdOffset;

        var vec = new Vector<int>(spectrum);

        // Okolí, které musí být také nad šumem (např. 5 bodů na každou stranu)
        int span = 5;

        // Cyklus běží tak, abychom nekoukali mimo pole
        for (int i = span; i < spectrum.Length - span; i++)
        {
            // 1. Je to lokální maximum? (tvůj původní nápad)
            //if (spectrum[i] > spectrum[i - 1] && spectrum[i] > spectrum[i + 1])
            //{
                // 2. Je to nad prahem šumu?
                if (spectrum[i] > threshold)
                {
                    // 3. JEDNODUCHÁ KONTROLA ŠÍŘKY:
                    // Koukneme se kousek doleva a kousek doprava.
                    // Pokud je to FM rádio, i tam musí být hodnota stále vysoko nad šumem.
                    if (spectrum[i - span] > threshold - 5 && spectrum[i + span] > threshold - 5)
                    {
                        // Přeskočíme okolí tohoto píku, abychom nenašli stejný kopec dvakrát
                        i += span;

                        // spocitame kolik je bodu vlevo a vpravo nad sum (median)
                        int leftCount = 0;
                        int rightCount = 0;
                        int j = i;
                        while (j>=0 && spectrum[j] > medianNoise)
                        {
                            leftCount++;
                            j--;
                        }
                        j = i;
                        while (j<spectrum.Length && spectrum[j] > medianNoise)
                        {
                            rightCount++;
                            j++;
                        }

                        peaks.Add(new Point(i, spectrum[i]), leftCount + rightCount);
                    }
                }
            //}
        }

        return peaks;
    }

    /// <summary>
    /// Vrátí pouze píky blízko středu spektra (
    /// spectrum.Length / 2 nebo zadaného centra) v rozsahu thresholdOffset.
    /// </summary>
    public static Dictionary<System.Drawing.Point,int> GetPeaksAroundCenter(int[] spectrum, int medianNoise, int thresholdOffset = 15, int center = -1)
    {
        if (spectrum == null || spectrum.Length == 0)
            return new Dictionary<System.Drawing.Point,int>();

        if (center < 0)
            center = spectrum.Length / 2;

        var peaks = GetPeaks(spectrum, medianNoise, thresholdOffset);

        return peaks
            .Where(pair => Math.Abs(pair.Key.X - center) <= thresholdOffset)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

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

    public string GetTextSpectrum(int width = 60, int height=20)
    {
        try
        {
            int[] spectrum;
            lock (_spectrumLock)
                {
                    spectrum = GetScaledSpectrum(width, height);
                }

                var sp = new char[height,width];

                var s = new StringBuilder();
                for (var row=0;row<height;row++)
                {
                    for (var col=0;col<width;col++)
                    {
                        sp[row,col] = ' ';
                    }
                }

                for (var i= 0;i<spectrum.Length;i++)
                {

                    for (var k=0;k<spectrum[i];k++)
                    {
                        char c;
                        if ((k>=0) && k<(0.25*spectrum[i]))
                        {
                            c = '\u2588';
                        } else
                        {
                            if ((k>=0.25*spectrum[i]) && k<(0.5*spectrum[i]))
                            {
                                c = '\u2593';
                            } else
                            {
                                if ((k>=0.5*spectrum[i]) && k<(0.75*spectrum[i]))
                                {
                                    c = '\u2592';
                                } else
                                {
                                    c = '\u2591';
                                }
                            }
                        }

                        var pos = height-k;
                        if (pos<0)
                        {
                                pos = 0;
                        }
                        if (pos>height-1)
                        {
                                pos = height-1;
                        }
                        sp[pos,i] = c;
                    }
                }

                for (var row=0;row<height;row++)
                {
                    for (var col=0;col<width;col++)
                    {
                        s.Append(sp[row,col]);
                    }
                    s.AppendLine();
                }

                return s.ToString();
        } catch (Exception ex)
        {
            _loggingService?.Error(ex);
            return "Spectrum error";
        }
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

    public void Stop()
    {
        _spectrumThreadWorker.Stop();
        _spectrumQueue.Clear();
    }

}
