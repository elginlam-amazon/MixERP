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

/*
    OWIN SIGNALR 2 STARTUP - EXCLUDED FROM THE net10.0 COMPILE.

    ASP.NET SignalR 2 was hosted through OWIN (Microsoft.Owin / Owin) and System.Web, neither of
    which exists on modern .NET. ASP.NET Core SignalR replaces both the host and the wire protocol:
    hubs are registered per route with MapHub<T>() in Program.cs instead of the single app.MapSignalR()
    endpoint. This file is kept for reference; see knowledge base doc 32.
*/

using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MixERP.Net.FrontEnd.Hubs.Startup))]

namespace MixERP.Net.FrontEnd.Hubs
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}