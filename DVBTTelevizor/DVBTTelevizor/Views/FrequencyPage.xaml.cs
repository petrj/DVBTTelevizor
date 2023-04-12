using LoggerService;
using NLog.Layouts;
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
            _viewModel.AutoTuningMaxFrequencyKHz = maximumKHz;
            _viewModel.AutoTuningFrequencyKHz = frequencyKHz;
            _viewModel.AutoTuningMinFrequencyKHz = minimumKHz;
            _viewModel.DefaultFrequencyKHz = defaultValueKHz;
        }

        public long SelectedFrequency
        {
            get { return _viewModel.AutoTuningFrequencyKHz; }
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
        }

        private void SliderFrequency_DragCompleted(object sender, EventArgs e)
        {
            _viewModel.RoundedFrequency();
        }

        /*

        private long RoundedFrequencyToBandWidth(string frequencyMHz, int silent)
        {
            if (!_viewModel.ValidFrequency(frequencyMHz))
                return _viewModel.AutoTuningMinFrequencyKHz;

            return RoundedFrequencyToBandWidth(_viewModel.ParseFreqMHzToKHz(frequencyMHz), silent, true);
        }



        private void SliderFrequencyTo_DragCompleted(object sender, EventArgs e)
        {
            _viewModel.AutoTuningFrequencyToKHz = RoundedFrequencyToBandWidth(_viewModel.AutoTuningFrequencyToKHz, 1);
        }

        private void EntryBandWidthMHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidBandWidth(EntryBandWidthMHz.Text))
            {
                _dialogService.Error($"BandWidth \"{EntryBandWidthMHz.Text}\" MHz is out of range {TuneViewModel.BandWidthMinKHz} KHz - {TuneViewModel.BandWidthMaxKHz} KHz");
                _viewModel.TuneBandWidthKHz = TuneViewModel.BandWidthDefaultKHz;
            }

            _viewModel.AutoTuningFrequencyFromKHz = RoundedFrequencyToBandWidth(_viewModel.AutoTuningFrequencyFromKHz, 0);
            _viewModel.AutoTuningFrequencyToKHz = RoundedFrequencyToBandWidth(_viewModel.AutoTuningFrequencyToKHz, 0);
        }

        private void EntryBandWidthKHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidBandWidth(_viewModel.TuneBandWidthKHz))
            {
                _dialogService.Error($"BandWidth \"{_viewModel.TuneBandWidthKHz}\" KHz is out of range {TuneViewModel.BandWidthMinKHz} KHz - {TuneViewModel.BandWidthMaxKHz} KHz");
                _viewModel.TuneBandWidthKHz = TuneViewModel.BandWidthDefaultKHz;
            }

            _viewModel.AutoTuningFrequencyFromKHz = RoundedFrequencyToBandWidth(_viewModel.AutoTuningFrequencyFromKHz, 0);
            _viewModel.AutoTuningFrequencyToKHz = RoundedFrequencyToBandWidth(_viewModel.AutoTuningFrequencyToKHz, 0);
        }

        private void BandWidthPicker_Unfocused(object sender, FocusEventArgs e)
        {
            _viewModel.AutoTuningFrequencyFromKHz = RoundedFrequencyToBandWidth(_viewModel.AutoTuningFrequencyFromKHz, 0);
            _viewModel.AutoTuningFrequencyToKHz = RoundedFrequencyToBandWidth(_viewModel.AutoTuningFrequencyToKHz, 0);
        }

        private void EntryFrequencyFromMHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidFrequency(EntryFrequencyFromMHz.Text))
            {
                _dialogService.Error($"Frequency \"{EntryFrequencyFromMHz.Text}\" MHz is out of range {_viewModel.AutoTuningMinFrequencyKHz} KHz - {_viewModel.AutoTuningMaxFrequencyKHz} KHz");
                _viewModel.AutoTuningFrequencyFromKHz = _viewModel._autoTuningMinFrequencyKHz;
            } else
            {
                _viewModel.AutoTuningFrequencyFromKHz = RoundedFrequencyToBandWidth(EntryFrequencyFromMHz.Text, 0);
            }
        }

        private void EntryFrequencyToMHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidFrequency(EntryFrequencyToMHz.Text))
            {
                _dialogService.Error($"Frequency \"{EntryFrequencyToMHz.Text}\" MHz is out of range {_viewModel.AutoTuningMinFrequencyKHz} KHz - {_viewModel.AutoTuningMaxFrequencyKHz} KHz");
                _viewModel.AutoTuningFrequencyToKHz = _viewModel._autoTuningMaxFrequencyKHz;
            } else
            {
                _viewModel.AutoTuningFrequencyToKHz = RoundedFrequencyToBandWidth(EntryFrequencyToMHz.Text, 0);
            }
        }

        private void EntryFrequencyFromKHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidFrequency(_viewModel.AutoTuningFrequencyFromKHz))
            {
                _dialogService.Error($"Frequency \"{_viewModel.AutoTuningFrequencyFromKHz}\" KHz is out of range {_viewModel.AutoTuningMinFrequencyKHz} KHz - {_viewModel.AutoTuningMaxFrequencyKHz} KHz");
                _viewModel.AutoTuningFrequencyFromKHz = _viewModel.AutoTuningMinFrequencyKHz;
            } else
            {
                _viewModel.AutoTuningFrequencyFromKHz = RoundedFrequencyToBandWidth(_viewModel.AutoTuningFrequencyFromKHz, 0);
            }
        }

        private void EntryFrequencyToKHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidFrequency(_viewModel.AutoTuningFrequencyToKHz))
            {
                _dialogService.Error($"Frequency \"{_viewModel.AutoTuningFrequencyToKHz}\" KHz is out of range {_viewModel.AutoTuningMinFrequencyKHz} KHz - {_viewModel.AutoTuningMaxFrequencyKHz} KHz");
                _viewModel.AutoTuningFrequencyToKHz = _viewModel._autoTuningMaxFrequencyKHz;
            } else
            {
                _viewModel.AutoTuningFrequencyToKHz = RoundedFrequencyToBandWidth(_viewModel.AutoTuningFrequencyToKHz, 0);
            }
        }
        */

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
                            if (_viewModel.AutoTuningFrequencyKHz + _viewModel.FrequencyKHzSliderStep <= _viewModel.AutoTuningMaxFrequencyKHz)
                            {
                                _viewModel.AutoTuningFrequencyKHz += _viewModel.FrequencyKHzSliderStep;
                            }
                            break;
                        }
                    break;
                        case KeyboardNavigationActionEnum.Left:
                        switch (_focusItems.FocusedItemName)
                        {
                            case "SliderFrequency":
                                if (_viewModel.AutoTuningFrequencyKHz - _viewModel.FrequencyKHzSliderStep >= _viewModel.AutoTuningMinFrequencyKHz)
                                {
                                    _viewModel.AutoTuningFrequencyKHz -= _viewModel.FrequencyKHzSliderStep;
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