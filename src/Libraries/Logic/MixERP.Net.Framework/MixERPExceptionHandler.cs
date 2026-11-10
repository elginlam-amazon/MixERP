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

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace MixERP.Net.Framework
{
    /// <summary>
    ///     Holds the last unhandled <see cref="MixERPException" /> so that the runtime-error
    ///     page can render it. This is the modern replacement for <c>Session["ex"]</c>: it is
    ///     registered as a scoped service, which in Blazor Server means one instance per
    ///     circuit (and in MVC/Razor Pages one instance per request). Unlike
    ///     <c>HttpContext.Session</c>, it is available during interactive SignalR rendering.
    /// </summary>
    public sealed class MixERPErrorState
    {
        /// <summary>
        ///     The exception awaiting display, or null when none has been recorded.
        /// </summary>
        public MixERPException Exception { get; set; }

        /// <summary>
        ///     Reads and clears the recorded exception.
        /// </summary>
        public MixERPException Consume()
        {
            MixERPException ex = this.Exception;
            this.Exception = null;
            return ex;
        }
    }

    /// <summary>
    ///     Records a <see cref="MixERPException" /> for display and sends the user to the
    ///     runtime-error page. Replaces the WebForms
    ///     <c>Server.TransferRequest("~/Site/RuntimeError.aspx")</c> behaviour.
    /// </summary>
    public interface IMixERPExceptionHandler
    {
        void Handle(MixERPException ex);
    }

    /// <summary>
    ///     Default <see cref="IMixERPExceptionHandler" />: stores the exception in the
    ///     per-circuit <see cref="MixERPErrorState" />, logs that it has been recorded, then
    ///     navigates to the migrated runtime-error route.
    /// </summary>
    public sealed class MixERPExceptionHandler : IMixERPExceptionHandler
    {
        /// <summary>
        ///     The migrated replacement for <c>~/Site/RuntimeError.aspx</c>.
        /// </summary>
        public const string RuntimeErrorRoute = "/Site/RuntimeError";

        private readonly MixERPErrorState errorState;
        private readonly ILogger<MixERPExceptionHandler> logger;
        private readonly NavigationManager navigationManager;

        public MixERPExceptionHandler(MixERPErrorState errorState,
            ILogger<MixERPExceptionHandler> logger,
            NavigationManager navigationManager)
        {
            ArgumentNullException.ThrowIfNull(errorState);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(navigationManager);

            this.errorState = errorState;
            this.logger = logger;
            this.navigationManager = navigationManager;
        }

        public void Handle(MixERPException ex)
        {
            if (ex == null)
            {
                return;
            }

            this.errorState.Exception = ex;
            this.logger.LogInformation("Exception object was added to session.");

            this.navigationManager.NavigateTo(RuntimeErrorRoute);
        }
    }

    /// <summary>
    ///     Bridges the static <see cref="MixERPException.Handle(MixERPException)" /> facade to
    ///     the DI container. Because the handler depends on scoped services (the per-circuit
    ///     <see cref="MixERPErrorState" /> and <see cref="NavigationManager" />), the host
    ///     registers a FACTORY that resolves it from the current scope rather than a single
    ///     shared instance.
    /// </summary>
    public static class MixERPExceptionHandlerRegistry
    {
        private static Func<IMixERPExceptionHandler> factory;

        /// <summary>
        ///     Resolves the handler for the current scope, or null when the host has not
        ///     registered one.
        /// </summary>
        public static IMixERPExceptionHandler Handler
        {
            get
            {
                Func<IMixERPExceptionHandler> current = factory;
                return current == null ? null : current();
            }

            set
            {
                factory = value == null ? null : new Func<IMixERPExceptionHandler>(() => value);
            }
        }

        /// <summary>
        ///     Registers a scope-aware factory, e.g.
        ///     <c>MixERPExceptionHandlerRegistry.SetFactory(() =&gt; accessor.Services.GetRequiredService&lt;IMixERPExceptionHandler&gt;())</c>.
        /// </summary>
        public static void SetFactory(Func<IMixERPExceptionHandler> handlerFactory)
        {
            factory = handlerFactory;
        }
    }
}
