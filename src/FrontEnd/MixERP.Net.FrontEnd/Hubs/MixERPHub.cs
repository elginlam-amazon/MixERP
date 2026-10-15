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
using Microsoft.AspNetCore.SignalR;

namespace MixERP.Net.FrontEnd.Hubs
{
    /// <summary>
    ///     Base class for MixERP SignalR hubs.
    ///     ASP.NET Core hub instances are disposed as soon as a hub method returns, while MixERP raises
    ///     progress notifications from background database/process callbacks. Hub methods call
    ///     <see cref="CaptureClients" /> first so those callbacks keep a valid client proxy.
    /// </summary>
    [CLSCompliant(false)]
    public abstract class MixERPHub : Hub
    {
        protected IClientProxy callerClients;
        protected IClientProxy allClients;

        protected void CaptureClients()
        {
            this.callerClients = this.Clients.Caller;
            this.allClients = this.Clients.All;
        }
    }
}
