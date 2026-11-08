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
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.Framework.Contracts.Checklist;

namespace MixERP.Net.FrontEnd.Endpoints
{
    /// <summary>
    ///     Replaces <c>Services/Checklists.asmx</c> (<c>GetFirstSteps</c>) — checklist task 75.
    ///     Consumed by <c>wwwroot/js/Pages/Modules/Checklists.aspx.js</c>, which binds
    ///     <c>msg.d</c> to the angular <c>CheckListController</c> scope and filters it on
    ///     <c>Status</c> / <c>Category</c>, so the DTO property names are a contract.
    /// </summary>
    public static class ChecklistsEndpoints
    {
        public const string GetFirstStepsRoute = "/services/checklists/getfirststeps";

        public static IEndpointRouteBuilder MapChecklistsServiceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(GetFirstStepsRoute, AsmxCompatibility.Verbs, (HttpContext context) =>
            {
                try
                {
                    return AsmxCompatibility.Envelope(GetFirstSteps());
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                //An AJAX POST from Checklists.aspx.js carries no antiforgery token (the legacy ASMX
                //had none either), and the payload is read-only.
                .DisableAntiforgery();

            return endpoints;
        }

        /// <summary>
        ///     Verbatim port of <c>Checklists.GetFirstSteps()</c>: every discovered
        ///     <c>FirstStep</c>, ordered by <c>Order</c>, projected onto the 9 property DTO.
        /// </summary>
        public static List<Checklist> GetFirstSteps()
        {
            List<Checklist> items = new List<Checklist>();

            foreach (FirstStep item in FirstStep.GetAll().OrderBy(o => o.Order))
            {
                items.Add(new Checklist
                {
                    Name = item.Name,
                    Icon = item.Icon,
                    Category = item.Category,
                    CategoryAlias = item.CategoryAlias,
                    Order = item.Order,
                    Description = item.Description,
                    Status = item.Status,
                    Message = item.Message,
                    NavigateUrl = item.NavigateUrl
                });
            }

            return items;
        }
    }

    /// <summary>
    ///     The DTO of the former <c>MixERP.Net.FrontEnd.Services.Checklist</c>. Shape unchanged.
    /// </summary>
    public class Checklist
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Category { get; set; }
        public string CategoryAlias { get; set; }
        public int Order { get; set; }
        public string Description { get; set; }
        public bool Status { get; set; }
        public string Message { get; set; }
        public string NavigateUrl { get; set; }
    }
}
