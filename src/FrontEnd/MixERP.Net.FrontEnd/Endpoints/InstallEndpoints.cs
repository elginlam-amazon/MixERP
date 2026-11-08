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
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using Serilog;

namespace MixERP.Net.FrontEnd.Endpoints
{
    /// <summary>
    ///     Replaces <c>Services/Install.asmx</c> (<c>SaveOffice</c>, 16 parameters) — checklist
    ///     task 76. Consumed by <c>wwwroot/js/Pages/Install.aspx.js</c>, which treats a truthy
    ///     <c>msg.d</c> as success and alerts the error message otherwise.
    /// </summary>
    public static class InstallEndpoints
    {
        public const string SaveOfficeRoute = "/services/install/saveoffice";

        public static IEndpointRouteBuilder MapInstallServiceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(SaveOfficeRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters =
                        await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    bool saved = SaveOffice(
                        parameters.GetString("officeCode"),
                        parameters.GetString("officeName"),
                        parameters.GetString("nickName"),
                        parameters.GetString("registrationDate"),
                        parameters.GetString("currencyCode"),
                        parameters.GetString("currencySymbol"),
                        parameters.GetString("currencyName"),
                        parameters.GetString("hundredthName"),
                        parameters.GetString("fiscalYearCode"),
                        parameters.GetString("fiscalYearName"),
                        parameters.GetString("startsFrom"),
                        parameters.GetString("endsOn"),
                        parameters.GetString("adminName"),
                        parameters.GetString("username"),
                        parameters.GetString("password"),
                        parameters.GetString("confirmPassword"));

                    return AsmxCompatibility.Envelope(saved);
                }
                catch (Exception ex)
                {
                    //MixERPException (all fields required / password mismatch / start > end) and any
                    //database failure alike reach the caller as the same { Message, StackTrace,
                    //ExceptionType } payload the ScriptService produced, with the RAW message
                    //(Services/web.config customErrors mode="Off").
                    return AsmxCompatibility.Error(ex);
                }
            })
                //The installer posts without an antiforgery token, and it must work before any user
                //exists (hence no authorization either — this is the pre-installation surface).
                .DisableAntiforgery()
                .AllowAnonymous();

            return endpoints;
        }

        /// <summary>
        ///     Verbatim port of <c>Install.SaveOffice(...)</c>: the all-fields-required guard, the
        ///     confirmation-password match, the three <c>Convert.ToDateTime</c> parses, the
        ///     start &gt; end guard, then <c>Offices.SaveOffice(...)</c> with log-and-rethrow.
        /// </summary>
        public static bool SaveOffice(string officeCode, string officeName, string nickName, string registrationDate,
            string currencyCode, string currencySymbol, string currencyName, string hundredthName,
            string fiscalYearCode, string fiscalYearName, string startsFrom, string endsOn, string adminName,
            string username, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(officeName) || string.IsNullOrWhiteSpace(officeCode) ||
                string.IsNullOrWhiteSpace(nickName) || string.IsNullOrWhiteSpace(registrationDate) ||
                string.IsNullOrWhiteSpace(currencyCode) || string.IsNullOrWhiteSpace(currencySymbol) ||
                string.IsNullOrWhiteSpace(currencyName) || string.IsNullOrWhiteSpace(hundredthName) ||
                string.IsNullOrWhiteSpace(fiscalYearCode) || string.IsNullOrWhiteSpace(fiscalYearName) ||
                string.IsNullOrWhiteSpace(startsFrom) || string.IsNullOrWhiteSpace(endsOn) ||
                string.IsNullOrWhiteSpace(adminName) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                throw new MixERPException(Labels.AllFieldsRequired);
            }

            if (password != confirmPassword)
            {
                throw new MixERPException(Warnings.ConfirmationPasswordDoesNotMatch);
            }

            DateTime dateOfRegistration = Convert.ToDateTime(registrationDate);
            DateTime fiscalYearStartDate = Convert.ToDateTime(startsFrom);
            DateTime fiscalYearEndDate = Convert.ToDateTime(endsOn);

            if (fiscalYearStartDate > fiscalYearEndDate)
            {
                throw new MixERPException(Warnings.StartDateGreaterThanEndDate);
            }

            try
            {
                Data.Office.Offices.SaveOffice(AppUsers.GetCurrentUserDB(), officeCode, officeName, nickName,
                    dateOfRegistration, currencyCode, currencySymbol, currencyName, hundredthName, fiscalYearCode,
                    fiscalYearName, fiscalYearStartDate, fiscalYearEndDate, adminName, username, password);

                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save office. {Exception}", ex);
                throw;
            }
        }
    }
}
