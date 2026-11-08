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
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Office;
using MixERP.Net.i18n.Resources;
using Serilog;

namespace MixERP.Net.Core.Modules.Sales.Endpoints
{
    /// <summary>
    ///     The three former <c>Modules/Sales/Services/Receipt/*.asmx</c> <c>[ScriptService]</c> web
    ///     services, as minimal-API JSON endpoints. All of them are called by
    ///     <c>wwwroot/js/entry/Receipt.js</c>. Each service keeps its own
    ///     <c>Map...(this IEndpointRouteBuilder)</c> extension method - nothing self-registers; the
    ///     host calls <see cref="SalesServiceEndpoints.MapSalesServiceEndpoints" /> once.
    ///     <para>URLs (both GET and POST):</para>
    ///     <list type="bullet">
    ///         <item><c>Accounts.asmx/GetBankAccounts</c> -&gt; <c>/services/sales/receipt/accounts/getbankaccounts</c></item>
    ///         <item><c>Accounts.asmx/GetPaymentCards</c> -&gt; <c>/services/sales/receipt/accounts/getpaymentcards</c></item>
    ///         <item><c>Accounts.asmx/GetMerchantFeeSetup</c> -&gt; <c>/services/sales/receipt/accounts/getmerchantfeesetup</c></item>
    ///         <item><c>Accounts.asmx/GetCashRepositories</c> -&gt; <c>/services/sales/receipt/accounts/getcashrepositories</c></item>
    ///         <item><c>Accounts.asmx/GetCostCenters</c> -&gt; <c>/services/sales/receipt/accounts/getcostcenters</c></item>
    ///         <item><c>Accounts.asmx/GetFlags</c> -&gt; <c>/services/sales/receipt/accounts/getflags</c></item>
    ///         <item><c>Currencies.asmx/GetCurrencies</c> -&gt; <c>/services/sales/receipt/currencies/getcurrencies</c></item>
    ///         <item><c>Currencies.asmx/GetExchangeRate</c> -&gt; <c>/services/sales/receipt/currencies/getexchangerate</c></item>
    ///         <item><c>Currencies.asmx/GetHomeCurrency</c> -&gt; <c>/services/sales/receipt/currencies/gethomecurrency</c></item>
    ///         <item><c>TransactionPosting.asmx/Save</c> -&gt; <c>/services/sales/receipt/transactionposting/save</c></item>
    ///     </list>
    ///     <para>
    ///         RESPONSE SHAPES ARE UNCHANGED. The five select-binding methods returned
    ///         <c>Collection&lt;System.Web.UI.WebControls.ListItem&gt;</c>; they now return
    ///         <c>Collection&lt;ListItem&gt;</c> (see <see cref="ListItem" />), whose <c>Text</c> /
    ///         <c>Value</c> properties are exactly what <c>mixerp-core.js</c>'s
    ///         <c>bindAjaxData</c> reads. <c>GetMerchantFeeSetup</c> still returns the
    ///         <c>MerchantFeeSetup</c> entity, whose <c>Rate</c> and <c>CustomerPaysFee</c>
    ///         properties Receipt.js reads off <c>msg.d</c>.
    ///     </para>
    ///     <para>
    ///         <c>GetFlags</c> has no caller in the shipped javascript, but it was public surface of
    ///         the service and is ported rather than dropped.
    ///     </para>
    ///     <para>
    ///         <c>AppUsers.GetCurrent()</c> / <c>GetCurrentUserDB()</c> are CORRECT here and
    ///         deliberately kept - these are real HTTP endpoints with a live <c>HttpContext</c>.
    ///     </para>
    /// </summary>
    public static class SalesReceiptEndpoints
    {
        public const string GetBankAccountsRoute = "/services/sales/receipt/accounts/getbankaccounts";
        public const string GetPaymentCardsRoute = "/services/sales/receipt/accounts/getpaymentcards";
        public const string GetMerchantFeeSetupRoute = "/services/sales/receipt/accounts/getmerchantfeesetup";
        public const string GetCashRepositoriesRoute = "/services/sales/receipt/accounts/getcashrepositories";
        public const string GetCostCentersRoute = "/services/sales/receipt/accounts/getcostcenters";
        public const string GetFlagsRoute = "/services/sales/receipt/accounts/getflags";

        public const string GetCurrenciesRoute = "/services/sales/receipt/currencies/getcurrencies";
        public const string GetExchangeRateRoute = "/services/sales/receipt/currencies/getexchangerate";
        public const string GetHomeCurrencyRoute = "/services/sales/receipt/currencies/gethomecurrency";

        public const string TransactionPostingSaveRoute = "/services/sales/receipt/transactionposting/save";

        /// <summary>
        ///     Replaces <c>Services/Receipt/Accounts.asmx</c> (six methods).
        /// </summary>
        public static IEndpointRouteBuilder MapSalesReceiptAccountsService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(GetBankAccountsRoute, AsmxCompatibility.Verbs, () => Invoke(GetBankAccounts))
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetPaymentCardsRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetPaymentCards(p.GetInt64("merchantAccountId")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetMerchantFeeSetupRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetMerchantFeeSetup(p.GetInt64("merchantAccountId"),
                        p.GetInt32("paymentCardId")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetCashRepositoriesRoute, AsmxCompatibility.Verbs, () => Invoke(GetCashRepositories))
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetCostCentersRoute, AsmxCompatibility.Verbs, () => Invoke(GetCostCenters))
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetFlagsRoute, AsmxCompatibility.Verbs, () => Invoke(GetFlags))
                .DisableAntiforgery()
                .RequireAuthorization();

            return endpoints;
        }

        /// <summary>
        ///     Replaces <c>Services/Receipt/Currencies.asmx</c> (three methods).
        /// </summary>
        public static IEndpointRouteBuilder MapSalesReceiptCurrenciesService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(GetCurrenciesRoute, AsmxCompatibility.Verbs, () => Invoke(GetCurrencies))
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetExchangeRateRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetExchangeRate(p.GetString("sourceCurrencyCode"),
                        p.GetString("destinationCurrencyCode")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetHomeCurrencyRoute, AsmxCompatibility.Verbs, () => Invoke(GetHomeCurrency))
                .DisableAntiforgery()
                .RequireAuthorization();

            return endpoints;
        }

        /// <summary>
        ///     Replaces <c>Services/Receipt/TransactionPosting.asmx</c> (<c>Save</c>).
        /// </summary>
        public static IEndpointRouteBuilder MapSalesReceiptTransactionPostingService(
            this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(TransactionPostingSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(Save(
                        p.GetString("partyCode"),
                        p.GetString("currencyCode"),
                        p.GetDecimal("amount"),
                        p.GetDecimal("debitExchangeRate"),
                        p.GetDecimal("creditExchangeRate"),
                        p.GetString("referenceNumber"),
                        p.GetString("statementReference"),
                        p.GetInt32("costCenterId"),
                        p.GetInt32("cashRepositoryId"),
                        p.GetNullableDateTime("postedDate"),
                        p.GetInt64("bankAccountId"),
                        p.GetInt32("paymentCardId"),
                        p.GetString("bankInstrumentCode"),
                        p.GetString("bankTransactionCode")));
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

        /// <summary>
        ///     The envelope/error wrapper for the parameterless methods.
        /// </summary>
        private static IResult Invoke(Func<object> method)
        {
            try
            {
                return AsmxCompatibility.Envelope(method());
            }
            catch (Exception ex)
            {
                return AsmxCompatibility.Error(ex);
            }
        }

        //=================================================================================
        // Accounts.asmx - the former [WebMethod] bodies, verbatim.
        //=================================================================================

        private static object GetBankAccounts()
        {
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();

            Collection<ListItem> values = new Collection<ListItem>();

            foreach (BankAccount bankAccount in Data.Helpers.Accounts.GetBankAccounts(AppUsers.GetCurrentUserDB(),
                officeId))
            {
                values.Add(new ListItem(bankAccount.BankName + " (" + bankAccount.BankAccountNumber + ")",
                    bankAccount.AccountId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static Collection<ListItem> GetPaymentCards(long merchantAccountId)
        {
            Collection<ListItem> values = new Collection<ListItem>();

            bool isMerchantAccount = Data.Helpers.Accounts.IsMerchantAccount(AppUsers.GetCurrentUserDB(),
                merchantAccountId);

            if (!isMerchantAccount)
            {
                return values;
            }

            foreach (PaymentCard card in Data.Helpers.Accounts.GetPaymentCards(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(card.PaymentCardName,
                    card.PaymentCardId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static MerchantFeeSetup GetMerchantFeeSetup(long merchantAccountId, int paymentCardId)
        {
            return Data.Helpers.Accounts.GetMerchantFeeSetup(AppUsers.GetCurrentUserDB(), merchantAccountId,
                paymentCardId);
        }

        private static object GetCashRepositories()
        {
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (CashRepository cashRepository in Data.Helpers.Accounts.GetCashRepositories(
                AppUsers.GetCurrentUserDB(), officeId))
            {
                values.Add(new ListItem(cashRepository.CashRepositoryName,
                    cashRepository.CashRepositoryId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static object GetCostCenters()
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (CostCenter costCenter in Data.Helpers.Accounts.GetCostCenters(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(costCenter.CostCenterName,
                    costCenter.CostCenterId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static object GetFlags()
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (FlagType flagType in Data.Helpers.Accounts.GetFlagTypes(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(flagType.FlagTypeName,
                    flagType.FlagTypeId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        //=================================================================================
        // Currencies.asmx - the former [WebMethod] bodies, verbatim.
        //=================================================================================

        private static object GetCurrencies()
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (Currency currency in Data.Helpers.Currencies.GetCurrencies(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(currency.CurrencyCode, currency.CurrencyCode));
            }

            return values;
        }

        private static decimal GetExchangeRate(string sourceCurrencyCode, string destinationCurrencyCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCurrencyCode))
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(destinationCurrencyCode))
            {
                return 0;
            }

            if (sourceCurrencyCode.Equals(destinationCurrencyCode))
            {
                return 1;
            }

            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();

            decimal exchangeRate = Data.Helpers.Transaction.GetExchangeRate(AppUsers.GetCurrentUserDB(), officeId,
                sourceCurrencyCode, destinationCurrencyCode);

            return exchangeRate;
        }

        private static object GetHomeCurrency()
        {
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            return Data.Helpers.Currencies.GetHomeCurrency(AppUsers.GetCurrentUserDB(), officeId);
        }

        //=================================================================================
        // TransactionPosting.asmx - the former [WebMethod] body, verbatim.
        //=================================================================================

        private static long Save(string partyCode, string currencyCode, decimal amount, decimal debitExchangeRate,
            decimal creditExchangeRate, string referenceNumber, string statementReference, int costCenterId,
            int cashRepositoryId, DateTime? postedDate, long bankAccountId, int paymentCardId,
            string bankInstrumentCode, string bankTransactionCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(partyCode))
                {
                    throw new ArgumentNullException("partyCode");
                }

                if (string.IsNullOrWhiteSpace(currencyCode))
                {
                    throw new ArgumentNullException("currencyCode");
                }

                if (amount <= 0)
                {
                    throw new ArgumentNullException("amount");
                }

                if (debitExchangeRate <= 0)
                {
                    throw new ArgumentNullException("debitExchangeRate");
                }

                if (creditExchangeRate <= 0)
                {
                    throw new ArgumentNullException("creditExchangeRate");
                }

                if (cashRepositoryId == 0 && bankAccountId == 0)
                {
                    throw new InvalidOperationException(Warnings.InvalidReceiptMode);
                }

                //PRESERVED: the second half of this condition tests bankInstrumentCode TWICE - the
                //bankTransactionCode check the author plainly intended is missing upstream. Left
                //exactly as it was, because "fixing" it would start rejecting cash receipts that
                //the application currently accepts.
                if (cashRepositoryId > 0 &&
                    (bankAccountId > 0 || !string.IsNullOrWhiteSpace(bankInstrumentCode) ||
                     !string.IsNullOrWhiteSpace(bankInstrumentCode)))
                {
                    throw new InvalidOperationException(Warnings.CashTransactionCannotContainBankInfo);
                }

                return PostTransaction(partyCode, currencyCode, amount, debitExchangeRate, creditExchangeRate,
                    referenceNumber, statementReference, costCenterId, cashRepositoryId, postedDate, bankAccountId,
                    paymentCardId, bankInstrumentCode, bankTransactionCode);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save sales receipt. {Exception}", ex);
                throw;
            }
        }

        private static long PostTransaction(string partyCode, string currencyCode, decimal amount,
            decimal debitExchangeRate, decimal creditExchangeRate, string referenceNumber, string statementReference,
            int costCenterId, int cashRepositoryId, DateTime? postedDate, long bankAccountId, int paymentCardId,
            string bankInstrumentCode, string bankTransactionCode)
        {
            int userId = AppUsers.GetCurrent().View.UserId.ToInt();
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

            long transactionMasterID = Data.Transactions.Receipt.PostTransaction(AppUsers.GetCurrentUserDB(), userId,
                officeId, loginId, partyCode, currencyCode, amount, debitExchangeRate, creditExchangeRate,
                referenceNumber, statementReference, costCenterId, cashRepositoryId, postedDate, bankAccountId,
                paymentCardId, bankInstrumentCode, bankTransactionCode);

            return transactionMasterID;
        }
    }
}
