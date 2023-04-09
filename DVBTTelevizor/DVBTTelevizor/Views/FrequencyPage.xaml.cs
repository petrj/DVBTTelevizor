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
    public partial class FrequencyPage : ContentPage, IOnKeyDown
    {
        private TuneViewModel _viewModel;
        protected ILoggingService _loggingService;
        //protected IDialogService _dialogService;
        //protected IDVBTDriverManager _driver;
        //protected DVBTTelevizorConfiguration _config;

        private KeyboardFocusableItemList _focusItems;

        public FrequencyPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
        {
            InitializeComponent();

            _loggingService = loggingService;
            //_dialogService = dialogService;
            //_driver = driver;
            //_config = config;

            BindingContext = _viewModel = new TuneViewModel(loggingService, dialogService, driver, config);

            BuildFocusableItems();
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("MinFrequency", new List<View>() { MinFrequencyBoxView, EntryMinFrequency }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SliderMinFrequency", new List<View>() { SliderMinFrequencyBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MaxFrequency", new List<View>() { MaxFrequencyBoxView, EntryMaxFrequency }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SliderMaxFrequency", new List<View>() { SliderMaxFrequencyBoxView }));
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

                //    case KeyboardNavigationActionEnum.Right:
                //    case KeyboardNavigationActionEnum.Left:
                //        TODO: move slider
                //        break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:

                        switch (_focusItems.FocusedItemName)
                        {
                            case "MinFrequency":
                                EntryMinFrequency.Focus();
                            break;

                        case "MaxFrequency":
                            EntryMaxFrequency.Focus();
                            break;
                    }
                    break;
            }
        }
    }
}