using DVBTTelevizor.MAUI.Messages;

namespace DVBTTelevizor.MAUI
{
    public partial class App : Application
    {
        private MainPage _mp;

        public App(MainPage mp)
        {
            InitializeComponent();

            _mp = mp;
            MainPage = new NavigationPage(_mp);
        }

        protected override void OnStart()
        {
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
        }
    }
}
