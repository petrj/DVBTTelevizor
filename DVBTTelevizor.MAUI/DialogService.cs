using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class DialogService : IDialogService
    {
        public DialogService(Page page)
        {
            DialogPage = page;
        }

        public Page DialogPage { get; set; }

        public async Task<bool> Confirm(string message, string title = "Confirm", string positiveText = "Yes", string negativeText = "No")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            var result = await dp.DisplayAlert(title, message, positiveText, negativeText);

            return result;
        }

        public async Task ConfirmSingleButton(string message, string title = "Confirm", string btnOK = "OK")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            await dp.DisplayAlert(title, message, btnOK);
        }

        public async Task Information(string message, string title = "Informatopn")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            await dp.DisplayAlert(title, message, "OK");
        }

        public async Task<string> Select(List<string> options, string title = "Select", string cancel = "Back")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            return await dp.DisplayActionSheet(title, cancel, null, options.ToArray());
        }
    }
}
