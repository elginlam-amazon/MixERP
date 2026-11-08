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
using System.Threading;

namespace MixERP.Net.Framework.Errors
{
    /// <summary>
    ///     Ambient accessor for the current user's <see cref="IMixERPErrorHandler" />.
    ///     <para>
    ///         The WebForms code it replaces was per-request by construction, because it reached for
    ///         <c>HttpContext</c>'s session. Its Blazor replacement is per-circuit: the handler depends on
    ///         circuit-scoped state and on that circuit's <c>NavigationManager</c>, so it must NEVER be
    ///         cached in a plain static field — the last circuit to register would otherwise win and one
    ///         user's exception would be recorded in (and navigate) another user's circuit, and after that
    ///         circuit ended the captured navigation manager would be dead.
    ///     </para>
    ///     <para>
    ///         Resolution order: the ambient handler set for the current async flow
    ///         (<see cref="SetCurrent" />, called from a <c>CircuitHandler</c> or at the top of a request),
    ///         then the factory registered once at startup (<see cref="UseFactory" />), which resolves the
    ///         scoped handler from the ambient service scope.
    ///     </para>
    /// </summary>
    public static class MixERPErrorHandlerAccessor
    {
        private static readonly AsyncLocal<IMixERPErrorHandler> AmbientHandler =
            new AsyncLocal<IMixERPErrorHandler>();

        private static Func<IMixERPErrorHandler> factory;

        /// <summary>
        ///     The handler for the current async flow, or null when none can be resolved.
        /// </summary>
        public static IMixERPErrorHandler Current
        {
            get
            {
                IMixERPErrorHandler ambient = AmbientHandler.Value;

                if (ambient != null)
                {
                    return ambient;
                }

                Func<IMixERPErrorHandler> resolve = factory;
                return resolve == null ? null : resolve();
            }
        }

        /// <summary>
        ///     Binds the handler to the current async flow (circuit or request). Pass null to unbind.
        /// </summary>
        public static void SetCurrent(IMixERPErrorHandler errorHandler)
        {
            AmbientHandler.Value = errorHandler;
        }

        /// <summary>
        ///     Registers the factory used when no ambient handler is bound. The factory must resolve the
        ///     handler from the CURRENT service scope on every call — for example
        ///     <c>() => serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService&lt;IMixERPErrorHandler&gt;()</c>
        ///     or, preferably, from the circuit's own scope.
        /// </summary>
        public static void UseFactory(Func<IMixERPErrorHandler> errorHandlerFactory)
        {
            if (errorHandlerFactory == null)
            {
                throw new ArgumentNullException(nameof(errorHandlerFactory));
            }

            factory = errorHandlerFactory;
        }
    }
}
