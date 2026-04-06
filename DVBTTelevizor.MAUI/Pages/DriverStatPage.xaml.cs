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


    private int[]? spectrum = null;
    private readonly Random rnd = new Random();

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

        spectrum = _spectrumWorker.GetScaledSpectrum(400,200);
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("Minus", new List<View>() { MinusButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Plus", new List<View>() { PlusButton }));

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
                        case "Minus":
                            MinusButton_Clicked(this, new EventArgs());
                            break;
                        case "Plus":
                            PlusButton_Clicked(this, new EventArgs());
                            break;
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"DriverStatPage Page OnTextSent {text}");
    }

    private void MinusButton_Clicked(object sender, EventArgs e)
    {
        _viewModel.Minus();
    }

    private void PlusButton_Clicked(object sender, EventArgs e)
    {
        _viewModel.Plus();
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        if (spectrum == null)
        {
            return;
        }

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);

        var paint = new SKPaint
        {
            Color = SKColors.Lime,
            StrokeWidth = 2,
            IsAntialias = false
        };

        int width = e.Info.Width;
        int height = e.Info.Height;

        float barWidth = (float)width / spectrum.Length;

        for (int i = 0; i < spectrum.Length; i++)
        {
            float value = spectrum[i] / 200f; // normalize 0–1
            float barHeight = value * height;
            float x = i * barWidth;
            float y = height - barHeight;

            canvas.DrawLine(x, height, x, y, paint);
        }
    }
}