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
using MixERP.Net.ApplicationState;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using Serilog;

namespace MixERP.Net.Core.Modules.Finance.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted <c>Services/EODOperation.asmx</c>
    ///     <c>[ScriptService]</c> web service, called by <c>wwwroot/js/EODOperation.js</c>
    ///     (<c>intializeEODOperation()</c> and <c>startNewDay()</c> - the first misspelling is
    ///     the original's).
    ///     <para>
    ///         Both former <c>[WebMethod]</c> bodies, and both private helpers, are carried over
    ///         VERBATIM. <c>InitializeEODOperation</c>'s <c>IsAdmin</c> gate is the real
    ///         authorization: it returns <c>false</c> (which the caller surfaces as
    ///         "Access is denied") rather than throwing, and <c>.RequireAuthorization()</c> only
    ///         requires A user, so the gate is reproduced exactly and NOT widened. The
    ///         two-minute <c>ForcedLogOffTimestamp</c> it then writes is what logs every other
    ///         user off before the end-of-day run.
    ///     </para>
    ///     <para>
    ///         <c>StartNewDay</c> was <c>void</c>; its caller only checks that the request
    ///         succeeded, so it keeps returning the <c>{"d":null}</c> envelope.
    ///     </para>
    /// </summary>
    public static class FinanceEODOperationEndpoints
    {
        private const string InitializeRoute = "/services/finance/eodoperation/initializeeodoperation";
        private const string StartNewDayRoute = "/services/finance/eodoperation/startnewday";

        public static IEndpointRouteBuilder MapFinanceEODOperationService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(InitializeRoute, AsmxCompatibility.Verbs, (HttpContext context) =>
            {
                try
                {
                    return AsmxCompatibility.Envelope(InitializeEODOperation());
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(StartNewDayRoute, AsmxCompatibility.Verbs, (HttpContext context) =>
            {
                try
                {
                    StartNewDay();

                    return AsmxCompatibility.Envelope(null);
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

        private static bool InitializeEODOperation()
        {
            try
            {
                if (!AppUsers.GetCurrent().View.IsAdmin.ToBool())
                {
                    return false;
                }

                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();

                Data.EODOperation.Initialize(AppUsers.GetCurrentUserDB(), userId, officeId);

                ForceLogOff(officeId);

                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Could not initialize eod operation. {Exception}", ex);
                throw;
            }
        }

        private static void StartNewDay()
        {
            try
            {
                SuggestDateReload();
            }
            catch (Exception ex)
            {
                Log.Warning("Could not start a new day. {Exception}", ex);
                throw;
            }
        }

        private static void ForceLogOff(int officeId)
        {
            Collection<FrequencyDates> applicationDates = Dates.GetFrequencyDates(AppUsers.GetCurrentUserDB());
            DateTime forcedLogOffOn = DateTime.Now.AddMinutes(2);

            if (applicationDates != null)
            {
                FrequencyDates model = applicationDates.FirstOrDefault(c => c.OfficeId.Equals(officeId));

                if (model != null)
                {
                    FrequencyDates item = model.Clone() as FrequencyDates;
                    if (item != null)
                    {
                        item.ForcedLogOffTimestamp = forcedLogOffOn;
                        item.NewDayStarted = false;

                        applicationDates.Add(item);
                        applicationDates.Remove(model);
                    }

                    Dates.SetApplicationDates(AppUsers.GetCurrentUserDB(), applicationDates);
                }
            }
        }

        private static void SuggestDateReload()
        {
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            Collection<FrequencyDates> applicationDates = Dates.GetFrequencyDates(AppUsers.GetCurrentUserDB());

            if (applicationDates != null)
            {
                FrequencyDates model = applicationDates.FirstOrDefault(c => c.OfficeId.Equals(officeId));
                if (model != null)
                {
                    FrequencyDates item = model.Clone() as FrequencyDates;
                    if (item != null)
                    {
                        item.NewDayStarted = true;

                        applicationDates.Add(item);
                        applicationDates.Remove(model);
                    }

                    Dates.SetApplicationDates(AppUsers.GetCurrentUserDB(), applicationDates);
                }
            }
        }
    }
}
