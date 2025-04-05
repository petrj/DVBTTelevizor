namespace DVBTTelevizor.MAUI;

using Microsoft.Maui.Controls;
using System.Windows.Input;

public partial class ImgButton : ContentView
{
     public readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(ImgButton),
            default(string),
            propertyChanged: OnAnyValueChanged);

    public static readonly BindableProperty TapCommandProperty =
    BindableProperty.Create(
        nameof(TapCommand),
        typeof(ICommand),
        typeof(ImgButton),
        null);

    public static readonly BindableProperty ButtonColorProperty =
    BindableProperty.Create(
        nameof(ButtonColor),
        typeof(Color),
        typeof(ImgButton),
        default(Color),
        propertyChanged: OnAnyValueChanged);

    public static readonly BindableProperty ImgProperty =
    BindableProperty.Create(
        nameof(Img),
        typeof(ImageSource),
        typeof(ImgButton),
        default(ImageSource),
        propertyChanged: OnAnyValueChanged);

    private static void OnAnyValueChanged(BindableObject bindable, object oldValue, object newValue)
    {

    }

    public ImgButton()
	{
		InitializeComponent();

        ButtonColor = Colors.Gray;
	}

    public ICommand TapCommand
    {
        get => (ICommand)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ImageSource Img
    {
        get => (ImageSource)GetValue(ImgProperty);
        set => SetValue(ImgProperty, value);
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        TapCommand?.Execute(null);
    }

    public Color ButtonColor
    {
        get => (Color)GetValue(ButtonColorProperty);
        set => SetValue(ButtonColorProperty, value);
    }

}