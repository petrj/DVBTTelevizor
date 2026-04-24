using CommunityToolkit.Mvvm.Messaging;
using DVBTelevizor;
using DVBTTelevizor.MAUI.Messages;
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
            UpdateSpectrum();
            SpectrumCanvas.InvalidateSurface();
            return true; // repeat
        });
    }

    private void _driver_RawDataReceived(object? sender, EventArgs e)
    {
        if (e is RawDataReceivedEventArgs args)
        {
            _spectrumWorker?.AddData(args.Data, args.DataSize);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _spectrumWorker?.Stop();
        _spectrumWorker = null;
    }

    private void UpdateSpectrum()
    {
        if (_spectrumWorker == null)
        {
            return;
        }

        _spectrum = _spectrumWorker?.Spectrum;
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_driver != null && _driver.Connected)
        {
            _spectrumWorker = new SpectrumWorker(_loggingService, 16384, _driver.DriverType == TV.AppDriverTypeEnum.DAB ? AudioTools.DABSampleRate : AudioTools.FMSampleRate);
        }

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"DriverStatPage Page OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

        switch (keyAction)
        {
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
        if (_spectrum == null)
        {
            return;
        }

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);

        using var paint = new SKPaint
        {
            Color = SKColors.Lime,
            StrokeWidth = 2,
            IsAntialias = false
        };

        int width = e.Info.Width;
        int height = e.Info.Height;

        float barWidth = (float)width / _spectrum.Length;

        float lastx = 0;
        float lasty = height;

        for (int i = 0; i < _spectrum.Length; i++)
        {
            // Use Point.Y as value
            float value = (float)_spectrum[i].Y / 200f; // normalize 0–1
            float barHeight = value * height;
            float x = i * barWidth;
            float y = height - barHeight;

            if (i>0)
            {
                canvas.DrawLine(lastx, lasty, x, y, paint);
            }

            lastx = x;
            lasty = y;
        }
    }
}