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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using MixERP.Net.Framework;
using MixERP.Net.Framework.Errors;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Binds the circuit's own scoped <see cref="IMixERPErrorHandler" /> to the ambient
    ///     accessor, which is the host obligation
    ///     <c>MixERP.Net.Framework/migration-summary.md</c> records: until this is done, every
    ///     static <c>MixERPException.Handle(...)</c> call only logs and the user never reaches the
    ///     runtime-error page.
    ///     <para>
    ///         Registered as a SCOPED <see cref="CircuitHandler" />, so each user's circuit binds
    ///         its OWN handler — the summary is explicit that caching a scoped handler globally
    ///         would let one user's exception navigate another user's circuit.
    ///     </para>
    /// </summary>
    public sealed class MixERPErrorHandlerCircuitHandler : CircuitHandler
    {
        private readonly IMixERPErrorHandler errorHandler;

        public MixERPErrorHandlerCircuitHandler(IMixERPErrorHandler errorHandler)
        {
            this.errorHandler = errorHandler;
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            if (this.errorHandler != null)
            {
                MixERPException.UseErrorHandler(this.errorHandler);
            }

            return Task.CompletedTask;
        }

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            // The ambient slot is an AsyncLocal, so it is re-bound on every circuit callback
            // entry point rather than once at circuit creation.
            if (this.errorHandler != null)
            {
                MixERPException.UseErrorHandler(this.errorHandler);
            }

            return Task.CompletedTask;
        }

        public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
            Func<CircuitInboundActivityContext, Task> next)
        {
            return context =>
            {
                if (this.errorHandler != null)
                {
                    MixERPException.UseErrorHandler(this.errorHandler);
                }

                return next(context);
            };
        }
    }
}
