namespace DVBTTelevizor.MAUI;
using Microsoft.Maui.Controls;

public partial class ImgButton : ContentView
{
    private string _title = String.Empty;
    private string _imgName = null;

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

    public readonly BindableProperty ImgNameProperty =
        BindableProperty.Create(
            nameof(ImgName),
            typeof(string),
            typeof(ImgButton),
            default(string),
            propertyChanged: OnAnyValueChanged);

    public static readonly BindableProperty ImgProperty =
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
            var val = (ImageSource)GetValue(ImgProperty);
            return val;
        }
        set => SetValue(ImgProperty, value);
    }

    public string ImgName
    {
        get
        {
            return _imgName;
        }
        set
        {
            _imgName = value;
            Img = ImageSource.FromFile(value);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(ImgName));
                OnPropertyChanged(nameof(Img));
            });

        }
    }
}