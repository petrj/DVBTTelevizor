using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

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

        #region Font Size

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
            OnPropertyChanged(nameof(FontSizeForCaption));
            OnPropertyChanged(nameof(FontSizeForPicker));
            OnPropertyChanged(nameof(FontSizeForLabel));
        }

        public string FontSizeForCaption
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public string FontSizeForLabel
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
