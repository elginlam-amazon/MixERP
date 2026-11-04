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

namespace MixERP.Net.Framework.WebServices
{
    /// <summary>
    ///     Base class for the JSON script services (formerly ASMX web services).
    ///     ASMX (System.Web.Services) is not available on ASP.NET Core; the services are now dispatched
    ///     by the front end's script service endpoint, which keeps the "*.asmx/MethodName" URLs and the
    ///     ASP.NET AJAX "{ d: result }" JSON contract used by the existing JavaScript.
    /// </summary>
    public abstract class WebService
    {
        public HttpContext Context
        {
            get { return HttpContext.Current; }
        }

        public HttpSessionState Session
        {
            get { return this.Context == null ? null : this.Context.Session; }
        }

        public HttpServerUtility Server
        {
            get { return this.Context == null ? null : this.Context.Server; }
        }

        public HttpApplicationState Application
        {
            get { return this.Context == null ? null : this.Context.Application; }
        }

        public IPrincipal User
        {
            get { return this.Context == null ? null : this.Context.User; }
        }
    }

    /// <summary>
    ///     Marks a public method of a <see cref="WebService" /> as callable from the client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WebMethodAttribute : Attribute
    {
        /// <summary>
        ///     Kept for source compatibility; session state is always available to script services.
        /// </summary>
        public bool EnableSession { get; set; }
    }

    /// <summary>
    ///     Marks a <see cref="WebService" /> as callable from client script (JSON).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ScriptServiceAttribute : Attribute
    {
    }
}
