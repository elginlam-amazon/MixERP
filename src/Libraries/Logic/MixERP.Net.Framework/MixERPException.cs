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
using MixERP.Net.Web.UI;
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

#if !NET8_0_OR_GREATER
        public MixERPException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        public string DBConstraintName
        {
            get { return this.dbConstraintName; }
        }

#if !NET8_0_OR_GREATER
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

                //Server.TransferRequest is not available on ASP.NET Core: transfer within the executing page,
                //or redirect when the exception is not raised by a page.
                Page page = Page.GetCurrent(HttpContext.Current);

                if (page != null)
                {
                    page.Transfer("~/Site/RuntimeError.aspx");
                    return;
                }

                HttpContext.Current.Response.Redirect("~/Site/RuntimeError.aspx");
            }
        }

    }
}