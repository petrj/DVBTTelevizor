namespace DVBTTelevizor.MAUI;

using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

public partial class Menu : ContentView
{
    public event EventHandler Tapped;

    public Menu()
	{
		InitializeComponent();
    }

    private void Menu_Tapped(object sender, TappedEventArgs e)
    {
        Tapped?.Invoke(this, e);
    }

    public MenuItem CreateMenuItem(string id, string title, string img, bool delimitterFollows = false)
    {
        var item = new MenuItem()
        {
            Id = id,
            Title = title,
            ImgSource = img,
            IsVisible = true
        };

        if (delimitterFollows)
        {
            item.Margin = new Thickness(10, 10, 10, 30);
        }

        return item;
    }
}