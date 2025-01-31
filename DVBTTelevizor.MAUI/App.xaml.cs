namespace DVBTTelevizor.MAUI
{
    public partial class App : Application
    {
        public App(MainPage mp)
        {
            InitializeComponent();

            MainPage = new NavigationPage(mp);
        }
    }
}
