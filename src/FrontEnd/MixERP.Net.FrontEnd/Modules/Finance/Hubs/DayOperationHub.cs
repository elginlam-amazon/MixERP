using Microsoft.AspNetCore.SignalR;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Events;
using MixERP.Net.Common.Extensions;
using MixERP.Net.i18n.Resources;
using System;
using System.Threading.Tasks;

namespace MixERP.Net.Core.Modules.Finance.Hubs
{
    /// <remarks>
    ///     Migrated from ASP.NET SignalR 2. ASP.NET Core SignalR dropped the dynamic client proxy,
    ///     so client methods are invoked by name with <c>SendAsync</c>, and hub methods are
    ///     asynchronous. See knowledge base doc 32.
    /// </remarks>
    [CLSCompliant(false)]
    public class DayOperationHub : Hub
    {
        public async Task PerformEOD()
        {
            if (!await this.IsValidRequest())
            {
                await this.Clients.Caller.SendAsync("getNotification", Warnings.AccessIsDenied,
                    Warnings.AccessIsDenied);
                return;
            }

            Data.EODOperation operation = new Data.EODOperation();
            operation.NotificationReceived += this.EOD_NotificationReceived;

            string catalog = AppUsers.GetCurrentUserDB();

            operation.Perform(catalog, AppUsers.GetCurrent().View.LoginId.ToLong());
        }

        /// <remarks>
        ///     Npgsql raises the notice event synchronously, so this handler cannot become
        ///     asynchronous. Blocking is safe here: ASP.NET Core has no synchronization context, so
        ///     there is no deadlock risk the way there was under System.Web.
        /// </remarks>
        private void EOD_NotificationReceived(object sender, MixERPPGEventArgs e)
        {
            this.Clients.Caller.SendAsync("getNotification", e.AdditionalInformation, e.Condition)
                .GetAwaiter().GetResult();
        }


        private async Task<bool> IsValidRequest()
        {
            await Task.Delay(2000);

            if (this.Context == null)
            {
                await this.Clients.Caller.SendAsync("getNotification", Warnings.AccessIsDenied);
                return false;
            }

            long globalLoginId = Conversion.TryCastLong(this.Context.User?.Identity?.Name);

            if (globalLoginId <= 0)
            {
                await this.Clients.Caller.SendAsync("getNotification", Warnings.AccessIsDenied);
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
