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

namespace MixERP.Net.i18n
{
    /// <summary>
    /// Holds the <see cref="I18nOptions" /> supplied by the host at startup.
    /// </summary>
    /// <remarks>
    /// The public localization surface of this library (<see cref="ResourceManager" />,
    /// <see cref="CurrentCulture" />) is static and is consumed by every MixERP module, so the
    /// configuration it needs is supplied once by the host through
    /// <see cref="I18nServiceCollectionExtensions.AddMixErpI18n" /> instead of being read
    /// ambiently from the legacy configuration and hosting-environment statics as it was on
    /// .NET Framework.
    /// </remarks>
    public static class I18nRuntime
    {
        private static I18nOptions current = new I18nOptions();

        /// <summary>
        /// The options currently in effect. Never null — defaults apply until the host configures it.
        /// </summary>
        public static I18nOptions Options => current;

        /// <summary>
        /// Called by the host during startup to supply configuration and the content root.
        /// </summary>
        public static void Configure(I18nOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            current = options;
        }
    }
}
