using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Plugin.InAppBilling;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class AboutPageViewModel : BaseViewModel
    {
        public AboutPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
        }

        public async Task<bool> Donate(string productId= "donation.2025.1")
        {
            _loggingService.Info($"Donate {productId}");

            IInAppBilling billing = null;
            try
            {
                if (!CrossInAppBilling.IsSupported)
                {
                    _loggingService.Error("Billing system is not supported on this device");
                    WeakReferenceMessenger.Default.Send(new ToastMessage("Billing system is not supported on this device".Translated()));
                    return false;
                }

                billing = CrossInAppBilling.Current;

                var connected = await billing.ConnectAsync();
                if (!connected)
                {
                    _loggingService.Error("Connection to billing failed");
                    WeakReferenceMessenger.Default.Send(new ToastMessage("Connection to billing failed".Translated()));

                    return false;
                }

                // Trigger purchase flow
                var purchase = await billing.PurchaseAsync(productId, ItemType.InAppPurchase);

                if (purchase == null)
                {
                    _loggingService.Error("User canceled billing or error");
                    WeakReferenceMessenger.Default.Send(new ToastMessage("Connection to billing failed".Translated()));

                    return false;
                }

                if (purchase.State == PurchaseState.Purchased)
                {
                    // ✅ Completed purchase → Consume it so user can donate again
                    await billing.ConsumePurchaseAsync(productId, purchase.PurchaseToken);

                    _loggingService.Info("Billing OK");
                    WeakReferenceMessenger.Default.Send(new ToastMessage("Thank you!".Translated()));

                    return true;
                }
                else if (purchase.State == PurchaseState.PaymentPending)
                {
                    _loggingService.Info("Pending");
                    WeakReferenceMessenger.Default.Send(new ToastMessage("Payment is being processed.".Translated()));

                    return false;
                }
                else
                {
                    return false;
                }
            }
            catch (InAppBillingPurchaseException pex)
            {
                if (pex.PurchaseError == PurchaseError.UserCancelled)
                {
                    _loggingService.Info("Payment cancelled");
                    return false;
                }

                throw;
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                WeakReferenceMessenger.Default.Send(new ToastMessage(ex.Message));
                return false;
            }
            finally
            {
                if (billing != null)
                {
                    await billing.DisconnectAsync();
                }
            }
        }
    }

}

