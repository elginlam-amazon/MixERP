using Microsoft.AspNetCore.SignalR;
using MixERP.Net.FrontEnd.Hubs;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Events;
using MixERP.Net.Common.Extensions;
using MixERP.Net.i18n.Resources;
using System;

namespace MixERP.Net.Core.Modules.Finance.Hubs
{
    [CLSCompliant(false)]
    public class DayOperationHub : MixERPHub
    {
        public void PerformEOD()
        {
            this.CaptureClients();

            if (!this.IsValidRequest())
            {
                this.callerClients.SendAsync("getNotification", Warnings.AccessIsDenied, Warnings.AccessIsDenied);
                return;
            }

            Data.EODOperation operation = new Data.EODOperation();
            operation.NotificationReceived += this.EOD_NotificationReceived;

            string catalog = AppUsers.GetCurrentUserDB();

            operation.Perform(catalog, AppUsers.GetCurrent().View.LoginId.ToLong());
        }

        private void EOD_NotificationReceived(object sender, MixERPPGEventArgs e)
        {
            this.callerClients.SendAsync("getNotification", e.AdditionalInformation, e.Condition);
        }


        private bool IsValidRequest()
        {
            System.Threading.Thread.Sleep(2000);

            if (this.Context == null)
            {
                this.callerClients.SendAsync("getNotification", Warnings.AccessIsDenied);
                return false;
            }

            long globalLoginId = Conversion.TryCastLong(this.Context.User.Identity.Name);

            if (globalLoginId <= 0)
            {
                this.callerClients.SendAsync("getNotification", Warnings.AccessIsDenied);
                return false;
            }

            if (!AppUsers.GetCurrent(globalLoginId).View.IsAdmin.ToBool())
            {
                return false;
            }

            if (!AppUsers.GetCurrent(globalLoginId).View.Elevated.ToBool())
            {
                return false;
            }

            return true;
        }
    }
}