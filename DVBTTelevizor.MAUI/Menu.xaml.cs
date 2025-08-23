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

        BindingContext = new MenuViewModel();
    }

    private void Menu_Tapped(object sender, TappedEventArgs e)
    {
        Tapped?.Invoke(this, e);
    }

    public void UpdateMenu(string title, IEnumerable<MenuItem> items = null)
    {
        if (BindingContext is MenuViewModel vm)
        {
            vm.UpdateMenu(items, title);
        }
    }

    public bool MenuVisible
    {
        get
        {
            if (BindingContext is MenuViewModel vm)
            {
                return vm.MenuVisible;
            }

            return false;
        }
        set
        {
            if (BindingContext is MenuViewModel vm)
            {
                vm.MenuVisible = value;
            }
            OnPropertyChanged(nameof(MenuVisible));
        }
    }

    public StackLayout MenuLayout
    {
        get
        {
            return MenuItemsStackLayout;
        }
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