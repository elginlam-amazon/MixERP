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
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.Finance.Data.Helpers;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Models.Transactions;
using MixERP.Net.WebControls.StockTransactionFactory.Helpers;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MixERP.Net.Core.Modules.Finance.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted <c>Services/Entry/JournalVoucher.asmx</c>
    ///     <c>[ScriptService]</c> web service, called by
    ///     <c>wwwroot/js/entry/JournalVoucher.js</c>'s <c>postpostJournalTransaction</c>.
    ///     <para>
    ///         The <c>Save</c> body is carried over VERBATIM, every validation included - the
    ///         debit/credit exclusivity and non-negativity checks, the account-number
    ///         existence check, the cash-repository code + BALANCE check for a credit on a cash
    ///         account, and the <c>drTotal != crTotal</c> double-entry check. Those are the
    ///         posting rules, not input polish; the <c>InvalidOperationException</c> messages
    ///         surface through the preserved HTTP 500 +
    ///         <c>{Message, StackTrace, ExceptionType}</c> payload, which is what the entry
    ///         screen's <c>errorLabelBottom</c> displays.
    ///     </para>
    ///     <para>
    ///         <c>JavaScriptSerializer.Deserialize&lt;dynamic&gt;</c> became Newtonsoft's
    ///         <c>JArray</c>/<c>JToken</c>, and the POSITIONAL <c>item[0..9]</c> access of the
    ///         posted grid is preserved exactly - the grid is serialized as an array of ARRAYS
    ///         by the javascript, in the column order
    ///         StatementReference, AccountNumber, Account, CashRepositoryCode, CurrencyCode,
    ///         Debit, Credit, ExchangeRate, LCDebit, LCCredit.
    ///     </para>
    /// </summary>
    public static class FinanceJournalVoucherEntryEndpoints
    {
        private const string SaveRoute = "/services/finance/entry/journalvoucher/save";

        public static IEndpointRouteBuilder MapFinanceJournalVoucherEntryService(
            this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(SaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(Save(p.GetDateTime("valueDate"), p.GetDateTime("bookDate"),
                        p.GetString("referenceNumber"), p.GetString("data"), p.GetInt32("costCenterId"),
                        p.GetString("attachmentsJSON")));
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
        // The former [WebMethod] body, carried over verbatim.
        //-------------------------------------------------------------------------------

        private static long Save(DateTime valueDate, DateTime bookDate, string referenceNumber, string data,
            int costCenterId, string attachmentsJSON)
        {
            try
            {
                Collection<JournalDetail> details = GetJournalDetailCollection(data);

                Collection<Attachment> attachments = CollectionHelper.GetAttachmentCollection(attachmentsJSON);

                foreach (JournalDetail model in details)
                {
                    if (model.Debit > 0 && model.Credit > 0)
                    {
                        throw new InvalidOperationException("Invalid data");
                    }

                    if (model.Debit == 0 && model.Credit == 0)
                    {
                        throw new InvalidOperationException("Invalid data");
                    }

                    if (model.Credit < 0 || model.Debit < 0)
                    {
                        throw new InvalidOperationException("Invalid data");
                    }

                    if (!AccountHelper.AccountNumberExists(AppUsers.GetCurrentUserDB(), model.AccountNumber))
                    {
                        throw new InvalidOperationException("Invalid account " + model.AccountNumber);
                    }

                    if (model.Credit > 0)
                    {
                        if (AccountHelper.IsCashAccount(AppUsers.GetCurrentUserDB(), model.AccountNumber))
                        {
                            if (
                                !CashRepositories.CashRepositoryCodeExists(AppUsers.GetCurrentUserDB(),
                                    model.CashRepositoryCode))
                            {
                                throw new InvalidOperationException("Invalid cash repository " +
                                                                    model.CashRepositoryCode);
                            }

                            if (
                                CashRepositories.GetBalance(AppUsers.GetCurrentUserDB(), model.CashRepositoryCode,
                                    model.CurrencyCode) < model.Credit)
                            {
                                throw new InvalidOperationException("Insufficient balance in cash repository.");
                            }
                        }
                    }
                }

                decimal drTotal = (from detail in details select detail.LocalCurrencyDebit).Sum();
                decimal crTotal = (from detail in details select detail.LocalCurrencyCredit).Sum();

                if (drTotal != crTotal)
                {
                    throw new InvalidOperationException("Referencing sides are not equal.");
                }

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return Transaction.Add(AppUsers.GetCurrentUserDB(), valueDate, bookDate, officeId, userId, loginId,
                    costCenterId, referenceNumber, details, attachments);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save transaction. {Exception}", ex);
                throw;
            }
        }

        private static Collection<JournalDetail> GetJournalDetailCollection(string json)
        {
            Collection<JournalDetail> details = new Collection<JournalDetail>();

            if (string.IsNullOrWhiteSpace(json))
            {
                return details;
            }

            JArray result = JArray.Parse(json);

            foreach (JToken item in result)
            {
                JournalDetail detail = new JournalDetail();
                detail.StatementReference = Conversion.TryCastString(item[0]);
                detail.AccountNumber = Conversion.TryCastString(item[1]);
                detail.Account = Conversion.TryCastString(item[2]);
                detail.CashRepositoryCode = Conversion.TryCastString(item[3]);
                detail.CurrencyCode = Conversion.TryCastString(item[4]);
                detail.Debit = Conversion.TryCastDecimal(item[5]);
                detail.Credit = Conversion.TryCastDecimal(item[6]);
                detail.ExchangeRate = Conversion.TryCastDecimal(item[7]);
                detail.LocalCurrencyDebit = Conversion.TryCastDecimal(item[8]);
                detail.LocalCurrencyCredit = Conversion.TryCastDecimal(item[9]);
                details.Add(detail);
            }

            return details;
        }
    }
}
