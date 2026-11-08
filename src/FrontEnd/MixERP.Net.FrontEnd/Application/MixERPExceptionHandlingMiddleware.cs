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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MixERP.Net.Framework;
using MixERP.Net.Framework.Errors;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Replacement for <c>Global.asax.cs</c>'s
    ///     <c>Application_Error → ApplicationError.Handle(Server.GetLastError())</c>.
    ///     <para>
    ///         The decision logic itself is NOT duplicated here: this middleware is only the new
    ///         trigger. It calls the ported <see cref="ApplicationError.Handle" /> with the
    ///         caught exception, which keeps the original order exactly — swallow a
    ///         <c>ThreadAbortException</c>, route a <c>MixERPException</c> (including an inner
    ///         one) to <c>MixERPException.Handle</c>, otherwise log and rethrow.
    ///     </para>
    ///     <para>
    ///         <c>Services/web.config</c> carried <c>&lt;customErrors mode="Off" /&gt;</c>, so the
    ///         five <c>/services/**</c> endpoints must answer with the RAW exception message —
    ///         their JavaScript callers parse the message text. Those requests therefore never
    ///         reach the redirect-to-runtime-error path.
    ///     </para>
    /// </summary>
    public sealed class MixERPExceptionHandlingMiddleware
    {
        /// <summary>
        ///     The legacy <c>Session["ex"]</c> key. <c>MixERPException.Handle</c> wrote the
        ///     exception there and <c>Site/RuntimeError.aspx</c> read it back. Over a Blazor
        ///     circuit that handoff is the scoped <see cref="MixERPErrorState" />; for a plain
        ///     HTTP request (where there is no circuit yet) the message is carried in the session
        ///     under the same key so the runtime-error page can render it after the redirect.
        /// </summary>
        public const string ExceptionSessionKey = "ex";

        private const string ServicesPathPrefix = "/services";

        private readonly RequestDelegate next;

        public MixERPExceptionHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await this.next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    // Nothing can be changed about a response already on the wire.
                    Log.Error("Exception occurred after the response started. {Exception}.", ex);
                    throw;
                }

                if (IsServicesRequest(context))
                {
                    await WriteRawServiceErrorAsync(context, ex);
                    return;
                }

                MixERPException mixERPException = ex as MixERPException ?? ex.InnerException as MixERPException;

                // ApplicationError.Handle reproduces the Application_Error decision tree:
                // it returns for a swallowed/handled exception and rethrows otherwise.
                ApplicationError.Handle(ex);

                if (mixERPException != null)
                {
                    RecordForRuntimeErrorPage(context, mixERPException);
                    context.Response.Redirect(MixERPErrorHandler.RuntimeErrorRoute);
                }
            }
        }

        private static bool IsServicesRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(ServicesPathPrefix,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     <c>customErrors mode="Off"</c> parity: the raw message, not a friendly page.
        /// </summary>
        private static async Task WriteRawServiceErrorAsync(HttpContext context, Exception ex)
        {
            Log.Error("Exception occurred in a service endpoint. {Exception}.", ex);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/plain; charset=utf-8";

            await context.Response.WriteAsync(ex.Message);
        }

        /// <summary>
        ///     Writes the exception into both handoff slots so the runtime-error page can render
        ///     it whether it is reached over a circuit or over a plain redirect.
        /// </summary>
        private static void RecordForRuntimeErrorPage(HttpContext context, MixERPException exception)
        {
            MixERPErrorState errorState = context.RequestServices.GetService<MixERPErrorState>();

            if (errorState != null)
            {
                errorState.Set(exception);
            }

            try
            {
                context.Session.SetString(ExceptionSessionKey, exception.Message ?? string.Empty);
            }
            catch (InvalidOperationException)
            {
                // Session is unavailable for this request (e.g. UseSession has not run for it).
                // The scoped error state above is then the only handoff, which is the same
                // degradation MixERPErrorHandler already documents.
            }
        }
    }
}
