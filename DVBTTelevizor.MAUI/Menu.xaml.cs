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
}