using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    class DialogService : IDialogService
    {
        public DialogService(Page page = null)
        {
            DialogPage = page;
        }

        public Page DialogPage { get; set; }

        public async Task<bool> Confirm(string message, string title = "Confirmation")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            var result = await dp.DisplayAlert(title, message, "Ano", "Ne");

            return result;
        }

        public async Task Information(string message, string title = "Warning")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            await dp.DisplayAlert(title, message, "OK");
        }

        public async Task Error(string message, string title = "Error")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            await dp.DisplayAlert(title, message, "OK");
        }
    }
}
