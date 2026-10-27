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
    ///     Base class for MixERP script services (formerly ASP.NET ASMX <c>System.Web.Services.WebService</c>).
    ///     ASMX is not available on .NET 10; the host application dispatches "*.asmx/{method}" requests
    ///     to the public methods marked with <see cref="WebMethodAttribute" />.
    /// </summary>
    [CLSCompliant(false)]
    public abstract class WebService : IDisposable
    {
        public HttpContext Context
        {
            get { return HttpContext.Current; }
        }

        public HttpSessionState Session
        {
            get { return this.Context?.Session; }
        }

        public HttpServerUtility Server
        {
            get { return this.Context?.Server; }
        }

        public IPrincipal User
        {
            get { return this.Context?.User; }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }

    /// <summary>
    ///     Marks a public method of a <see cref="WebService" /> as callable through "*.asmx/{method}".
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WebMethodAttribute : Attribute
    {
        public bool EnableSession { get; set; }
    }

    /// <summary>
    ///     Describes the XML namespace of a <see cref="WebService" /> (used for XML responses).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class WebServiceAttribute : Attribute
    {
        public const string DefaultNamespace = "http://tempuri.org/";

        public WebServiceAttribute()
        {
            this.Namespace = DefaultNamespace;
        }

        public string Namespace { get; set; }
    }

    /// <summary>
    ///     Indicates that a <see cref="WebService" /> can be invoked from client script (JSON).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ScriptServiceAttribute : Attribute
    {
    }

    public enum ResponseFormat
    {
        Json = 0,
        Xml = 1
    }

    /// <summary>
    ///     Specifies how a <see cref="WebMethodAttribute" /> method is invoked from client script.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ScriptMethodAttribute : Attribute
    {
        public ResponseFormat ResponseFormat { get; set; }
        public bool UseHttpGet { get; set; }
    }
}
