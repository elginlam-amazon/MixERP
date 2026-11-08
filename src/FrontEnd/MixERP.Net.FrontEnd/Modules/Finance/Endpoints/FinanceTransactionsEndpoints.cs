/********************************************************************************
Copyright (C) Binod Nepal, Mix Open Foundation (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.Finance.Data.Helpers;
using MixERP.Net.Framework;
using MixERP.Net.WebControls.Common.Helpers;
using MixERP.Net.i18n.Resources;
using Serilog;

namespace MixERP.Net.Core.Modules.Finance.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted <c>Services/Transactions.asmx</c>
    ///     <c>[ScriptService]</c> web service - the voucher verification workflow
    ///     (<c>Approve</c> / <c>Reject</c>), the journal entry's exchange-rate lookup and the
    ///     account statement's book-date reconciliation.
    ///     <para>
    ///         Every former <c>[WebMethod]</c> body is carried over VERBATIM, including the two
    ///         <c>const int verificationStatusId</c> values (<c>2</c> for approve, <c>-3</c> for
    ///         reject) which are what <c>transactions.verify</c> records, and
    ///         <c>Reconcile</c>'s <c>bookDate &gt; FiscalYearEndDate</c> refusal - that guard is
    ///         the only thing stopping a transaction being back-dated past the fiscal year end.
    ///     </para>
    ///     <para>
    ///         The <c>Approve</c>/<c>Reject</c> callers
    ///         (<c>wwwroot/js/VoucherVerification.ascx.js</c>) only check that the request
    ///         SUCCEEDED, so the former <c>void</c> return stays a <c>{"d":null}</c> envelope and
    ///         a refusal keeps arriving as the preserved HTTP 500 +
    ///         <c>{Message, StackTrace, ExceptionType}</c> payload that
    ///         <c>logAjaxErrorMessage(xhr)</c> displays.
    ///     </para>
    /// </summary>
    public static class FinanceTransactionsEndpoints
    {
        private const string ApproveRoute = "/services/finance/transactions/approve";
        private const string RejectRoute = "/services/finance/transactions/reject";
        private const string GetExchangeRateRoute = "/services/finance/transactions/getexchangerate";
        private const string ReconcileRoute = "/services/finance/transactions/reconcile";

        public static IEndpointRouteBuilder MapFinanceTransactionsService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(ApproveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    Approve(p.GetInt64("tranId"), p.GetString("reason"));

                    return AsmxCompatibility.Envelope(null);
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(RejectRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    Reject(p.GetInt64("tranId"), p.GetString("reason"));

                    return AsmxCompatibility.Envelope(null);
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetExchangeRateRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetExchangeRate(p.GetString("currencyCode")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(ReconcileRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(Reconcile(p.GetString("tranCode"), p.GetInt32("year"),
                        p.GetInt32("month"), p.GetInt32("day")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            return endpoints;
        }

        //-------------------------------------------------------------------------------
        // The former [WebMethod] bodies, carried over verbatim.
        //-------------------------------------------------------------------------------

        private static void Approve(long tranId, string reason)
        {
            try
            {
                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();
                const int verificationStatusId = 2;

                Transaction.Verify(AppUsers.GetCurrentUserDB(), tranId, officeId, userId, loginId, verificationStatusId,
                    reason);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not approve transaction #{TranId} with reason {Reason}. {Exception}", tranId, reason,
                    ex);
                throw;
            }
        }

        private static decimal GetExchangeRate(string currencyCode)
        {
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            decimal exchangeRate = Transaction.GetExchangeRate(AppUsers.GetCurrentUserDB(), officeId, currencyCode);

            return exchangeRate;
        }

        private static void Reject(long tranId, string reason)
        {
            try
            {
                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();
                const int verificationStatusId = -3;

                Transaction.Verify(AppUsers.GetCurrentUserDB(), tranId, officeId, userId, loginId, verificationStatusId,
                    reason);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not reject transaction #{TranId} with reason {Reason}. {Exception}", tranId, reason,
                    ex);
                throw;
            }
        }

        private static bool Reconcile(string tranCode, int year, int month, int day)
        {
            DateTime bookDate = new DateTime(year, month, day);
            string catalog = AppUsers.GetCurrentUserDB();
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();

            FrequencyDates model = DatePersister.GetFrequencyDates(catalog, officeId);

            if (bookDate > model.FiscalYearEndDate)
            {
                throw new MixERPException(Warnings.InvalidDate);
            }

            return Transaction.Reconcile(AppUsers.GetCurrentUserDB(), tranCode, bookDate);
        }
    }
}
