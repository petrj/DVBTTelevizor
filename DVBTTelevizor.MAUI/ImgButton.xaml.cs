namespace DVBTTelevizor.MAUI;
using Microsoft.Maui.Controls;

public partial class ImgButton : ContentView
{
    private string _title = String.Empty;
    private ImageSource _img = "icon.png";

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
            typeof(ImageSource),
            typeof(ImgButton),
            default(ImageSource),
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

    public ImageSource Img
    {
        get
        {
            return _img; // NO image! It works only when "tune.png" used
        }
        set
        {
            _img = value;

            _img = "tune.png"; // this works

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(Img));
            });
        }
    }
}