namespace DVBTTelevizor.MAUI;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (Parent is NavigationPage navigationPage)
        {
            navigationPage.BarBackgroundColor = Color.FromArgb("#29242a");
            navigationPage.BarTextColor = Colors.White;
        }
    }
}