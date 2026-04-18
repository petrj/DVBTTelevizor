using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class BaseViewModel : BaseNotifableObject
    {
        protected ILoggingService _loggingService;
        protected string _publicDirectory;
        protected ITVConfiguration _configuration;

        private bool _menuVisible = false;

        public BaseViewModel(ILoggingService loggingService,
            IDriverConnector driver,
            ITVConfiguration tvConfiguration,
            IPublicDirectoryProvider publicDirectoryProvider)
        {
            _loggingService = loggingService;
            _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();
            _configuration = tvConfiguration;

            WeakReferenceMessenger.Default.Register<FontSizeChangedMessage>(this, (r, m) =>
            {
                _loggingService.Info($"BaseViewModel: FontSizeChanged");
                NotifyFontSizeChange();
            });
        }

        public bool MenuVisible
        {
            get
            {
                return _menuVisible;
            }
            set
            {
                _menuVisible = value;

                OnPropertyChanged(nameof(MenuVisible));
            }
        }

        public static string GetDVBTDriverTypeName(AppDriverTypeEnum? driverType)
        {
            switch (driverType)
            {
                case  AppDriverTypeEnum.DVBT:
                    return "DVB-T";
                case  AppDriverTypeEnum.FM:
                    return "FM (SDR Driver)";
                case  AppDriverTypeEnum.DAB:
                    return "DAB (SDR Driver)";
                default:
                    return "";
            }
        }

        public static string GetDVBTDriverShortName(AppDriverTypeEnum? driverType)
        {
            switch (driverType)
            {
                case  AppDriverTypeEnum.DVBT:
                    return "DVB-T";
                case  AppDriverTypeEnum.FM:
                    return "FM";
                case AppDriverTypeEnum.DAB:
                    return "DAB";
                default:
                    return String.Empty;
            }
        }

        //public async Task ChangeDriver(AppDriverTypeEnum driver)
        //{
        //    _loggingService.Info($"ChangeDriver");

        //    if (_configuration.AppDriverType == driver)
        //    {
        //        _loggingService.Info($"ChangeDriver: already using {driver}");
        //        return;
        //    }

        //    if ((_driver != null) && (_driver.Connected))
        //    {
        //        await _driver.Stop();
        //        await _driver.Disconnect();
        //    }

        //    _configuration.AppDriverType = driver;

        //    WeakReferenceMessenger.Default.Send(new InitDriverMessage(String.Empty));
        //    ////Task.Delay(500).Wait();
        //    ////WeakReferenceMessenger.Default.Send(new ConnectMessage(String.Empty));
        //}

        public static string DeviceFriendlyName
        {
            get
            {
                return $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}";
            }
        }

        public ITVConfiguration Config
        {
            get
            {
                return _configuration;
            }
        }

        public void NotifyFontSizeChange()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(FontSizeForCaption));
                OnPropertyChanged(nameof(FontSizeForPicker));
                OnPropertyChanged(nameof(FontSizeForLabel));
                OnPropertyChanged(nameof(FontSizeForChannelNumber));
                OnPropertyChanged(nameof(FontSizeForDetailNote));
                OnPropertyChanged(nameof(FontSizeForEntry));
                OnPropertyChanged(nameof(FontSizeForEPGTitle));
                OnPropertyChanged(nameof(ImageIconSize));
                OnPropertyChanged(nameof(FontSizeForDescription));
                OnPropertyChanged(nameof(FontSizeForLargeCaption));
            });
        }

        public int GetScaledSize(int normalSize)
        {
            switch (_configuration.AppFontSize)
            {
                case AppFontSizeEnum.AboveNormal:
                    return Convert.ToInt32(Math.Round(normalSize * 1.12));
                case AppFontSizeEnum.Big:
                    return Convert.ToInt32(Math.Round(normalSize * 1.25));
                case AppFontSizeEnum.Bigger:
                    return Convert.ToInt32(Math.Round(normalSize * 1.5));
                case AppFontSizeEnum.VeryBig:
                    return Convert.ToInt32(Math.Round(normalSize * 1.75));
                case AppFontSizeEnum.Huge:
                    return Convert.ToInt32(Math.Round(normalSize * 2.0));
                case AppFontSizeEnum.HugePlus:
                    return Convert.ToInt32(Math.Round(normalSize * 2.20));
                case AppFontSizeEnum.HugeTriplePLus:
                    return Convert.ToInt32(Math.Round(normalSize * 2.50));
                default: return normalSize;
            }
        }

        public string ImageIconSize
        {
            get
            {
                return GetScaledSize(20).ToString();
            }
        }

        public string ImageLargeIconSize
        {
            get
            {
                return GetScaledSize(30).ToString();
            }
        }

        public string FontSizeForLargeCaption
        {
            get
            {
                return GetScaledSize(25).ToString();
            }
        }

        public string FontSizeForCaption
        {
            get
            {
                return GetScaledSize(17).ToString();
            }
        }

        public string FontSizeForLabel
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public string FontSizeForDescription
        {
            get
            {
                return GetScaledSize(13).ToString();
            }
        }

        public string FontSizeForEPGTitle
        {
            get
            {
                return GetScaledSize(13).ToString();
            }
        }

        public string FontSizeForEntry
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public string FontSizeForPicker
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public string FontSizeForChannelNumber
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public string FontSizeForDetailNote
        {
            get
            {
                return GetScaledSize(9).ToString();
            }
        }

        public string HighlightedButtonColor
        {
            get { return "#41b3ff"; }
        }

        public string ButtonColor
        {
            get { return "Gray"; }
        }

    }
}
