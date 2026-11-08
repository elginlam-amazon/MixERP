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
using System.Web;
using Serilog;

namespace MixERP.Net.Framework
{
    /// <remarks>
    ///     The formatter based serialization members (the
    ///     <c>(SerializationInfo, StreamingContext)</c> constructor and the <c>GetObjectData</c>
    ///     override, which also carried a Code Access Security demand) were removed: BinaryFormatter
    ///     no longer exists on modern .NET, so that scaffolding is obsolete and inert
    ///     (SYSLIB0051, SYSLIB0003, CS0672). See knowledge base doc 28.
    /// </remarks>
    [Serializable]
    public class MixERPException : Exception
    {
        private readonly string dbConstraintName;

        public MixERPException()
        {
        }

        public MixERPException(string message)
            : base(message)
        {
        }

        public MixERPException(string message, Exception exception)
            : base(message, exception)
        {
        }

        public MixERPException(string message, Exception exception, string dbConstraintName)
            : base(message, exception)
        {
            this.dbConstraintName = dbConstraintName;
        }

        public string DBConstraintName
        {
            get { return this.dbConstraintName; }
        }

        /// <remarks>
        ///     The Web Forms implementation called <c>Server.TransferRequest</c>, which is not
        ///     supported on ASP.NET Core (SYSWEB0001): there is no way to re execute the pipeline
        ///     against another path in place. A client redirect reaches the same error page, and the
        ///     exception object still travels through the session, so the page renders the same
        ///     details. The visible difference is that the browser url now changes and the original
        ///     form post is not preserved.
        /// </remarks>
        public static void Handle(MixERPException ex)
        {
            if (ex == null)
            {
                return;
            }

            HttpContext context = HttpContext.Current;

            if (context == null)
            {
                return;
            }

            if (context.Session != null)
            {
                context.Session["ex"] = ex;
                Log.Information("Exception object was added to session.");

                context.Response.Redirect("~/Site/RuntimeError.aspx", false);
            }
        }

    }
}