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
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
***********************************************************************************/

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.Extensions.DependencyInjection;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.i18n;
using MixERP.Net.Web.Services;
using MixERP.Net.Web.UI;
using MixERP.Net.WebControls.AttachmentFactory;
using MixERP.Net.WebControls.AttachmentFactory.Handlers;
using IHttpHandler = System.Web.IHttpHandler;

namespace MixERP.Net.FrontEnd.Hosting
{
    /// <summary>
    ///     Endpoint which executes the migrated Web Forms application: pages (*.aspx), page routes
    ///     (the former Global.asax RouteTable registrations), script services (*.asmx/{method}), HTTP handlers and
    ///     embedded web resources (WebResource.axd).
    /// </summary>
    internal static class LegacyEndpoints
    {
        private const string DefaultPage = "~/SignIn.aspx";
        private const string ReportPage = "~/Reports/ReportMaster.aspx";
        private const string ModulePage = "~/Modules/Default.aspx";

        public static void MapLegacyApplication(this WebApplication app)
        {
            //Handlers formerly registered in the system.webServer/handlers section of Web.config.
            ControlRegistry.Register("~/Resources.js", typeof(ResourcesHandler));
            ControlRegistry.Register("~/FileUploadHanlder.ashx", typeof(FileUploadHanlder));
            ControlRegistry.Register("~/FileUploadHanlder.asmx", typeof(UploadService));

            app.MapGet(ClientScriptManager.WebResourceHandlerPath, WebResourceHandler.ProcessAsync);

            app.MapFallback("{**path}", ProcessAsync)
                .RequireSystemWebAdapterSession()
                .PreBufferRequestStream()
                .BufferResponseStream();
        }

        private static async Task ProcessAsync(HttpContext context)
        {
            string path = context.Request.Path.HasValue ? context.Request.Path.Value : "/";

            if (ScriptServiceInvoker.TryParse(path, out string servicePath, out string methodName))
            {
                Type serviceType = ControlRegistry.Resolve("~" + servicePath);

                if (serviceType != null && typeof(WebService).IsAssignableFrom(serviceType))
                {
                    await ScriptServiceInvoker.InvokeAsync(context, serviceType, methodName).ConfigureAwait(false);
                    return;
                }
            }

            System.Web.HttpContext legacyContext = context.AsSystemWeb();
            ApplyAutomaticCulture(context);

            Type type = ControlRegistry.Resolve("~" + path);

            if (type != null && typeof(Page).IsAssignableFrom(type))
            {
                ExecutePage(legacyContext, type, null);
                return;
            }

            if (type != null && typeof(IHttpHandler).IsAssignableFrom(type))
            {
                IHttpHandler handler = (IHttpHandler)Activator.CreateInstance(type);
                handler.ProcessRequest(legacyContext);
                return;
            }

            //Page routes (formerly registered in Application_Start).
            if (path == "/" || path.Length == 0)
            {
                ExecutePage(legacyContext, ControlRegistry.Resolve(DefaultPage), null);
                return;
            }

            string routePath;

            if (TryMatchRoute(path, "/Reports/", false, out routePath))
            {
                ExecutePage(legacyContext, ControlRegistry.Resolve(ReportPage), routePath);
                return;
            }

            if (TryMatchRoute(path, "/Modules/", true, out routePath))
            {
                ExecutePage(legacyContext, ControlRegistry.Resolve(ModulePage), routePath);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }

        private static bool TryMatchRoute(string path, string prefix, bool catchAll, out string routePath)
        {
            routePath = null;

            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string value = path.Substring(prefix.Length);

            if (string.IsNullOrWhiteSpace(value) || (!catchAll && value.Contains('/')))
            {
                return false;
            }

            routePath = Uri.UnescapeDataString(value);
            return true;
        }

        private static void ExecutePage(System.Web.HttpContext context, Type pageType, string routePath)
        {
            if (pageType == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            Page page = (Page)Activator.CreateInstance(pageType);

            if (routePath != null)
            {
                page.RouteData.Values["path"] = routePath;
            }

            try
            {
                page.ProcessRequest(context);
            }
            catch (Exception ex) when (!(ex is PageEndException))
            {
                //Formerly Application_Error: MixERPExceptions are shown on the runtime error page, others are rethrown.
                try
                {
                    LegacyApplication.Error(ex);
                }
                catch (PageEndException)
                {
                    //The error page was rendered.
                }
            }
        }

        /// <summary>
        ///     Equivalent of globalization culture="auto" uiCulture="auto": the browser's preferred language is used
        ///     until the page applies the culture of the signed-in user.
        /// </summary>
        private static void ApplyAutomaticCulture(HttpContext context)
        {
            string header = context.Request.Headers.AcceptLanguage.ToString();

            if (string.IsNullOrWhiteSpace(header))
            {
                return;
            }

            string language = header.Split(',')[0].Split(';')[0].Trim();

            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(language);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
            catch (CultureNotFoundException)
            {
                //Keep the default culture.
            }
        }
    }

    /// <summary>
    ///     Serves resources embedded in MixERP assemblies and declared with <see cref="WebResourceAttribute" />.
    /// </summary>
    internal static class WebResourceHandler
    {
        public static async Task ProcessAsync(HttpContext context)
        {
            string assemblyName = context.Request.Query["a"];
            string resourceName = context.Request.Query["r"];

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));

            WebResourceAttribute declaration = assembly?.GetCustomAttributes<WebResourceAttribute>()
                .FirstOrDefault(r => string.Equals(r.WebResource, resourceName, StringComparison.Ordinal));

            Stream stream = declaration == null ? null : assembly.GetManifestResourceStream(declaration.WebResource);

            if (stream == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            using (stream)
            {
                context.Response.ContentType = declaration.ContentType;
                context.Response.Headers.CacheControl = "public, max-age=86400";
                await stream.CopyToAsync(context.Response.Body).ConfigureAwait(false);
            }
        }
    }
}
