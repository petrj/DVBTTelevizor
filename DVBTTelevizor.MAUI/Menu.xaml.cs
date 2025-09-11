namespace DVBTTelevizor.MAUI;

using Android.Media;
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
            MenuScrollView.ScrollToAsync(0, 0, false);
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
            item.Margin = new Thickness(5, 10, 5, 30);
        } else
        {
            item.Margin = new Thickness(5, 10, 5, 10);
        }

        return item;
    }

    public async Task SelectNextMenuItem(IEnumerable<MenuItem> menuItems, bool reverse)
    {
        var now = false;
        var selected = false;
        MenuItem first = null;
        var menuIndex = reverse ? MenuLayout.Children.Count - 1 : 0;

        foreach (var item in (reverse ? menuItems.AsEnumerable().Reverse() : menuItems))
        {
            if (first == null)
            {
                first = item;
            }

            if (now)
            {
                item.Selected = true;
                selected = true;
                item.Update();

                await MenuScrollView.ScrollToAsync(MenuLayout.Children[menuIndex] as Element, ScrollToPosition.MakeVisible, false);
                break;
            }
            else
            if (item.Selected)
            {
                item.Selected = false;
                item.Update();
                now = true;
            }

            if (reverse)
            {
                menuIndex--;
            }
            else
            {
                menuIndex++;
            }
        }

        if (!selected && first != null)
        {
            first.Selected = true;
            first.Update();

            var firstItemIndex = reverse ? MenuLayout.Children.Count - 1 : 0;
            await MenuScrollView.ScrollToAsync(MenuLayout.Children[firstItemIndex] as Element, ScrollToPosition.MakeVisible, false);
        }
    }




}