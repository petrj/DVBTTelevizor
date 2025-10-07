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
    public class MenuViewModel : BaseNotifableObject
    {
        private bool _menuVisible = false;
        private int _fontSizeIndex = 0;
        private string _title = "Menu".Translated();
        public ObservableCollection<MenuItem> MenuItems { get; set; } = new ObservableCollection<MenuItem>();

        public MenuViewModel()
        {
            WeakReferenceMessenger.Default.Register<FontSizeChangedMessage>(this, (r, m) =>
            {
                SetFontSizeByIndex(m.Value);
            });
        }

        private int GetScaledSize(int index, int normalSize = 12)
        {
            switch (index)
            {
                case 1:
                    return Convert.ToInt32(Math.Round(normalSize * 1.12));
                case 2:
                    return Convert.ToInt32(Math.Round(normalSize * 1.25));
                case 3:
                    return Convert.ToInt32(Math.Round(normalSize * 1.5));
                case 4:
                    return Convert.ToInt32(Math.Round(normalSize * 1.75));
                case 5:
                    return Convert.ToInt32(Math.Round(normalSize * 2.0));
                default: return normalSize;
            }
        }

        public void SetFontSizeByIndex(int index = 0)
        {
            _fontSizeIndex = index;
            UpdateFontSize(GetScaledSize(index));
        }

        private void UpdateFontSize(int size)
        {
            foreach (MenuItem item in MenuItems)
            {
                item.FontSize = size;
                item.Update();
            }
            OnPropertyChanged(nameof(MenuItems));
            OnPropertyChanged(nameof(FontSize));
        }

        public int FontSize
        {
            get
            {
                return GetScaledSize(_fontSizeIndex);
            }
        }

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
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

        public void UpdateMenu(IEnumerable<MenuItem> items, string title)
        {
            Title = title;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (items != null)
                {
                    MenuItems.Clear();

                    var menuItems = new ObservableCollection<MenuItem>();
                    foreach (var item in items)
                    {
                        MenuItems.Add(item);
                    }
                }
                OnPropertyChanged(nameof(MenuItems));
            });
        }
    }
}

