using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;
using Microsoft.Maui.Handlers;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI
{
    public class AppMenu
    {
        private Menu _menu = null;
        private AppFontSizeEnum _appFontSize = AppFontSizeEnum.Normal;

        private List<MenuItem> _menuItems = new List<MenuItem>();

        public event EventHandler<MenuVisibleChangedEventArgs>? MenuVisibleChanged;

        public AppMenu(Menu menu)
        {
            _menu = menu;
        }

        public AppFontSizeEnum FontSize
        {
            get { return _appFontSize; }
            set { _appFontSize = value; }
        }

        public void Clear()
        {
            _menuItems.Clear();
        }

        public void AddItem(MenuItem item)
        {
            _menuItems.Add(item);
        }

        public void Finish(string title)
        {
            _menu.UpdateMenu((int)_appFontSize,title, _menuItems);
        }

        public void ShowOrHideMenu()
        {
            if (_menu.MenuVisible)
            {
                HideMenu();
            }
            else
            {
                ShowMenu();
            }
        }

        public void ShowMenu()
        {
            _menu.MenuVisible = true;
            MenuVisibleChanged?.Invoke(this, new MenuVisibleChangedEventArgs(true));
        }

        public void HideMenu()
        {
            _menu.MenuVisible = false;
            MenuVisibleChanged?.Invoke(this, new MenuVisibleChangedEventArgs(false));
        }


        public void BuildChangeDriverMenu(IDriverConnector _driver, DVBTDriverTypeEnum selectedDriverType, int previousSelectedDriverTypeIndex)
        {
            if (_driver == null || !_driver.Connected)
            {
                return;
            }

            ShowOrHideMenu();

            if (_menu.IsVisible)
            {
                Clear();

                var currentDriverName = _driver.Configuration.DeviceName;
                var currentDriverType = BaseViewModel.GetDVBTDriverTypeName(selectedDriverType);
                var previousDriverType = BaseViewModel.GetDVBTDriverTypeName(previousSelectedDriverTypeIndex);

                AddItem(_menu.CreateMenuItem("menuChangeDriver", "Disconnect {0} ({1}) and connect {2}?"
                    .Translated(currentDriverType, currentDriverName, previousDriverType), "refresh.png"));
                AddItem(_menu.CreateMenuItem("menuCancel", "Stay connected to {0}".Translated(currentDriverType), "close.png"));

                Finish("Please confirm change of driver:".Translated());
            }
        }
    }
}
