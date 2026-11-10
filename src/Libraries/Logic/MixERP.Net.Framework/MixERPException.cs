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

using System.Runtime.Serialization;

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

        public MixERPException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public string DBConstraintName
        {
            get { return this.dbConstraintName; }
        }

        /// <summary>
        ///     Hands the exception over to the application's exception handler, which records
        ///     it for the runtime-error page and navigates the user there.
        /// </summary>
        /// <remarks>
        ///     On .NET Framework this stored the exception in <c>Session["ex"]</c> and performed
        ///     a <c>Server.TransferRequest("~/Site/RuntimeError.aspx")</c>. Both are gone on
        ///     modern .NET, so the behaviour now lives in
        ///     <see cref="IMixERPExceptionHandler" />, which the host registers and wires into
        ///     this facade at startup through <see cref="SetHandler" />. Until a handler is
        ///     wired up the exception is rethrown so that it reaches the ASP.NET Core
        ///     exception-handling middleware (which renders the runtime-error page) — it is
        ///     never silently swallowed.
        /// </remarks>
        public static void Handle(MixERPException ex)
        {
            if (ex == null)
            {
                return;
            }

            IMixERPExceptionHandler handler = MixERPExceptionHandlerRegistry.Handler;

            if (handler == null)
            {
                throw ex;
            }

            handler.Handle(ex);
        }

        /// <summary>
        ///     Wires the application's exception handler into the static <see cref="Handle" />
        ///     facade. Call this from the host's startup code.
        /// </summary>
        public static void SetHandler(IMixERPExceptionHandler handler)
        {
            MixERPExceptionHandlerRegistry.Handler = handler;
        }
    }
}
