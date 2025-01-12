using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public interface IDialogService
    {
        Page DialogPage { get; set; }

        Task<bool> Confirm(string message, string title = "Confirmation", string positiveText = "Yes", string negativeText = "No");
        Task Information(string message, string title = "Information");
        Task ConfirmSingleButton(string message, string title = "Confirm", string btnOK = "OK");
        Task<string> Select(List<string> options, string title = "Select", string cancel = "Back");
    }
}
