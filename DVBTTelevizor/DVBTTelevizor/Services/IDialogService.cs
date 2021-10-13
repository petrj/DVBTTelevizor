using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;


namespace DVBTTelevizor
{
    public interface IDialogService
    {
        Page DialogPage { get; set; }

        Task<bool> Confirm(string message, string title = "Confirmation", string positiveText = "Yes", string negativeText = "No");
        Task Information(string message, string title = "Information");
        Task Error(string message, string title = "Error");

        Task<string> DisplayActionSheet(string title, string cancel, List<string> buttonLabels);
    }
}
