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
using System.Security.Principal;
using System.Web;
using System.Web.SessionState;

using MixERP.Net.Framework.Services;

namespace MixERP.Net.Framework.Services
{
    /// <summary>
    /// Base class for the application's JSON script services (formerly ASP.NET ASMX <c>System.Web.Services.WebService</c>).
    /// ASMX is not available on .NET 10; the host maps "*.asmx/{method}" requests to [WebMethod] members of
    /// [ScriptService] classes and exposes the current request through the same members WebService offered.
    /// </summary>
    [CLSCompliant(false)]
    public abstract class WebService
    {
        public HttpContext Context
        {
            get { return HttpContext.Current; }
        }

        public HttpApplicationState Application
        {
            get { return this.Context?.Application; }
        }

        public HttpServerUtility Server
        {
            get { return this.Context?.Server; }
        }

        public HttpSessionState Session
        {
            get { return this.Context?.Session; }
        }

        public IPrincipal User
        {
            get { return this.Context?.User; }
        }
    }
}
