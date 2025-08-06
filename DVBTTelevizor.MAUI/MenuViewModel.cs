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
        private string _title = "Menu".Translated();
        public ObservableCollection<MenuItem> MenuItems { get; set; } = new ObservableCollection<MenuItem>();

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

