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
using System.Runtime.Serialization;
using System.Web;
using Serilog;

namespace MixERP.Net.Framework
{
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

#if NETSTANDARD2_0
        // Formatter-based serialization is obsolete on .NET (SYSLIB0051); kept for .NET Framework callers.
        public MixERPException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        public string DBConstraintName
        {
            get { return this.dbConstraintName; }
        }

#if NETSTANDARD2_0
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
#endif

        public static void Handle(MixERPException ex)
        {
            if (ex == null)
            {
                return;
            }

            if (HttpContext.Current.Session != null)
            {
                HttpContext.Current.Session["ex"] = ex;
                Log.Information("Exception object was added to session.");

#pragma warning disable SYSWEB0001 // The netstandard2.0 build runs on the .NET Framework System.Web, where TransferRequest is supported.
                HttpContext.Current.Server.TransferRequest("~/Site/RuntimeError.aspx", true);
#pragma warning restore SYSWEB0001
            }
        }

    }
}