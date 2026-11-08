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
using System.Configuration;

namespace MixERP.Net.Framework
{
    /// <summary>
    ///     Single read path for the application level settings that used to live in the
    ///     <c>&lt;appSettings&gt;</c> section of Web.config.
    /// </summary>
    /// <remarks>
    ///     Web.config does not exist on modern .NET: the web host now reads
    ///     <c>appsettings.json</c> through <c>Microsoft.Extensions.Configuration</c>, and
    ///     <c>ConfigurationManager.AppSettings</c> no longer sees those values. Rather than thread
    ///     <c>IConfiguration</c> through every library, the host registers a lookup once during
    ///     startup with <see cref="UseProvider" /> and the libraries keep reading settings by key.
    ///     When no provider is registered — the console utilities, which still ship a real
    ///     App.config, and unit tests — this falls back to
    ///     <see cref="ConfigurationManager.AppSettings" />, so those hosts are unaffected. See
    ///     knowledge base doc 11.
    /// </remarks>
    public static class AppSettings
    {
        private static Func<string, string> provider;

        /// <summary>
        ///     Registers the host's setting lookup. Called once from the web host's startup with a
        ///     delegate over its <c>IConfiguration</c>. Passing <c>null</c> restores the
        ///     <see cref="ConfigurationManager" /> fallback.
        /// </summary>
        public static void UseProvider(Func<string, string> valueProvider)
        {
            provider = valueProvider;
        }

        /// <summary>
        ///     Returns the setting for <paramref name="key" />, or <c>null</c> when it is not
        ///     configured.
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            Func<string, string> current = provider;

            if (current != null)
            {
                string value = current(key);

                if (value != null)
                {
                    return value;
                }
            }

            return ConfigurationManager.AppSettings[key];
        }

        /// <summary>
        ///     Returns the setting for <paramref name="key" />, or <paramref name="fallback" /> when
        ///     it is not configured.
        /// </summary>
        public static string Get(string key, string fallback)
        {
            return Get(key) ?? fallback;
        }
    }
}
