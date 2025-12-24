using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
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
        private DriverTypeEnum? _ignoreDriver = null;

        private bool _DVBTDriverActive = false;
        private bool _FMDriverActive = false;

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

            WeakReferenceMessenger.Default.Register<DriverUpdateStateMessage>(this, (r, m) =>
            {
                UpdateActiveDriverType();
                NotifyDriverChange();
            });
        }

        public virtual async Task FillDrivers()
        {
            Drivers.Clear();

            Drivers.Add("DVBT".Translated());
            Drivers.Add("RTLSDR - FM".Translated());

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                OnPropertyChanged(nameof(Drivers));
            });

            UpdateActiveDriverType();
        }

        public void UpdateActiveDriverType()
        {
            Task.Run(async () =>
            {
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (_configuration.DVBTDriverType == DriverTypeEnum.RTLSDRDriver)
                        {
                            IgnoreDriver = DriverTypeEnum.RTLSDRDriver;
                        } else
                        {
                           IgnoreDriver = DriverTypeEnum.AndroidDVBTDriver;
                        }

                        FMDriverActive = _configuration.DVBTDriverType == DriverTypeEnum.RTLSDRDriver;
                        DVBTDriverActive = !FMDriverActive;
                    });
                }
                finally
                {

                }
            });
        }

        public virtual async Task ReConnectDriver()
        {
            if (
                 (DVBTDriverActive && (!(_driver is DVBTDriverConnector)))
                 ||
                 (FMDriverActive && (!(_driver is RTLSDRDriverConnector)))
                )
            {
                // switch driver
                WeakReferenceMessenger.Default.Send(new InitDriverMessage(String.Empty));
            }

            await Task.Delay(1000);
            WeakReferenceMessenger.Default.Send(new ConnectMessage(String.Empty));

            //WeakReferenceMessenger.Default.Send(new DVBTDriverStateChangedMessages(null));
        }

        public static string GetDVBTDriverTypeName(DriverTypeEnum driverType)
        {
            // DVBTDriverTypeEnum
            //   *  AndroidDVBTDriver = 0,            => 0
            //      AndroidTestingDVBTDriver = 1,
            //      TestTuneDriver = 2,
            //   *  RTLSDRTCPIPFMDriver = 3,          => 1
            //      RTLSDRFMDriver = 4
            switch ((int)driverType)
            {
                case 0:
                case 1:
                case 2:
                    return "DVBT";
                case 3:
                case 4:
                    return "RTLSDR - FM";
                default:
                    return "";
            }
        }

        public bool DVBTDriverActive
        {
            get
            {
                return _DVBTDriverActive;
                //switch (_configuration.DVBTDriverType)
                //{
                //    case DriverTypeEnum.RTLSDRDriver:
                //        return false;
                //    default:
                //        return true;
                //}
            }
            set
            {
                _DVBTDriverActive = value;
                //if (value)
                //{
                //    if (_prevActiveDriverType != _configuration.DVBTDriverType)
                //    {
                //        _prevActiveDriverType = _configuration.DVBTDriverType;
                //    }
                //    _configuration.DVBTDriverType = DriverTypeEnum.AndroidDVBTDriver;
                //}
                //else
                //{
                //    _configuration.DVBTDriverType = DriverTypeEnum.RTLSDRDriver;
                //}

                NotifyDriverChange();
            }
        }

        public bool FMDriverActive
        {
            get
            {
                return _FMDriverActive;
                //switch (_configuration.DVBTDriverType)
                //{
                //    case DriverTypeEnum.RTLSDRDriver:
                //        return true;
                //    default:
                //        return false;
                //}
            }
            set
            {
                _FMDriverActive = value;
                //if (value)
                //{
                //    if (_prevActiveDriverType != _configuration.DVBTDriverType)
                //    {
                //        _prevActiveDriverType = _configuration.DVBTDriverType;
                //    }
                //    _configuration.DVBTDriverType = DriverTypeEnum.RTLSDRDriver;
                //}
                //else
                //{
                //    _configuration.DVBTDriverType = DriverTypeEnum.AndroidDVBTDriver;
                //}

                NotifyDriverChange();
            }
        }

        public async Task ChangeDriver(DriverTypeEnum driver)
        {
            _loggingService.Info($"ChangeDriver");

            if ((_driver != null) && (_driver.Connected))
            {
                await _driver.Stop();
                await _driver.Disconnect();
            }

            _configuration.DVBTDriverType = driver;

            await ReConnectDriver();
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

        public void NotifyDriverChange()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(FMDriverActive));
                OnPropertyChanged(nameof(DVBTDriverActive));

                OnPropertyChanged(nameof(ConnectedDevice));
                OnPropertyChanged(nameof(DriverStateStatus));
            });
        }

        public DriverTypeEnum? IgnoreDriver
        {
            get
            {
                return _ignoreDriver;
            }
            set
            {
                _ignoreDriver = value;
            }
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

        public string ConnectedDevice
        {
            get
            {
                if (_driver == null ||
                    _driver.Configuration == null ||
                    String.IsNullOrWhiteSpace(_driver.Configuration.DeviceName))
                {
                    return "No compatible device".Translated();
                }

                return _driver.Configuration.DeviceName;
            }
        }

        public string DriverStateStatus
        {
            get
            {
                if (_driver == null || !_driver.DriverInstalled)
                {
                    return "Driver not installed!".Translated();
                }

                if (_driver.Connected)
                {
                    return "Connected".Translated();
                }

                return "Disconnected".Translated();
            }
        }

    }
}
