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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace MixERP.Net.Framework
{
    /// <summary>
    ///     Static access to the ambient web host services for the library code that used to
    ///     call <c>HttpContext.Current</c>, <c>ConfigurationManager.AppSettings</c> and
    ///     <c>HostingEnvironment.MapPath</c>. The host configures it once at startup through
    ///     <see cref="MixERPFrameworkServiceCollectionExtensions.UseMixERPFramework" />.
    /// </summary>
    public static class WebContext
    {
        private static IHttpContextAccessor accessor;
        private static IConfiguration configuration;
        private static string contentRootPath = AppContext.BaseDirectory;
        private static string webRootPath = AppContext.BaseDirectory;

        public static void Configure(IHttpContextAccessor httpContextAccessor, IConfiguration appConfiguration,
            string contentRoot, string webRoot)
        {
            accessor = httpContextAccessor;
            configuration = appConfiguration;

            if (!string.IsNullOrWhiteSpace(contentRoot))
            {
                contentRootPath = contentRoot;
            }

            webRootPath = string.IsNullOrWhiteSpace(webRoot) ? contentRootPath : webRoot;
        }

        /// <summary>
        ///     The current request, or null when there is none (background work, a Blazor
        ///     circuit after the initial request, or a non-web host).
        /// </summary>
        public static HttpContext Current
        {
            get { return accessor?.HttpContext; }
        }

        /// <summary>
        ///     The application root on disk (the replacement for
        ///     <c>HttpRequest.PhysicalApplicationPath</c>).
        /// </summary>
        public static string ContentRootPath
        {
            get { return contentRootPath; }
        }

        /// <summary>
        ///     Reads an application setting. The legacy <c>Web.config</c> appSettings keys are
        ///     looked up at the configuration root first and then under the <c>MixERP</c>
        ///     section, matching the convention used by MixERP.Net.i18n.
        /// </summary>
        public static string GetAppSetting(string key)
        {
            if (configuration == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return configuration[key] ?? configuration["MixERP:" + key];
        }

        /// <summary>
        ///     Maps an application-relative path (<c>~/x/y</c> or <c>/x/y</c>) to a physical path.
        ///     Static content (formerly the site root) lives under the web root when it exists
        ///     there; otherwise the path is resolved against the content root.
        /// </summary>
        public static string MapPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            string trimmed = path.Trim();

            if (Path.IsPathRooted(trimmed) && !trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                return Path.GetFullPath(trimmed);
            }

            string relative = trimmed.TrimStart('~').Replace('\\', '/').TrimStart('/');
            relative = relative.Replace('/', Path.DirectorySeparatorChar);

            string inWebRoot = Path.GetFullPath(Path.Combine(webRootPath, relative));

            if (File.Exists(inWebRoot) || Directory.Exists(inWebRoot))
            {
                return inWebRoot;
            }

            return Path.GetFullPath(Path.Combine(contentRootPath, relative));
        }

        /// <summary>
        ///     Resolves an application-relative url (<c>~/x</c>) against the request path base,
        ///     the replacement for <c>Control.ResolveUrl</c>.
        /// </summary>
        public static string ResolveUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("~", StringComparison.Ordinal))
            {
                return url;
            }

            string pathBase = Current?.Request.PathBase.Value ?? string.Empty;
            return pathBase.TrimEnd('/') + "/" + url.Substring(1).TrimStart('/');
        }
    }
}
