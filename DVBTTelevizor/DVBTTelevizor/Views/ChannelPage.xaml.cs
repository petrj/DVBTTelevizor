using Android.Widget;
using Java.Lang;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChannelPage : ContentPage, IOnKeyDown
    {
        private ChannelPageViewModel _viewModel;
        private ChannelService _channelService;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        private KeyboardFocusableItemList _focusItems;
        private string _previousValue;

        public ChannelPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _channelService = channelService;

            BindingContext = _viewModel = new ChannelPageViewModel(_loggingService, _dialogService, driver, config);

            BuildFocusableItems();

            Appearing += ChannelPage_Appearing;

            EntryName.Focused += delegate {
                EntryName.CursorPosition = EntryName.Text.Length;
                _previousValue = _viewModel.Channel.Name;
            };
            EntryNumber.Focused += delegate
            {
                EntryNumber.CursorPosition = EntryNumber.Text.Length;
                _previousValue = _viewModel.Channel.Number;
            };

            EntryNumber.Unfocused += EntryNumber_Unfocused;
        }

        private void EntryNumber_Unfocused(object sender, FocusEventArgs e)
        {
            int num;
            if (!int.TryParse(EntryNumber.Text, out num) || (num < 1) || (num>32000))
            {
                _dialogService.Error($"Invalid number");
                _viewModel.Channel.Number = _previousValue;
                _viewModel.NotifyChannelChange();
            }

            Task.Run(async () =>
            {
                foreach (var ch in await _channelService.LoadChannels())
                {
                    if (ch.FrequencyAndMapPID == _viewModel.Channel.FrequencyAndMapPID)
                    {
                        continue;
                    }

                    if (ch.Number == num.ToString())
                    {
                        Device.BeginInvokeOnMainThread(
                            delegate
                            {
                                _dialogService.Error($"Number {num} already used");
                                _viewModel.Channel.Number = _previousValue;
                                _viewModel.NotifyChannelChange();
                            });

                        break;
                    }
                }
            });
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("Number", new List<View>() { NumberBoxView, EntryNumber}))
                .AddItem(KeyboardFocusableItem.CreateFrom("Name", new List<View>() { NameBoxView, EntryName }));

            _focusItems.OnItemFocusedEvent += ChannelPage_OnItemFocusedEvent;
        }

        private void ChannelPage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            // scroll to item
            ChannelPageScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height / 2, false);
        }

        private void ChannelPage_Appearing(object sender, EventArgs e)
        {
            _viewModel.NotifyFontSizeChange();
            _focusItems.FocusItem("OKButton");
        }

        public DVBTChannel Channel
        {
            get
            {
                return _viewModel.Channel;
            }
            set
            {
                _viewModel.Channel = value;
                Title = _viewModel.Channel.Name;
            }
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"ChannelPage OnKeyDown {key}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                    _focusItems.FocusNextItem();
                    break;

                case KeyboardNavigationActionEnum.Up:
                    _focusItems.FocusPreviousItem();
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:

                        switch (_focusItems.FocusedItemName)
                        {
                            case "Number":
                                EntryNumber.Focus();
                                break;

                            case "Name":
                                EntryName.Focus();
                                break;
                    }
                    break;
            }
        }

        public void OnTextSent(string text)
        {
            switch (_focusItems.FocusedItemName)
            {
                case "Number":
                    EntryNumber.Text = text;
                    break;
                case "Name":
                    EntryName.Text = text;
                    break;
            }
        }
    }
}