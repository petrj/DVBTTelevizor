namespace DVBTTelevizor.MAUI;
using Microsoft.Maui.Controls;

public partial class ImgButton : ContentView
{
    private string _title = String.Empty;
    private string _img = null;

    public readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(ImgButton),
            default(string),
            propertyChanged: OnAnyValueChanged);

    private static void OnAnyValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
    }

    public readonly BindableProperty ImgProperty =
        BindableProperty.Create(
            nameof(Img),
            typeof(string),
            typeof(ImgButton),
            default(string),
            propertyChanged: OnAnyValueChanged);

    public ImgButton()
	{
		InitializeComponent();
	}

    public string Title
    {
        get
        {
            return _title;
        }
        set
        {
            _title = value;
            OnPropertyChanged(nameof(Title));
        }
    }

    public string Img
    {
        get
        {
            return _img;
        }
        set
        {
            _img = value;
            OnPropertyChanged(nameof(Img));
        }
    }
}