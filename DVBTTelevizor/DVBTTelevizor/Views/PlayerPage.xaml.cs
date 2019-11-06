using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerPage : ContentPage
    {
        LibVLC _libVLC = null;
        MediaPlayer _mediaPlayer;
        DVBTDriverManager _driver;
        Media _media = null;

        public PlayerPage(DVBTDriverManager driver)
        {
            InitializeComponent();

            _driver = driver;
         
            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };
            videoView.MediaPlayer = _mediaPlayer;          
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_media == null)
            {
                _media = new Media(_libVLC, _driver.VideoStream, new string[] { });
            }
            
            videoView.MediaPlayer.Play(_media);
            
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            videoView.MediaPlayer.Stop();

           Task.Run(async () =>
           {
               await _driver.Stop();
           });            
        }
    }
}