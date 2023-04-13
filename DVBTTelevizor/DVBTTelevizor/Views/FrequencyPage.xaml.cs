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

        public void SetFrequency(string title, long frequencyKHz, long minimumKHz, long maximumKHz, long defaultValueKHz)
        {
            _viewModel.Title = title;
            _viewModel.MaxFrequencyKHz = maximumKHz;
            _viewModel.FrequencyKHz = frequencyKHz;
            _viewModel.MinFrequencyKHz = minimumKHz;
            _viewModel.DefaultFrequencyKHz = defaultValueKHz;
        }

        public long SelectedFrequency
        {
            get { return _viewModel.FrequencyKHz; }
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("FrequencyKHz", new List<View>() { FrequencyKHzBoxView, EntryFrequencyKHz }))
                .AddItem(KeyboardFocusableItem.CreateFrom("FrequencyMHz", new List<View>() { FrequencyMHzBoxView, EntryFrequencyMHz }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SliderFrequency", new List<View>() { SliderFrequencyBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DefaultButton", new List<View>() { DefaultButton }));

            SliderFrequency.DragCompleted += SliderFrequency_DragCompleted;

            EntryFrequencyKHz.Unfocused += EntryFrequencyKHz_Unfocused;
            EntryFrequencyMHz.Unfocused += EntryFrequencyMHz_Unfocused;
        }

        private void EntryFrequencyMHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidFrequency(EntryFrequencyMHz.Text))
            {
                _dialogService.Error($"Frequency \"{EntryFrequencyMHz.Text}\" MHz is out of range {_viewModel.MinFrequencyKHz} KHz - {_viewModel.MaxFrequencyKHz} KHz");
                _viewModel.FrequencyKHz = _viewModel.DefaultFrequencyKHz;
            }
        }

        private void EntryFrequencyKHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidFrequency(_viewModel.FrequencyKHz))
            {
                _dialogService.Error($"Frequency \"{_viewModel.FrequencyKHz}\" KHz is out of range {TuneViewModel.BandWidthMinKHz} KHz - {TuneViewModel.BandWidthMaxKHz} KHz");
                _viewModel.FrequencyKHz = _viewModel.DefaultFrequencyKHz;
            }
        }

        private void SliderFrequency_DragCompleted(object sender, EventArgs e)
        {
            _viewModel.RoundFrequency();
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

                        case "DefaultButton":
                            _viewModel.SetDefaultFrequencyCommand.Execute(null);
                            break;
                    }
                    break;
            }
        }
    }
}