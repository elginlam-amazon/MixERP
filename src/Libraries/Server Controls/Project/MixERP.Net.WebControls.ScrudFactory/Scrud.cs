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

//RELOCATED FROM THE HOST: MixERP.Net.FrontEnd/Controls/Scrud.cs, namespace
//MixERP.Net.FrontEnd.Controls.
//
//WHY IT MOVED. `Scrud` has NO consumer in the host — all 73 `new Scrud()` call sites are in the
//eight Modules/* projects, which is exactly why those projects carried a ProjectReference to the
//HOST (the inverted direction that makes their components undiscoverable as Razor Class
//Libraries). Breaking that reference therefore required relocating this type as well as the two
//base classes. It landed HERE, next to the `ScrudForm` it derives from, because:
//  * every module already references MixERP.Net.WebControls.ScrudFactory, so no new reference
//    was introduced anywhere;
//  * it could NOT go into MixERP.Net.Framework with the base classes — ScrudFactory references
//    MixERP.Net.Framework, so a Framework-resident subclass of ScrudForm would close a cycle.
//The CLASS NAME is unchanged; the module `using MixERP.Net.FrontEnd.Controls;` lines were
//rewritten to `using MixERP.Net.WebControls.ScrudFactory;`.
//
//WHAT THIS TYPE IS FOR. The original WebForms subclass
//
//    public sealed class Scrud : ScrudForm
//    {
//        public Scrud()
//        {
//            this.UserId     = AppUsers.GetCurrent().View.UserId.ToInt();
//            this.UserName   = AppUsers.GetCurrent().View.UserName;
//            this.OfficeCode = AppUsers.GetCurrent().View.OfficeName;
//            this.OfficeId   = AppUsers.GetCurrent().View.OfficeId.ToInt();
//            this.Catalog    = AppUsers.GetCurrentUserDB();
//        }
//    }
//
//is now a BLAZOR COMPONENT, written as component markup by its consumers
//
//    <Scrud KeyColumn="sales_team_id" TableSchema="core" Table="sales_teams"
//           ViewSchema="core" View="sales_team_scrud_view" Text="@Titles.SalesTeams" />
//
//and its ONLY job is still the one the constructor had: supplying the five login-derived
//parameters. Every other ScrudForm parameter is inherited, so no parameter had to be re-declared
//or forwarded (which is why this is a subclass and not a wrapper component) — keeping it is what
//saves the 73 call sites from hand-forwarding five values each.
//
//TWO THINGS CHANGED FROM THE WEBFORMS VERSION, BOTH MANDATORY:
//
// 1. THE ASSIGNMENTS MOVED OUT OF THE CONSTRUCTOR into OnInitializedAsync. A component's
//    constructor runs before DI/parameters, and -- far more important --
//    AppUsers.GetCurrent() / AppUsers.GetCurrentUserDB() resolve the user through
//    IHttpContextAccessor, which is NULL over a Blazor circuit: called from interactive component
//    code they silently return an EMPTY login (UserId 0, no catalog), and the form would query
//    the wrong/no database with no error at all.
//
// 2. THE SOURCE IS NOW ICurrentLogin (MixERP.Net.Framework.Security), the circuit-safe accessor,
//    read once per component initialization. The five values map 1:1 onto the originals,
//    INCLUDING the OfficeCode <- OfficeName assignment (NOT OfficeCode) -- that is what the
//    original did and what #OfficeCodeHidden has always carried.
//
//A consumer that needs to override any of the five may still pass it as a parameter: the
//assignments only fill a value the consumer did not supply.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MixERP.Net.Framework.Security;

namespace MixERP.Net.WebControls.ScrudFactory
{
    public sealed class Scrud : ScrudForm
    {
        [Inject]
        private ICurrentLogin CurrentLogin { get; set; }

        protected override async Task OnInitializedAsync()
        {
            LoginInfo login = this.CurrentLogin == null
                ? LoginInfo.Empty
                : await this.CurrentLogin.GetLoginAsync();

            if (login != null && login.IsSignedIn)
            {
                if (this.UserId == 0)
                {
                    this.UserId = login.UserId;
                }

                if (string.IsNullOrWhiteSpace(this.UserName))
                {
                    this.UserName = login.UserName;
                }

                if (string.IsNullOrWhiteSpace(this.OfficeCode))
                {
                    //The original assigned View.OfficeName here - preserved deliberately.
                    this.OfficeCode = login.OfficeName;
                }

                if (this.OfficeId == 0)
                {
                    this.OfficeId = login.OfficeId;
                }
            }

            if (string.IsNullOrWhiteSpace(this.Catalog))
            {
                this.Catalog = login == null ? null : login.Catalog;
            }

            await base.OnInitializedAsync();
        }
    }
}
