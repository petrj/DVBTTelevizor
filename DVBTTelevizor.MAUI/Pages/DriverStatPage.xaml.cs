using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;
using Microsoft.Maui.Controls.PlatformConfiguration;
using RTLSDR.Common;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Linq.Expressions;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class DriverStatPage : ContentPage, IOnKeyDown
{
    private DriverStatPageViewModel _viewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private System.Drawing.Point[]? _spectrum = null;

    private KeyboardFocusableItemList _focusItems;
    private SpectrumWorker? _spectrumWorker = null;
    private bool _visible = false;

    public DriverStatPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _viewModel = new DriverStatPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        _driver.RawDataReceived += _driver_RawDataReceived;

        BuildFocusableItems();

        // Start a timer to update the spectrum ~60 FPS
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(200), () =>
        {
            if (_visible)
            {
                UpdateSpectrum();
                SpectrumCanvas.InvalidateSurface();
            }

            return true; // repeat
        });
    }

    private void _driver_RawDataReceived(object? sender, EventArgs e)
    {
        if (_visible && e is RawDataReceivedEventArgs args)
        {
            _spectrumWorker?.AddData(args.Data, args.DataSize);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _visible = false;
        //_spectrumWorker?.Stop();
        //_spectrumWorker = null;
    }

    private void UpdateSpectrum()
    {
        if (_spectrumWorker == null)
        {
            _spectrumWorker = new SpectrumWorker(_loggingService, 16384, _driver.DriverType == TV.AppDriverTypeEnum.DAB ? AudioTools.DABSampleRate : AudioTools.FMSampleRate);
        }

        _spectrum = _spectrumWorker?.Spectrum;
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("PositionBoxView1", new List<View>() { PositionBoxView1 }))
            .AddItem(KeyboardFocusableItem.CreateFrom("PositionBoxView2", new List<View>() { PositionBoxView2 }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));

        _visible = true;
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"DriverStatPage Page OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Down:
            case KeyboardNavigationActionEnum.Right:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusNextItem(true);
                });
                break;

            case KeyboardNavigationActionEnum.Up:
            case KeyboardNavigationActionEnum.Left:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusPreviousItem(true);
                });
                break;

            case KeyboardNavigationActionEnum.Back:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                });
                break;

            case KeyboardNavigationActionEnum.OK:

                if (_focusItems.FocusedItem == null)
                    return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    switch (_focusItems.FocusedItem.Name)
                    {

                    }
                });
                break;
        }
    }
    public void OnTextSent(string text)
    {
        _loggingService.Debug($"DriverStatPage Page OnTextSent {text}");
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        if (_spectrum == null || _spectrum.Length == 0)
        {
            return;
        }

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);

        int width = e.Info.Width;
        int height = e.Info.Height;

        // Find min/max dB values for normalization
        float minDb = float.MaxValue;
        float maxDb = float.MinValue;

        foreach (var point in _spectrum)
        {
            float db = point.Y;
            if (db < minDb) minDb = db;
            if (db > maxDb) maxDb = db;
        }

        // Avoid division by zero
        if (maxDb <= minDb)
        {
            maxDb = minDb + 1;
        }

        using var paint = new SKPaint
        {
            Color = SKColors.ForestGreen,
            StrokeWidth = 2,
            IsAntialias = false
        };

        float barWidth = (float)width / _spectrum.Length;
        float dbRange = maxDb - minDb;
        float centerY = height / 2f;

        float lastx = 0;
        float lasty = centerY;

        for (int i = 0; i < _spectrum.Length; i++)
        {
            // Normalize dB value to range -1 to 1 (centered)
            float normalizedValue = ((_spectrum[i].Y - minDb) / dbRange) * 2f - 1f;

            // Map to canvas: positive dB values go up, negative go down
            float barHeight = normalizedValue * (height / 2f);
            float x = i * barWidth;
            float y = centerY - barHeight;

            if (i > 0)
            {
                canvas.DrawLine(lastx, lasty, x, y, paint);
            }

            lastx = x;
            lasty = y;
        }
    }
}