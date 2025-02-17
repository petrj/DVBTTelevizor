﻿using LoggerService;
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
        public DVBTChannel ChannelToDelete { get; set; } = null;

        private ChannelPageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        private KeyboardFocusableItemList _focusItems;
        private string _previousNumber;
        private string _previousName;

        public ChannelPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new ChannelPageViewModel(_loggingService, _dialogService, driver, config, channelService);

            BuildFocusableItems();

            Appearing += ChannelPage_Appearing;

            EntryName.Focused += delegate
            {
                EntryName.CursorPosition = EntryNumber.Text == null ? 0 : EntryName.Text.Length;
                _previousName = _viewModel.Channel.Name;
            };
            EntryNumber.Focused += delegate
            {
                EntryNumber.CursorPosition = EntryNumber.Text == null ? 0 : EntryNumber.Text.Length;
                _previousNumber = _viewModel.Channel.Number;
            };

            EntryNumber.Unfocused += EntryNumber_Unfocused;
            EntryName.Unfocused += EntryName_Unfocused;

            ButtonUp.Clicked += ButtonUp_Clicked;
            ButtonDown.Clicked += ButtonDown_Clicked;
        }

        public void SetAudioTracks(bool enabled, Dictionary<int, string> playingChannelAudioTracks, int activeId)
        {
             AudioTracksInfoVisible = enabled && playingChannelAudioTracks.Count > 0;

            _viewModel.SetAudioTracks(enabled ? playingChannelAudioTracks : new Dictionary<int, string>(), activeId);
        }

        public void SetSubtitles(bool enabled, Dictionary<int, string> playingChannelSubtitles, int activeId)
        {
            SubtitlesTracksInfoVisible = enabled && playingChannelSubtitles.Count > 0;

            _viewModel.SetSubtitleTracks(enabled ? playingChannelSubtitles : new Dictionary<int, string>(), activeId);
        }

        public bool Changed
        {
            get
            {
                return _viewModel.Changed;
            }
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
            if (_viewModel.Channel.Name == _previousName)
            {
                return;
            }

            Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(EntryName.Text))
                {
                    _viewModel.Channel.Name = _previousName;

                    Device.BeginInvokeOnMainThread( async () =>
                    {
                        await _dialogService.Error($"Invalid name");
                        _viewModel.NotifyChannelChange();
                    });
                }

                if (_viewModel.Channel.Name != _previousName)
                {
                    _viewModel.Changed = true;
                    await _viewModel.SaveChannels();
                }

                _previousName = null;
            });
        }

        private void EntryNumber_Unfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel.Channel.Number == _previousNumber)
            {
                return;
            }

            Task.Run(async () =>
            {
                int num;
                if (!int.TryParse(EntryNumber.Text, out num) || (num < 1) || (num > 32000))
                {
                    _viewModel.Channel.Number = _previousNumber;
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await _dialogService.Error($"Invalid number");
                        _viewModel.NotifyChannelChange();
                    });
                }
                else if (_viewModel.NumberUsed(EntryNumber.Text))
                {
                    _viewModel.Channel.Number = _previousNumber;
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await _dialogService.Error($"Number {num} is already used");
                        _viewModel.NotifyChannelChange();
                    });
                } else
                {
                    _viewModel.Changed = true;
                    await _viewModel.SaveChannels();
                }

                _previousNumber = null;
            });
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("Name", new List<View>() { NameBoxView, EntryName }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Number", new List<View>() { NumberBoxView, EntryNumber }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Up", new List<View>() { ButtonUp }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Down", new List<View>() { ButtonDown }))

                .AddItem(KeyboardFocusableItem.CreateFrom("KeyStop01", new List<View>() { KeyStop01 }))
                .AddItem(KeyboardFocusableItem.CreateFrom("KeyStop02", new List<View>() { KeyStop02 }))
                .AddItem(KeyboardFocusableItem.CreateFrom("KeyStop03", new List<View>() { KeyStop02 }))

                .AddItem(KeyboardFocusableItem.CreateFrom("Audio", new List<View>() { ButtonChangeAudio }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Subtitles", new List<View>() { ButtonChangeSubtitles }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Delete", new List<View>() { ButtonDeleteChannel }));

            _focusItems.OnItemFocusedEvent += ChannelPage_OnItemFocusedEvent;
        }

        private void ChannelPage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            if ( ((args.FocusedItem.Name == "Audio") && (!_viewModel.AudioTracksInfoVisible)) ||
                 ((args.FocusedItem.Name == "Subtitles") && (!_viewModel.SubtitlesTracksInfoVisible)) ||
                 ((args.FocusedItem.Name == "Delete") && (!_viewModel.DeleteVisible))
                )
            {
                Action action = null;

                switch (_focusItems.LastFocusDirection)
                {
                    case KeyboardFocusDirection.Next: action = delegate { _focusItems.FocusNextItem(); }; break;
                    case KeyboardFocusDirection.Previous: action = delegate { _focusItems.FocusPreviousItem(); }; break;
                    default: action = delegate { args.FocusedItem.DeFocus(); }; break;
                }

                action();
                return;
            }

            // scroll to item
            ChannelPageScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height / 2, false);
        }

        private void ChannelPage_Appearing(object sender, EventArgs e)
        {
            _viewModel.NotifyFontSizeChange();
            _focusItems.DeFocusAll();
           // AudioTracksListView.HeightRequest = _audioTracksListViewHeight;
        }

        public async Task Reload(string frequencyAndMapPID)
        {
            _viewModel.Changed = false;
            await _viewModel.Reload(frequencyAndMapPID);
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

        public bool DeleteVisible
        {
            get
            {
                return _viewModel.DeleteVisible;
            }
            set
            {
                _viewModel.DeleteVisible = value;
            }
        }

        public bool SubtitlesTracksInfoVisible
        {
            get
            {
                return _viewModel.SubtitlesTracksInfoVisible;
            }
            set
            {
                _viewModel.SubtitlesTracksInfoVisible = value;
            }
        }

        public bool AudioTracksInfoVisible
        {
            get
            {
                return _viewModel.AudioTracksInfoVisible;
            }
            set
            {
                _viewModel.AudioTracksInfoVisible = value;
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

                case KeyboardNavigationActionEnum.Left:
                    if (_focusItems.FocusedItemName == "Down")
                    {
                        _focusItems.FocusPreviousItem();
                    }
                    break;

                case KeyboardNavigationActionEnum.Right:
                    if (_focusItems.FocusedItemName == "Up")
                    {
                        _focusItems.FocusNextItem();
                    }
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

                        case "Audio":
                            ButtonChangeAudio_Clicked(this, null);
                            break;

                        case "Subtitles":
                            ButtonChangeSubtitles_Clicked(this, null);
                            break;

                        case "Delete":
                            ButtonDeleteChannel_Clicked(this, null);
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

        private void ButtonChangeAudio_Clicked(object sender, EventArgs e)
        {
            MessagingCenter.Send("", BaseViewModel.MSG_ChangeAudioTrackRequest);
        }

        private void ButtonChangeSubtitles_Clicked(object sender, EventArgs e)
        {
            MessagingCenter.Send("", BaseViewModel.MSG_ChangeSubtitlesRequest);
        }

        private async void ButtonDeleteChannel_Clicked(object sender, EventArgs e)
        {
            if (await _dialogService.Confirm($"Are you sure to delete channel {_viewModel.Channel.Name}?"))
            {
                ChannelToDelete = _viewModel.Channel;
                await Navigation.PopAsync();
            }
        }
    }
}