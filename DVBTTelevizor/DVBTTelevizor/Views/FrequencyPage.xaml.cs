using DVBTTelevizor.Models;
using LoggerService;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FrequencyPage : ContentPage, IOnKeyDown
    {
        private FrequencyViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;

        private KeyboardFocusableItemList _focusItems;

        public FrequencyPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new FrequencyViewModel(loggingService, dialogService, driver, config);

            BuildFocusableItems();
        }

        public long MinFrequencyKHz
        {
            get { return _viewModel.MinFrequencyKHz; }
            set { _viewModel.MinFrequencyKHz = value; }
        }

        public long MaxFrequencyKHz
        {
            get { return _viewModel.MaxFrequencyKHz; }
            set { _viewModel.MaxFrequencyKHz = value; }
        }

        public long FrequencyKHz
        {
            get { return _viewModel.FrequencyKHz; }
            set { _viewModel.FrequencyKHz = value; }
        }

        public long FrequencyKHzDefault
        {
            get { return _viewModel.FrequencyKHzDefault; }
            set { _viewModel.FrequencyKHzDefault = value; }
        }

        public long FrequencyKHzSliderStep
        {
            get { return _viewModel.FrequencyKHzSliderStep; }
            set { _viewModel.FrequencyKHzSliderStep = value; }
        }

        public string PageTitle
        {
            get { return _viewModel.Title; }
            set { _viewModel.Title = value; }
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("FrequencyKHz", new List<View>() { FrequencyKHzBoxView, EntryFrequencyKHz }))
                .AddItem(KeyboardFocusableItem.CreateFrom("FrequencyMHz", new List<View>() { FrequencyMHzBoxView, EntryFrequencyMHz }))
                .AddItem(KeyboardFocusableItem.CreateFrom("LeftFrequency", new List<View>() { LeftButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SliderFrequency", new List<View>() { SliderFrequencyBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("RightFrequency", new List<View>() { RightButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DefaultButton", new List<View>() { DefaultButton }));

            SliderFrequency.DragCompleted += SliderFrequency_DragCompleted;

            EntryFrequencyKHz.Unfocused += EntryFrequencyKHz_Unfocused;
            EntryFrequencyMHz.Unfocused += EntryFrequencyMHz_Unfocused;

            EntryFrequencyKHz.Focused += delegate { EntryFrequencyKHz.CursorPosition = EntryFrequencyKHz.Text.Length; };
            EntryFrequencyMHz.Focused += delegate { EntryFrequencyMHz.CursorPosition = EntryFrequencyMHz.Text.Length; };

            _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;
        }

        private void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            // scroll to item
            FrequencyPageScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height / 2, false);
        }

        private void EntryFrequencyMHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidFrequency(EntryFrequencyMHz.Text))
            {
                _dialogService.Error($"Frequency \"{EntryFrequencyMHz.Text}\" MHz is out of range {_viewModel.MinFrequencyKHz} KHz - {_viewModel.MaxFrequencyKHz} KHz");
                _viewModel.FrequencyKHz = _viewModel.FrequencyKHzDefault;
            }
        }

        private void EntryFrequencyKHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidFrequency(_viewModel.FrequencyKHz))
            {
                _dialogService.Error($"Frequency \"{_viewModel.FrequencyKHz}\" KHz is out of range {TuneViewModel.BandWidthMinKHz} KHz - {TuneViewModel.BandWidthMaxKHz} KHz");
                _viewModel.FrequencyKHz = _viewModel.FrequencyKHzDefault;
            }
        }

        private void SliderFrequency_DragCompleted(object sender, EventArgs e)
        {
            _viewModel.RoundFrequency();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.NotifyFontSizeChange();
            _focusItems.DeFocusAll();
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"FrequencyPage OnKeyDown {key}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                        _focusItems.FocusNextItem();
                    break;

                case KeyboardNavigationActionEnum.Up:
                    _focusItems.FocusPreviousItem();
                    break;

                    case KeyboardNavigationActionEnum.Right:
                        switch (_focusItems.FocusedItemName)
                        {
                            case "SliderFrequency":
                            if (_viewModel.FrequencyKHz + _viewModel.FrequencyKHzSliderStep <= _viewModel.MaxFrequencyKHz)
                            {
                                _viewModel.FrequencyKHz += _viewModel.FrequencyKHzSliderStep;
                            }
                            break;
                        }
                    break;
                        case KeyboardNavigationActionEnum.Left:
                        switch (_focusItems.FocusedItemName)
                        {
                            case "SliderFrequency":
                                if (_viewModel.FrequencyKHz - _viewModel.FrequencyKHzSliderStep >= _viewModel.MinFrequencyKHz)
                                {
                                    _viewModel.FrequencyKHz -= _viewModel.FrequencyKHzSliderStep;
                                }
                                break;
                        }
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:

                    switch (_focusItems.FocusedItemName)
                    {
                        case "FrequencyKHz":
                            EntryFrequencyKHz.Focus();
                            break;

                        case "FrequencyMHz":
                            EntryFrequencyMHz.Focus();
                            break;

                        case "LeftFrequency":
                            _viewModel.LeftFrequencyCommand.Execute(null);
                            break;

                        case "RightFrequency":
                            _viewModel.RightFrequencyCommand.Execute(null);
                            break;

                        case "DefaultButton":
                            _viewModel.SetDefaultFrequencyCommand.Execute(null);
                            break;
                    }
                    break;
            }
        }

        public void OnTextSent(string text)
        {
            switch (_focusItems.FocusedItemName)
            {
                case "FrequencyKHz":
                    EntryFrequencyKHz.Text = text;
                    break;
                case "FrequencyMHz":
                    EntryFrequencyMHz.Text = text;
                    break;
            }
        }
    }
}