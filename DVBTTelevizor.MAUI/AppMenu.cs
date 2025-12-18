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

        public MenuItem AddItem(MenuItem item)
        {
            _menuItems.Add(item);
            return item;
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


        public void ShowChangeDriverMenu(IDriverConnector _driver, string currentDriverName, int newDriverTypeIndex)
        {
            if (_driver == null || !_driver.Connected)
            {
                return;
            }

            ShowOrHideMenu();

            if (_menu.IsVisible)
            {
                Clear();

                var configDriverName = _driver.Configuration.DeviceName;
                var newDriverName = BaseViewModel.GetDVBTDriverTypeName(newDriverTypeIndex);

                var menuItem = AddItem(_menu.CreateMenuItem("menuChangeDriver", "Disconnect {0} ({1}) and connect {2}?"
                    .Translated(configDriverName, currentDriverName, newDriverName), "refresh.png"));
                menuItem.DriverTypeIndex = newDriverTypeIndex;

                AddItem(_menu.CreateMenuItem("menuCancel", "Stay connected to {0}".Translated(currentDriverName), "close.png"));

                Finish("Please confirm change of driver:".Translated());
            }
        }

        public void ShowRetryTuneMenu()
        {
            ShowOrHideMenu();

            if (_menu.IsVisible)
            {
                Clear();

                AddItem(_menu.CreateMenuItem("menuRetryTune", "Retry".Translated(), "refresh.png"));
                AddItem(_menu.CreateMenuItem("menuDriver", "Driver ...".Translated(), "driver.png"));
                AddItem(_menu.CreateMenuItem("menuCancel", "Cancel".Translated(), "cancel.png"));

                Finish("Tuning failed. Check USB connection".Translated());
            }
        }

        public void ShowRetryPlayMenu(string channelId)
        {
            ShowOrHideMenu();

            if (_menu.IsVisible)
            {
                Clear();

                var playItem = AddItem(_menu.CreateMenuItem($"menuRetryPlay-{channelId}", "Retry".Translated(), "refresh.png"));
                playItem.ChannelId = channelId;

                AddItem(_menu.CreateMenuItem("menuDriver", "Driver ...".Translated(), "driver.png"));
                AddItem(_menu.CreateMenuItem("menuCancelPlay", "Cancel".Translated(), "cancel.png"));

                Finish("Playing failed. Check USB connection".Translated());
            }
        }

        public void ShowConfirmMenu(string title, string titleYes, string titleNo, string actionConfirm, string actionNotConfirm)
        {
            ShowOrHideMenu();

            if (_menu.IsVisible)
            {
                Clear();

                AddItem(_menu.CreateMenuItem(actionConfirm, titleYes, "confirm.png"));
                AddItem(_menu.CreateMenuItem(actionNotConfirm, titleNo, "cancel.png"));

                Finish(title);
            }
        }
    }
}
