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
using MixERP.Net.Framework.Errors;
using Serilog;

namespace MixERP.Net.Framework
{
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

        /// <summary>
        ///     The error handler used by <see cref="Handle" /> for the CURRENT user. It replaces the
        ///     WebForms session / <c>Server.TransferRequest</c> pair, which has no equivalent in
        ///     ASP.NET Core. Resolved per circuit/request — never cached process-wide — because the
        ///     handler owns circuit-scoped state and that circuit's navigation manager.
        /// </summary>
        public static IMixERPErrorHandler ErrorHandler
        {
            get { return MixERPErrorHandlerAccessor.Current; }
        }

        /// <summary>
        ///     Binds the handler that <see cref="Handle" /> delegates to, for the current circuit/request
        ///     only. In Blazor Server call this from a <c>CircuitHandler</c> with the circuit's own scoped
        ///     instance:
        ///     <c>MixERPException.UseErrorHandler(scope.ServiceProvider.GetRequiredService&lt;IMixERPErrorHandler&gt;())</c>.
        ///     For the process-wide fallback, register a factory with
        ///     <see cref="MixERPErrorHandlerAccessor.UseFactory" /> instead — do not store a scoped
        ///     instance globally.
        /// </summary>
        public static void UseErrorHandler(IMixERPErrorHandler errorHandler)
        {
            if (errorHandler == null)
            {
                throw new ArgumentNullException(nameof(errorHandler));
            }

            MixERPErrorHandlerAccessor.SetCurrent(errorHandler);
        }

        /// <summary>
        ///     Records the exception for display and routes the user to the runtime-error page.
        ///     Equivalent of the original WebForms implementation, which put the exception into the session
        ///     and transferred the request to <c>~/Site/RuntimeError.aspx</c>.
        /// </summary>
        public static void Handle(MixERPException ex)
        {
            if (ex == null)
            {
                return;
            }

            IMixERPErrorHandler handler = ErrorHandler;

            if (handler == null)
            {
                // No handler configured: the original code also did nothing when the session was absent,
                // but a silent no-op would hide the failure, so record it.
                Log.Error(ex, "No IMixERPErrorHandler is configured; the exception could not be routed to the runtime-error page. Call MixERPException.UseErrorHandler at startup.");
                return;
            }

            handler.Handle(ex);
        }
    }
}
