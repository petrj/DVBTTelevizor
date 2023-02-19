using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class ConfigViewModel : INotifyPropertyChanged
    {
        protected DVBTTelevizorConfiguration _config;

        public ConfigViewModel(DVBTTelevizorConfiguration config)
        {
            _config = config;
        }

        public DVBTTelevizorConfiguration Config
        {
            get
            {
                return _config;
            }
        }

        public static bool ChannelExists(ObservableCollection<DVBTChannel> channels, long frequency, long ProgramMapPID)
        {
            foreach (var ch in channels)
            {
                if (ch.Frequency == frequency &&
                    ch.ProgramMapPID == ProgramMapPID)
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetNextChannelNumber(ObservableCollection<DVBTChannel> channels)
        {
            var res = 0;

            foreach (var ch in channels)
            {
                int n;
                if (int.TryParse(ch.Number, out n))
                {
                    if (n>res)
                    {
                        res = n;
                    }
                }
            }

            return res +1;
        }

        #region INotifyPropertyChanged

        protected bool SetProperty<T>(ref T backingStore, T value,
           [CallerMemberName] string propertyName = "",
           Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Font & Image Size

        public int GetScaledSize(int normalSize)
        {
            switch (_config.AppFontSize)
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

        public void NotifyFontSizeChange()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(FontSizeForCaption));
                OnPropertyChanged(nameof(FontSizeForPicker));
                OnPropertyChanged(nameof(FontSizeForLabel));
                OnPropertyChanged(nameof(FontSizeForChannelNumber));
                OnPropertyChanged(nameof(FontSizeForDetailNote));
                OnPropertyChanged(nameof(FontSizeForEntry));
                OnPropertyChanged(nameof(ImageIconSize));
                OnPropertyChanged(nameof(FontSizeForDescription));
            });
        }

        public string ImageIconSize
        {
            get
            {
                return GetScaledSize(20).ToString();
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

        #endregion
    }
}
