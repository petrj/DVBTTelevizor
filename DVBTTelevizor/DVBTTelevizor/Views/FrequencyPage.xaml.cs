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

            EntryFrequencyKHz.Unfocused += EntryFrequencyKHz_Unfocused;
            EntryFrequencyMHz.Unfocused += EntryFrequencyMHz_Unfocused;
        }

        private void EntryFrequencyMHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidFrequency(EntryFrequencyMHz.Text))
            {
                _dialogService.Error($"BandWidth \"{EntryFrequencyMHz.Text}\" MHz is out of range {_viewModel.AutoTuningMinFrequencyKHz} KHz - {_viewModel.AutoTuningMaxFrequencyKHz} KHz");
                _viewModel.AutoTuningFrequencyKHz = _viewModel.DefaultFrequencyKHz;
            }
        }

        private void EntryFrequencyKHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidFrequency(_viewModel.AutoTuningFrequencyKHz))
            {
                _dialogService.Error($"BandWidth \"{_viewModel.AutoTuningFrequencyKHz}\" KHz is out of range {TuneViewModel.BandWidthMinKHz} KHz - {TuneViewModel.BandWidthMaxKHz} KHz");
                _viewModel.AutoTuningFrequencyKHz = _viewModel.DefaultFrequencyKHz;
            }
        }

        private void SliderFrequency_DragCompleted(object sender, EventArgs e)
        {
            _viewModel.RoundedFrequency();
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