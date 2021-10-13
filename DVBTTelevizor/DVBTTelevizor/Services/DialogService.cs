using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class DialogService : IDialogService
    {
        private Page _page;

        public DialogService(Page page = null)
        {
            _page = page;
        }

        public Page DialogPage
        {
            get
            {
                return _page == null ? Application.Current.MainPage : _page;
            }
            set
            {
                _page = value;
            }
       }

        public async Task<bool> Confirm(string message, string title = "Confirmation", string positiveText="Yes", string negativeText = "No")
        {
            return await DialogPage.DisplayAlert(title, message, positiveText, negativeText);
        }

        public async Task Information(string message, string title = "Warning")
        {
            await DialogPage.DisplayAlert(title, message, "OK");
        }

        public async Task Error(string message, string title = "Error")
        {
            await DialogPage.DisplayAlert(title, message, "OK");
        }

        public async Task<string> DisplayActionSheet(string title, string cancel, List<string> buttonLabels)
        {
            return await DialogPage.DisplayActionSheet(title, cancel, null, buttonLabels.ToArray());
        }
    }
}
