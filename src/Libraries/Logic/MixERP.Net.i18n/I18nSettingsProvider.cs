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

using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MixERP.Net.i18n
{
    /// <summary>
    ///     Supplies this library's settings: the database server parameters held in the
    ///     external installation config file, and the missing-resource behaviour switch.
    ///     Replaces the .NET Framework
    ///     <c>ConfigurationManager.AppSettings</c> + <c>HostingEnvironment.MapPath</c> +
    ///     <c>ConfigurationManager.OpenMappedExeConfiguration</c> combination.
    /// </summary>
    public interface II18nSettingsProvider
    {
        /// <summary>
        ///     Reads a single key from the <c>&lt;appSettings&gt;</c> of the external
        ///     database-server config file. Returns <see cref="string.Empty" /> when the
        ///     file, the section or the key is absent — the same fallback the original
        ///     implementation had.
        /// </summary>
        string GetDbServerParameter(string keyName);

        /// <summary>
        ///     True when a missing localized resource should return the resource key
        ///     instead of throwing.
        /// </summary>
        bool SupressMissingResourceException { get; }
    }

    /// <summary>
    ///     <see cref="IConfiguration" />-backed implementation of
    ///     <see cref="II18nSettingsProvider" />. The external config file keeps its
    ///     original XML shape (so existing MixERP installations are untouched); it is read
    ///     with <see cref="XDocument" /> rather than <c>System.Configuration</c>, which
    ///     makes it work on Linux as well as Windows.
    /// </summary>
    public sealed class I18nSettingsProvider : II18nSettingsProvider
    {
        private readonly I18nOptions options;
        private readonly string contentRootPath;
        private readonly ILogger logger;

        public I18nSettingsProvider(IOptions<I18nOptions> options, IHostEnvironment environment,
            ILogger<I18nSettingsProvider> logger = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(environment);

            this.options = options.Value;
            this.contentRootPath = environment.ContentRootPath;
            this.logger = logger ?? NullLogger<I18nSettingsProvider>.Instance;
        }

        public I18nSettingsProvider(I18nOptions options, string contentRootPath, ILogger logger = null)
        {
            ArgumentNullException.ThrowIfNull(options);

            this.options = options;
            this.contentRootPath = contentRootPath ?? AppContext.BaseDirectory;
            this.logger = logger ?? NullLogger<I18nSettingsProvider>.Instance;
        }

        /// <summary>
        ///     True when the configured value equals "TRUE" case-insensitively. A missing
        ///     or blank value yields false (the original expression threw a
        ///     NullReferenceException during static field initialization when the key was
        ///     absent; defaulting to false keeps the exception-throwing behaviour of
        ///     <see cref="ResourceManager.GetString" /> while no longer crashing at
        ///     type-load time).
        /// </summary>
        public bool SupressMissingResourceException
        {
            get
            {
                string value = this.options.SupressMissingResourceException;

                if (string.IsNullOrWhiteSpace(value))
                {
                    return false;
                }

                return value.ToUpperInvariant().Equals("TRUE", StringComparison.Ordinal);
            }
        }

        public string GetDbServerParameter(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return string.Empty;
            }

            string path = this.ResolveConfigFilePath(this.options.DbServerConfigFileLocation);

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                // The original implementation also degraded to an empty value here, but
                // silently. Log it: a missing installation config file means every
                // database-server parameter comes back blank and the resulting connection
                // string fails with an opaque Npgsql error.
                this.logger.LogWarning(
                    "MixERP localization: the database server config file '{ConfiguredPath}' (resolved to '{ResolvedPath}') was not found; '{KeyName}' resolves to an empty value. Set the 'DbServerConfigFileLocation' configuration key.",
                    this.options.DbServerConfigFileLocation, path, keyName);

                return string.Empty;
            }

            XDocument document;

            try
            {
                document = XDocument.Load(path);
            }
            catch (System.Xml.XmlException exception)
            {
                // System.Configuration raised ConfigurationErrorsException for malformed
                // config. The empty-value contract is kept, but the operator still gets to
                // see the failure.
                this.logger.LogError(exception,
                    "MixERP localization: the database server config file '{ResolvedPath}' is not well-formed XML; '{KeyName}' resolves to an empty value.",
                    path, keyName);

                return string.Empty;
            }

            XElement appSettings = document.Root?.Element("appSettings");

            if (appSettings == null)
            {
                this.logger.LogWarning(
                    "MixERP localization: the database server config file '{ResolvedPath}' has no <appSettings> section; '{KeyName}' resolves to an empty value.",
                    path, keyName);

                return string.Empty;
            }

            foreach (XElement add in appSettings.Elements("add"))
            {
                string key = add.Attribute("key")?.Value;

                if (key != null && key.Equals(keyName, StringComparison.Ordinal))
                {
                    return add.Attribute("value")?.Value ?? string.Empty;
                }
            }

            return string.Empty;
        }

        /// <summary>
        ///     Resolves an application-relative path such as
        ///     <c>~/MixERP.Installation.config</c> against the host's content root. This is
        ///     the cross-platform replacement for
        ///     <c>System.Web.Hosting.HostingEnvironment.MapPath</c>.
        /// </summary>
        private string ResolveConfigFilePath(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return string.Empty;
            }

            string trimmed = configuredPath.Trim();

            // Rootedness is decided on the RAW value, before any ~/separator rewriting.
            if (Path.IsPathRooted(trimmed))
            {
                return Path.GetFullPath(trimmed);
            }

            // A legacy Windows-style absolute path (e.g. C:\MixERP\x.config) is not
            // "rooted" on Linux, so it would otherwise be appended to the content root and
            // silently fail to resolve. Surface it instead of degrading quietly.
            if (trimmed.Length > 2 && trimmed[1] == ':')
            {
                this.logger.LogError(
                    "MixERP localization: 'DbServerConfigFileLocation' is the Windows absolute path '{ConfiguredPath}', which cannot be resolved on this platform. Use an application-relative path such as '~/MixERP.Installation.config' or a native absolute path.",
                    configuredPath);

                return string.Empty;
            }

            string relative = trimmed;

            if (relative.StartsWith("~", StringComparison.Ordinal))
            {
                relative = relative.Substring(1);
            }

            // Normalise the Windows-style separators found in legacy config values and
            // strip the leading slash so Path.Combine treats the value as relative.
            relative = relative.Replace('\\', '/').TrimStart('/');

            if (relative.Length == 0)
            {
                return string.Empty;
            }

            string combined = Path.Combine(this.contentRootPath, relative.Replace('/', Path.DirectorySeparatorChar));

            return Path.GetFullPath(combined);
        }
    }

    /// <summary>
    ///     Static facade over <see cref="II18nSettingsProvider" />, preserving the static
    ///     call sites (<see cref="ResourceManager" />, <see cref="ConnectionStringHelper" />)
    ///     that the 31 consuming projects rely on. Hosts call
    ///     <see cref="Configure" /> during startup; until then a neutral provider is used,
    ///     so the facade is never in an uninitialized (null) state.
    /// </summary>
    public static class I18nSettings
    {
        private static II18nSettingsProvider instance =
            new I18nSettingsProvider(new I18nOptions(), AppContext.BaseDirectory);

        /// <summary>
        ///     Points the static facade at the application's own settings provider, e.g.
        ///     <c>I18nSettings.Configure(app.Services.GetRequiredService&lt;II18nSettingsProvider&gt;())</c>.
        /// </summary>
        public static void Configure(II18nSettingsProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            instance = provider;
        }

        /// <summary>
        ///     The provider currently backing the static facade.
        /// </summary>
        public static II18nSettingsProvider Current
        {
            get { return instance; }
        }
    }
}
