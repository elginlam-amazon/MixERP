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

namespace MixERP.Net.Framework.Errors
{
    /// <summary>
    ///     Holds the last unhandled <see cref="MixERPException" /> so the runtime-error page can render it.
    ///     This replaces <c>Session["ex"]</c>: in Blazor Server the instance is registered as a scoped
    ///     service, so it lives for the duration of the user's circuit, exactly as the session slot did
    ///     for the duration of the WebForms session.
    /// </summary>
    public class MixERPErrorState
    {
        /// <summary>
        ///     The exception recorded by <see cref="IMixERPErrorHandler.Handle" />, or null when none.
        /// </summary>
        public MixERPException Exception { get; private set; }

        /// <summary>
        ///     UTC time the exception was recorded.
        /// </summary>
        public DateTime? RecordedOn { get; private set; }

        /// <summary>
        ///     Raised whenever the recorded exception changes, so a rendering component can refresh.
        /// </summary>
        public event Action Changed;

        public void Set(MixERPException exception)
        {
            this.Exception = exception;
            this.RecordedOn = exception == null ? (DateTime?)null : DateTime.UtcNow;
            this.OnChanged();
        }

        public void Clear()
        {
            this.Exception = null;
            this.RecordedOn = null;
            this.OnChanged();
        }

        private void OnChanged()
        {
            Action handler = this.Changed;

            if (handler != null)
            {
                handler();
            }
        }
    }
}
