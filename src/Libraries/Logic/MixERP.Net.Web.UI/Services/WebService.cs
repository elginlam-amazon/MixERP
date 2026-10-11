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
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
***********************************************************************************/

using System;
using System.Security.Principal;
using System.Web;
using System.Web.SessionState;

namespace MixERP.Net.Web.Services
{
    /// <summary>
    ///     Base class of JSON script services (formerly ASMX web services marked with [ScriptService]).
    ///     The host dispatches POST /{path}.asmx/{method} requests to public methods marked with <see cref="WebMethodAttribute" />,
    ///     binding the JSON body properties to parameters by name and wrapping the result as {"d": result}.
    /// </summary>
    public class WebService : IDisposable
    {
        [CLSCompliant(false)]
        public HttpContext Context => HttpContext.Current;
        [CLSCompliant(false)]
        public HttpSessionState Session => this.Context?.Session;
        [CLSCompliant(false)]
        public HttpServerUtility Server => this.Context?.Server;
        [CLSCompliant(false)]
        public HttpApplicationState Application => this.Context?.Application;
        public IPrincipal User => this.Context?.User;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WebMethodAttribute : Attribute
    {
        public bool EnableSession { get; set; }
        public int CacheDuration { get; set; }
        public string Description { get; set; }
        public string MessageName { get; set; }
        public bool BufferResponse { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class WebServiceAttribute : Attribute
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public enum WsiProfiles
    {
        None = 0,
        BasicProfile1_1 = 1
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class WebServiceBindingAttribute : Attribute
    {
        public WsiProfiles ConformsTo { get; set; }
        public bool EmitConformanceClaims { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Location { get; set; }
    }
}

namespace MixERP.Net.Web.Script.Services
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ScriptServiceAttribute : Attribute
    {
    }

    public enum ResponseFormat
    {
        Json = 0,
        Xml = 1
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ScriptMethodAttribute : Attribute
    {
        public ResponseFormat ResponseFormat { get; set; }
        public bool UseHttpGet { get; set; }
        public bool XmlSerializeString { get; set; }
    }
}
