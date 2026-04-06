using CommunityToolkit.Maui;
using DVBTTelevizor.MAUI.Platforms.Windows;
using DVBTTelevizor.TV;
using LibVLCSharp.MAUI;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace DVBTTelevizor.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLibVLCSharp()
                 .UseSkiaSharp()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<LoggerProvider>();
            builder.Services.AddSingleton<PublicDirectoryProvider>();
            builder.Services.AddSingleton<DVBTTelevizorConfiguration>();
            builder.Services.AddSingleton<RTLSDRDriverPlatformImplementation>();

            builder.Services.AddSingleton<IPublicDirectoryProvider, PublicDirectoryProvider>();
            builder.Services.AddSingleton<ITVConfiguration, DVBTTelevizorConfiguration>();
            builder.Services.AddSingleton<ILoggingProvider, LoggerProvider>();
            builder.Services.AddSingleton<IRTLSDRDriverPlatformImplementation, RTLSDRDriverPlatformImplementation>();

            builder.ConfigureMauiHandlers(handlers =>
{
    handlers.AddHandler(typeof(VideoView), typeof(VideoViewHandler));
});


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
