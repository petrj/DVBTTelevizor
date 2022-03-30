using Android.Widget;
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
    public partial class ChannelPage : ContentPage
    {
        private ChannelPageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;

        public ChannelPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new ChannelPageViewModel(_loggingService, _dialogService, driver, config);

            Appearing += ChannelPage_Appearing;
        }

        private void ChannelPage_Appearing(object sender, EventArgs e)
        {
            _viewModel.NotifyFontSizeChange();
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
    }
}