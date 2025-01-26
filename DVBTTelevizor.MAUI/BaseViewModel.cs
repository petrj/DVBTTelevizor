using LoggerService;
using System;
using System.Collections.Generic;
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
        protected ITVCConfiguration _configuration;
        protected IDialogService _dialogService;

        public BaseViewModel(ILoggingService loggingService,
            IDriverConnector driver,
            ITVCConfiguration tvConfiguration,
            IDialogService dialogService,
            IPublicDirectoryProvider publicDirectoryProvider)
        {
            _loggingService = loggingService;
            _driver = driver;
            _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();
            _configuration = tvConfiguration;
            _dialogService = dialogService;
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
