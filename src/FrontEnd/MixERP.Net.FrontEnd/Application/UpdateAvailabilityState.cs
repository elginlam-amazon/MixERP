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

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Replacement for <c>HttpApplicationState["UpdateAvailable"]</c>, which
    ///     <c>UpdateManager.CheckForUpdates(this.Application)</c> used to set and
    ///     <c>Base/MixERPMasterPage.Update()</c> used to read.
    ///     <para>
    ///         Registered as a SINGLETON, because application state was process-wide. It is
    ///         deliberately not session/circuit scoped: the flag describes the deployment, not
    ///         the user. <c>MixERPMasterPage.Update()</c> still applies the per-user
    ///         <c>View.IsAdmin</c> gate on top of it (checklist task 66).
    ///     </para>
    /// </summary>
    public interface IUpdateAvailabilityState
    {
        /// <summary>
        ///     True once a newer release has been found. The legacy code only ever set this to
        ///     <c>true</c> and never cleared it, so the default is <c>false</c> and the setter is
        ///     the only transition.
        /// </summary>
        bool UpdateAvailable { get; }

        /// <summary>
        ///     Records that a newer release is available.
        /// </summary>
        void SetUpdateAvailable();
    }

    /// <summary>
    ///     Default singleton implementation of <see cref="IUpdateAvailabilityState" />.
    /// </summary>
    public sealed class UpdateAvailabilityState : IUpdateAvailabilityState
    {
        private volatile bool updateAvailable;

        public bool UpdateAvailable => this.updateAvailable;

        public void SetUpdateAvailable()
        {
            this.updateAvailable = true;
        }
    }
}
