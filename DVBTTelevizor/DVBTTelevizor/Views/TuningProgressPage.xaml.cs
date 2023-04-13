using LoggerService;
using Org.Xmlpull.V1.Sax2;
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
    public partial class TuningProgressPage : ContentPage, IOnKeyDown
    {
        private TuneViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected IDVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;

        private KeyboardFocusableItemList _focusItems;

        public TuningProgressPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _config = config;

            BindingContext = _viewModel = new TuneViewModel(_loggingService, _dialogService, _driver, _config);

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_CloseActualPage, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                   Navigation.PopAsync();
                });
            });

            BuildFocusableItems();
        }

        private void BuildFocusableItems()
        {
         /*   _focusItemsAuto = new KeyboardFocusableItemList();
            _focusItemsManual = new KeyboardFocusableItemList();
            _focusItemsAbort = new KeyboardFocusableItemList();
            _focusItemsDone = new KeyboardFocusableItemList();

            _focusItemsAuto
                .AddItem(KeyboardFocusableItem.CreateFrom("AutoManualTuning", new List<View>() { AutoManualTuningBoxView, AutoManualPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditBandWidth", new List<View>() { EditBandWidthButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditFrequencyFrom", new List<View>() { EditFrequencyFromButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditFrequencyTo", new List<View>() { EditFrequencyToButton }))

                //.AddItem(KeyboardFocusableItem.CreateFrom("BandWidtMHz", new List<View>() { AutoFrequencyBoxView }))
                //.AddItem(KeyboardFocusableItem.CreateFrom("Frequency", new List<View>() { FrequencyBoxView, EntryFrequency }))
                //.AddItem(KeyboardFocusableItem.CreateFrom("Channel", new List<View>() { ChannelBoxView, ChannelPicker }))

                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2TuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }));


            _focusItemsManual
                .AddItem(KeyboardFocusableItem.CreateFrom("ManualTuning", new List<View>() { AutoManualTuningBoxView, AutoManualPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Frequency", new List<View>() { FrequencyBoxView, EntryFrequency }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Channel", new List<View>() { ChannelBoxView, ChannelPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("BandWith", new List<View>() { BandWithBoxView, EntryBandWidth }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2TuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }));


            _focusItemsAbort.AddItem(KeyboardFocusableItem.CreateFrom("AbortButton", new List<View>() { AbortTuneButton }));
            _focusItemsDone.AddItem(KeyboardFocusableItem.CreateFrom("FinishButton", new List<View>() { FinishButton }));

            _focusItemsAuto.OnItemFocusedEvent += TunePage_OnItemFocusedEvent;
            _focusItemsManual.OnItemFocusedEvent += TunePage_OnItemFocusedEvent;
            _focusItemsAbort.OnItemFocusedEvent += TunePage_OnItemFocusedEvent;
            _focusItemsDone.OnItemFocusedEvent += TunePage_OnItemFocusedEvent;*/
        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
           ChannelsListView.ScrollTo(_viewModel.SelectedChannel, ScrollToPosition.MakeVisible, false);
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"TuningProgressPage OnKeyDown {key}");

            /*
            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                    if (ToolBarSelected)
                    {
                        ToolBarSelected = false;
                    }
                    else
                    {
                        _focusItems.FocusNextItem();
                    }
                    break;

                case KeyboardNavigationActionEnum.Up:
                    if (ToolBarSelected)
                    {
                        ToolBarSelected = false;
                    }
                    else
                    {
                        _focusItems.FocusPreviousItem();
                    }
                    break;

                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Left:
                    ToolBarSelected = !ToolBarSelected;
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:
                    if (ToolBarSelected)
                    {
                        ToolConnect_Clicked(this, null);
                    }
                    else
                    {
                        switch (_focusItems.FocusedItemName)
                        {
                            case "AutoManualTuning":
                                //_viewModel.ManualTuning = !_viewModel.ManualTuning;
                                AutoManualPicker.Focus();
                                //UpdateFocusedPart(_viewModel.ManualTuning ? "ManualTuning" : "AutoTuning", "ManualTuning");
                                break;

                            case "EditBandWidth":
                                EditBandWidthButtton_Clicked(this, null);
                                break;


                            case "EditFrequencyFrom":
                                EditFrequencyFromButtton_Clicked(this, null);
                                break;

                            case "EditFrequencyTo":
                                EditFrequencyToButtton_Clicked(this, null);
                                break;

                            case "TuneButton":
                                _viewModel.TuneCommand.Execute(null);
                                break;

                            case "AbortButton":
                                _viewModel.AbortTuneCommand.Execute(null);
                                break;

                            case "FinishButton":
                                _viewModel.FinishTunedCommand.Execute(null);
                                break;

                            case "Channel":
                                //ChannelPicker.Focus();
                                break;

                            case "Frequency":
                                //EntryFrequency.Focus();
                                break;

                            case "BandWith":
                                //EntryBandWidth.Focus();
                                break;

                            case "DVBT":
                                DVBTTuningCheckBox.IsToggled = !DVBTTuningCheckBox.IsToggled;
                                break;

                            case "DVBT2":
                                DVBT2TuningCheckBox.IsToggled = !DVBT2TuningCheckBox.IsToggled;
                                break;
                        }
                    }
                    break;
            }*/
        }
    }
}