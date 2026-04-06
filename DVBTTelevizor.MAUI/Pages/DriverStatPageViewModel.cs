using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Plugin.InAppBilling;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class DriverStatPageViewModel : BaseViewModel
    {
        private string _stat = string.Empty;
        private int _fontSize = 0;

        public DriverStatPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {

            WeakReferenceMessenger.Default.Register<DriverUpdateStatMessage>(this, (r, m) =>
            {
                Task.Run(async () =>
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        Stat = m.Value == null ? String.Empty : m.Value.Stat;
                    });
                });
            });
        }

        public string Stat
        {
            get { return _stat; }
            set
            {
                if (_stat != value)
                {
                    _stat = value;
                    OnPropertyChanged(nameof(Stat));
                }
            }
        }

        public int AppFontSize
        {
            get
            {
                var normalSize = 12;
                switch (FontSize)
                {
                    case 0:
                        return Convert.ToInt32(Math.Round(normalSize * 1.12));
                    case 1:
                        return Convert.ToInt32(Math.Round(normalSize * 1.25));
                    case 2:
                        return Convert.ToInt32(Math.Round(normalSize * 1.5));
                    case 3:
                        return Convert.ToInt32(Math.Round(normalSize * 1.75));
                    case 4:
                        return Convert.ToInt32(Math.Round(normalSize * 2.0));
                    case 5:
                        return Convert.ToInt32(Math.Round(normalSize * 2.20));
                    case 6:
                        return Convert.ToInt32(Math.Round(normalSize * 2.50));
                    default: return normalSize;
                }
            }
        }

        public int FontSize
        {
            get
            {
                return _fontSize;
            }
            set
            {
                _fontSize = value;

                Task.Run(async () =>
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        OnPropertyChanged(nameof(FontSize));
                        OnPropertyChanged(nameof(AppFontSize));
                        OnPropertyChanged(nameof(PlusVisible));
                        OnPropertyChanged(nameof(MinusVisible));
                    });
                });
            }
        }

        public async void Plus()
        {
            if (FontSize < 6)
            {
                FontSize++;
            }
        }

        public async void Minus()
        {
            if (FontSize > 0)
            {
                FontSize--;
            }
        }

        public bool PlusVisible
        {
            get
            {
                return FontSize < 7;
            }
        }

        public bool MinusVisible
        {
            get
            {
                return FontSize > 0;
            }
        }
    }

}

