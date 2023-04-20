using LoggerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static Android.App.Assist.AssistStructure;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BandWidthPage : ContentPage, IOnKeyDown
    {
        private BandWidthPageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;

        private KeyboardFocusableItemList _focusItems;

        public BandWidthPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new BandWidthPageViewModel(loggingService, dialogService, driver, config);

            BuildFocusableItems();
        }

        public long BandWidth
        {
            get { return _viewModel.BandWidthKHz; }
            set { _viewModel.BandWidthKHz = value; }
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("BandWidthKHz", new List<View>() { BandWidthKHzBoxView, EntryBandWidthKHz }))
                .AddItem(KeyboardFocusableItem.CreateFrom("BandWidthMHz", new List<View>() { BandWidthMHzBoxView, EntryBandWidthMHz }))
                .AddItem(KeyboardFocusableItem.CreateFrom("BandWidthCustom", new List<View>() { BandWidthCustomBoxView, BandWidthPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DefaultButton", new List<View>() { DefaultButton }));

            EntryBandWidthKHz.Unfocused += EntryBandWidthKHz_Unfocused;
            EntryBandWidthMHz.Unfocused += EntryBandWidthMHz_Unfocused;

            EntryBandWidthMHz.Focused += delegate { EntryBandWidthMHz.CursorPosition = EntryBandWidthMHz.Text.Length; };
            EntryBandWidthKHz.Focused += delegate { EntryBandWidthKHz.CursorPosition = EntryBandWidthKHz.Text.Length; };

            _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;
        }

        private void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            // scroll to item
            BandWithPageScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height / 2, false);
        }

        private void EntryBandWidthMHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidBandWidth(EntryBandWidthMHz.Text))
            {
                _dialogService.Error($"BandWidth \"{EntryBandWidthMHz.Text}\" MHz is out of range {TuneViewModel.BandWidthMinKHz} KHz - {TuneViewModel.BandWidthMaxKHz} KHz");
                _viewModel.BandWidthKHz = TuneViewModel.BandWidthDefaultKHz;
            }
        }

        private void EntryBandWidthKHz_Unfocused(object sender, FocusEventArgs e)
        {
            if (!_viewModel.ValidBandWidth(_viewModel.BandWidthKHz))
            {
                _dialogService.Error($"BandWidth \"{_viewModel.BandWidthKHz}\" KHz is out of range {TuneViewModel.BandWidthMinKHz} KHz - {TuneViewModel.BandWidthMaxKHz} KHz");
                _viewModel.BandWidthKHz = TuneViewModel.BandWidthDefaultKHz;
            }
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

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:

                    switch (_focusItems.FocusedItemName)
                    {
                        case "BandWidthCustom":
                            BandWidthPicker.Focus();
                            break;

                        case "BandWidthKHz":
                            EntryBandWidthKHz.Focus();
                            break;

                        case "BandWidthMHz":
                            EntryBandWidthMHz.Focus();
                            break;

                        case "DefaultButton":
                            _viewModel.SetDefaultBandWidthCommand.Execute(null);
                            break;
                    }
                    break;
            }
        }
    }
}