namespace DVBTTelevizor.MAUI;
using Microsoft.Maui.Controls;

public partial class ImgButton : ContentView
{
    private string _title = String.Empty;
    private string _img = String.Empty;

    public static readonly BindableProperty TitleProperty =
          BindableProperty.Create(nameof(Title), typeof(string), typeof(ImgButton), default(string));

    public static readonly BindableProperty ImageSourceProperty =
        BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(ImgButton), default(ImageSource));


    public ImgButton()
	{
		InitializeComponent();
	}

    public string Title
    {
        get
        {
            return "_title";
        }
        set
        {
            _title = value;
            OnPropertyChanged(nameof(Title));
        }
    }

    public string ImageSource
    {
        get
        {
            return _img;
        }
        set
        {
            _img = value;
            OnPropertyChanged(nameof(ImageSource));
        }
    }
}