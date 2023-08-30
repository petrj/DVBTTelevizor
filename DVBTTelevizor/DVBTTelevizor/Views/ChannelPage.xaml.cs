using Android.Widget;
using Java.Lang;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        private KeyboardFocusableItemList _focusItems;
        private string _previousValue;

        public ChannelPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, Action<string> onEditedChannelChanged)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new ChannelPageViewModel(_loggingService, _dialogService, driver, config);

            _viewModel.OnEditedChannelChanged = onEditedChannelChanged;

            BuildFocusableItems();

            Appearing += ChannelPage_Appearing;

            EntryName.Focused += delegate
            {
                EntryName.CursorPosition = EntryNumber.Text == null ? 0 : EntryName.Text.Length;
                _previousValue = _viewModel.Channel.Name;
            };
            EntryNumber.Focused += delegate
            {
                EntryNumber.CursorPosition = EntryNumber.Text == null ? 0 : EntryNumber.Text.Length;
                _previousValue = _viewModel.Channel.Number;
            };

            EntryNumber.Unfocused += EntryNumber_Unfocused;
            EntryName.Unfocused += EntryName_Unfocused;

            ButtonUp.Clicked += ButtonUp_Clicked;
            ButtonDown.Clicked += ButtonDown_Clicked;
        }

        public void SetChannels(ObservableCollection<DVBTChannel> Channels, ObservableCollection<DVBTChannel> AllChannels)
        {
            _viewModel.Channels = Channels;
            _viewModel.AllChannels = AllChannels;
        }

        private void ButtonDown_Clicked(object sender, EventArgs e)
        {
            _viewModel.DownCommand.Execute(null);
        }

        private void ButtonUp_Clicked(object sender, EventArgs e)
        {
            _viewModel.UpCommand.Execute(null);
        }

        protected override bool OnBackButtonPressed()
        {
            return base.OnBackButtonPressed();
        }

        private void EntryName_Unfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.Channel.Name == _previousValue)
            {
                return;
            }

            Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(EntryName.Text))
                {
                    _viewModel.Channel.Name = _previousValue;

                    Device.BeginInvokeOnMainThread( async () =>
                    {
                        await _dialogService.Error($"Invalid name");
                        _viewModel.NotifyChannelChange();
                    });
                }

                if (_viewModel.Channel.Name != _previousValue)
                {
                    // saving
                    _viewModel.OnEditedChannelChanged(_viewModel.Channel.FrequencyAndMapPID);
                }

                _previousValue = null;
            });
        }

        private void EntryNumber_Unfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.Channel.Number == _previousValue)
            {
                return;
            }

            Task.Run(async () =>
            {
                int num;
                if (!int.TryParse(EntryNumber.Text, out num) || (num < 1) || (num > 32000))
                {
                    await _dialogService.Error($"Invalid number");
                    _viewModel.Channel.Number = _previousValue;
                    _viewModel.NotifyChannelChange();
                }
                else
                {
                    foreach (var ch in _viewModel.AllChannels)
                    {
                        if (ch.FrequencyAndMapPID == _viewModel.Channel.FrequencyAndMapPID)
                        {
                            continue;
                        }

                        if (ch.Number == num.ToString())
                        {
                            _viewModel.Channel.Number = _previousValue;

                            Device.BeginInvokeOnMainThread(
                                delegate
                                {
                                    _dialogService.Error($"Number {num} already used");
                                    _viewModel.NotifyChannelChange();
                                });

                            break;
                        }
                    }
                }

                if (_viewModel.Channel.Number != _previousValue)
                {
                    // saving
                    _viewModel.OnEditedChannelChanged(_viewModel.Channel.FrequencyAndMapPID);
                }

                _previousValue = null;
            });
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("Number", new List<View>() { NumberBoxView, EntryNumber }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Name", new List<View>() { NameBoxView, EntryName }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Up", new List<View>() { ButtonUp }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Down", new List<View>() { ButtonDown }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ChannelEnd", new List<View>() { ChannelEndLabel }));

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
            _focusItems.DeFocusAll();
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

        public bool StreamInfoVisible
        {
            get
            {
                return _viewModel.StreamInfoVisible;
            }
            set
            {
                _viewModel.StreamInfoVisible = value;
            }
        }

        public bool StreamBitRateVisible
        {
            get
            {
                return _viewModel.StreamBitRateVisible;
            }
            set
            {
                _viewModel.StreamBitRateVisible = value;
            }
        }

        public bool SignalStrengthVisible
        {
            get
            {
                return _viewModel.SignalStrengthVisible;
            }
            set
            {
                _viewModel.SignalStrengthVisible = value;
            }
        }

        public string StreamVideoSize
        {
            get
            {
                return _viewModel.StreamVideoSize;
            }
            set
            {
                _viewModel.StreamVideoSize = value;
            }
        }

        public string Bitrate
        {
            get
            {
                return _viewModel.Bitrate;
            }
            set
            {
                _viewModel.Bitrate = value;
            }
        }

        public string SignalStrength
        {
            get
            {
                return _viewModel.SignalStrength;
            }
            set
            {
                _viewModel.SignalStrength = value;
            }
        }

        public string StreamAudioTracks
        {
            get
            {
                return _viewModel.StreamAudioTracks;
            }
            set
            {
                _viewModel.StreamAudioTracks = value;
            }
        }

        public string StreamSubtitles
        {
            get
            {
                return _viewModel.StreamSubtitles;
            }
            set
            {
                _viewModel.StreamSubtitles = value;
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

                            case "Up":
                                ButtonUp_Clicked(this, null);
                                break;

                            case "Down":
                                ButtonDown_Clicked(this, null);
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