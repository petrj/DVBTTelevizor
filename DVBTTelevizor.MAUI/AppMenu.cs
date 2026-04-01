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
        private bool _visible = false;

        public event EventHandler<MenuVisibleChangedEventArgs>? MenuVisibleChanged;

        public AppMenu(Menu menu)
        {
            _menu = menu;
        }

        public bool IsVisible
        {
            get { return _visible; }
        }

        public AppFontSizeEnum FontSize
        {
            get { return _appFontSize; }
            set { _appFontSize = value; }
        }

        public List<MenuItem> MenuItems
        {
            get { return _menuItems; }
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
            _visible = true;
            MenuVisibleChanged?.Invoke(this, new MenuVisibleChangedEventArgs(true));
        }

        public void HideMenu()
        {
            _menu.MenuVisible = false;
            _visible = false;
            MenuVisibleChanged?.Invoke(this, new MenuVisibleChangedEventArgs(false));
        }

        public void ShowConfirmChangeDriverMenu(IDriverConnector driver, AppDriverTypeEnum currentDriverType, AppDriverTypeEnum newDriverType)
        {
            //if (_driver == null || !_driver.Connected)
            //{
            //    return;
            //}

            ShowMenu();

            Clear();

            var newDriverName = BaseViewModel.GetDVBTDriverTypeName(newDriverType);
            var currentDriverName = BaseViewModel.GetDVBTDriverTypeName(currentDriverType);

            // driver: Not installed => installed > connected

            string question;
            if (driver != null && !driver.Connected)
            {
                question = "Connect to {0}?".Translated(newDriverName);
            } else
            {
                question = "Disconnect {0} and connect {1}?"
                    .Translated(currentDriverName, newDriverName);
            }

            var menuItem = AddItem(_menu.CreateMenuItem("menuChangeDriver", question, "driver.png"));
            menuItem.DriverType = newDriverType;

            AddItem(_menu.CreateMenuItem("menuCancelChangeDriver", "Continue using {0}".Translated(currentDriverName), "back.png"));

            Finish("Please confirm driver change:".Translated());
        }

        public void ShowFMorDABConnectMenu()
        {
            ShowMenu();

            Clear();

            AddItem(_menu.CreateMenuItem("menuConnectFM", "FM".Translated(), "driver.png"));
            AddItem(_menu.CreateMenuItem("menuConnectDAB", "DAB".Translated(), "driver.png"));

            var title = "Connect RTLSDR driver".Translated();

            AddItem(_menu.CreateMenuItem("menuCancel", "Cancel".Translated(), "cancel.png"));

            Finish(title);
        }


        public void ShowRetryTuneMenu(IDriverConnector driver)
        {
            ShowMenu();

            Clear();

            AddItem(_menu.CreateMenuItem("menuRetryTune", "Retry".Translated(), "refresh.png"));

            var title = "Tuning failed.".Translated();
            if (driver == null || !driver.DriverInstalled)
            {
                title += "Driver is not installed.".Translated();
                AddItem(_menu.CreateMenuItem("menuInstallDriver", "Install driver".Translated(), "driver.png"));
            }
            else
            {
                title += "Check USB connection.".Translated();

                if (!driver.Connected)
                {
                    AddItem(_menu.CreateMenuItem("menuConnectDriver", "Connect".Translated(), "refresh.png"));
                }
            }

            AddItem(_menu.CreateMenuItem("menuDriver", "Driver ...".Translated(), "menu.png"));
            AddItem(_menu.CreateMenuItem("menuCancel", "Cancel".Translated(), "cancel.png"));

            Finish(title);
        }

        public void ShowRetryPlayMenu(IDriverConnector driver, string channelId)
        {
            ShowMenu();

            Clear();

            var playItem = AddItem(_menu.CreateMenuItem($"menuRetryPlay", "Retry".Translated(), "refresh.png"));
            playItem.ChannelId = channelId;


            var title = "Playing failed.".Translated();
            if (driver == null || !driver.DriverInstalled)
            {
                title += "Driver is not installed.".Translated();
                AddItem(_menu.CreateMenuItem("menuInstallDriver", "Install driver".Translated(), "driver.png"));
            }
            else
            {
                title += "Check USB connection.".Translated();

                if (!driver.Connected)
                {
                    AddItem(_menu.CreateMenuItem("menuConnectDriver", "Connect".Translated(), "refresh.png"));
                }
            }

            AddItem(_menu.CreateMenuItem("menuDriver", "Driver ...".Translated(), "driver.png"));
            AddItem(_menu.CreateMenuItem("menuCancelPlay", "Cancel".Translated(), "cancel.png"));

            Finish("Playing failed. Check USB connection".Translated());

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

        public MenuItem? GetSelectedMenuItem()
        {
            if (MenuItems == null)
            {
                return null;
            }

            foreach (var item in MenuItems)
            {
                if (item.Selected)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
