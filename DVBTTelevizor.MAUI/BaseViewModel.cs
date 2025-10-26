using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
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
        protected IDriverConnector _driver;
        protected string _publicDirectory;
        protected ITVConfiguration _configuration;

        public ObservableCollection<string> Drivers { get; set; } = new ObservableCollection<string>();

        public BaseViewModel(ILoggingService loggingService,
            IDriverConnector driver,
            ITVConfiguration tvConfiguration,
            IPublicDirectoryProvider publicDirectoryProvider)
        {
            _loggingService = loggingService;
            _driver = driver;
            _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();
            _configuration = tvConfiguration;

            WeakReferenceMessenger.Default.Register<FontSizeChangedMessage>(this, (r, m) =>
            {
                _loggingService.Info($"BaseViewModel: FontSizeChanged");
                NotifyFontSizeChange();
            });
        }

        public virtual async Task FillDrivers()
        {
            Drivers.Clear();

            Drivers.Add("DVBT".Translated());
            Drivers.Add("FM".Translated());

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                OnPropertyChanged(nameof(Drivers));
                OnPropertyChanged(nameof(DriverTypeIndex));
            });
        }

        public DVBTDriverTypeEnum SelectedDriverType
        {
            get
            {
                switch (DriverTypeIndex)
                {
                    case 1:
                        return DVBTDriverTypeEnum.RTLSDRTCPIPFMDriver;
                    case 0:
                    default:
                        return DVBTDriverTypeEnum.AndroidDVBTDriver;
                }
            }
        }

        public int DriverTypeIndex
        {
            get
            {
                // DVBTDriverTypeEnum
                //   *  AndroidDVBTDriver = 0,            => 0
                //      AndroidTestingDVBTDriver = 1,
                //      TestTuneDriver = 2,
                //   *  RTLSDRTCPIPFMDriver = 3,          => 1
                //      RTLSDRFMDriver = 4

                switch (_configuration.DVBTDriverType)
                {
                    case DVBTDriverTypeEnum.AndroidDVBTDriver:
                        return 0;
                    case DVBTDriverTypeEnum.RTLSDRTCPIPFMDriver:
                        return 1;
                    default:
                        return 0;
                }
            }
            set
            {
                switch (value)
                {
                    case 0:
                        _configuration.DVBTDriverType = DVBTDriverTypeEnum.AndroidDVBTDriver;
                        break;
                    case 1:
                        _configuration.DVBTDriverType = DVBTDriverTypeEnum.RTLSDRTCPIPFMDriver;
                        break;
                }
                OnPropertyChanged(nameof(DriverTypeIndex));
            }
        }

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
    }
}
