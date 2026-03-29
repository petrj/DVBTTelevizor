using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;

namespace DVBTTelevizor.MAUI
{
    public partial class App : Application
    {
        private MainPage _mp;
        private bool _resuming = false;

        public App(MainPage mp)
        {
            InitializeComponent();

            _mp = mp;
            MainPage = new NavigationPage(_mp);
        }

        protected override void OnStart()
        {
            // always check driver change when appearing main page
            WeakReferenceMessenger.Default.Send(new CheckDriversRequestMessage(null));
        }

        protected override void OnSleep()
        {
            if (!_mp.Configuration.PlayOnBackground)
            {
                Task.Run(async () =>
                {
                    await _mp.ActionStop(true);
                });
            }
        }

        protected override void OnResume()
        {
            if (_resuming)
                return;

            try
            {
                _resuming = true;

                // always check driver change when appearing main page
                WeakReferenceMessenger.Default.Send(new CheckDriversRequestMessage(null));

                if (_mp != null)
                {
                    Task.Run(async () =>
                    {
                        if (_mp.PlayingState != PlayingStateEnum.Stopped)
                        {
                            await _mp.FixVideo(true);
                        }
                        else
                        {
                            _mp.SetFixVideNeeded();
                        }

                        //await _mp.CheckDriverInstallationChange();
                        await _mp.FocusSelectedChannel();
                    });
                }
            } finally
            {
                _resuming = false;
            }
        }
    }
}
