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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.AspNetCore.SystemWebAdapters.SessionState.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MixERP.Net.Entities.Models.Transactions;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.FrontEnd.Base;
using MixERP.Net.i18n;
using MixERP.Net.WebControls.AttachmentFactory;

namespace MixERP.Net.FrontEnd
{
    public static class Program
    {
        // <requestLimits maxAllowedContentLength="1073741824" /> and <httpRuntime maxRequestLength="1048576" /> (KB).
        private const long MaxRequestLength = 1073741824;

        public static void Main(string[] args)
        {
            // MixERP was written against Npgsql 2.x, which read and wrote timestamps without UTC conversion.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            // The modules and plugins are copied next to the front end instead of being referenced.
            PluginLoader.LoadAll(AppContext.BaseDirectory);

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = MaxRequestLength);
            builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = MaxRequestLength);

            // <sessionState mode="InProc" cookieless="UseCookies" timeout="60" />
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // <authentication mode="Forms"><forms loginUrl="/SignIn.aspx" slidingExpiration="true" /></authentication>
            // The ticket was issued for 30 minutes (MixERPWebpage.SetAuthenticationTicket).
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = MixERPWebpage.LoginUrl;
                    options.ReturnUrlParameter = "ReturnUrl";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                });
            builder.Services.AddAuthorization();

            builder.Services.AddSignalR();

            ISystemWebAdapterBuilder systemWeb = builder.Services.AddSystemWebAdapters()
                .AddJsonSessionSerializer(RegisterSessionKeys)
                .AddWrappedAspNetCoreSession()
                .AddHttpApplication<Global>()
                .AddRouting(Routes.RegisterRoutes);

            // <system.webServer><handlers> of the former web.config.
            systemWeb.AddHttpHandler<ResourcesHandler>("/Resources.js");
            systemWeb.AddHttpHandler<FileUploadHanlder>("/FileUploadHanlder.ashx");

            // *.ashx files declare their handler type with <%@ WebHandler Class="..." %>.
            foreach (KeyValuePair<string, Type> handler in WebFormsDirectives.Find(builder.Environment.ContentRootPath, ".ashx"))
            {
                AddHttpHandler(systemWeb, handler.Value, handler.Key);
            }

            systemWeb.AddWebForms()
                .AddDynamicPages();

            WebApplication app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseFolderAuthorization();
            app.UseStaticFiles(GetStaticFileOptions(app.Environment));
            app.UseAuthorization();
            app.UseSession();
            app.UseSystemWebAdapters();

            // *.asmx files declare their service type with <%@ WebService Class="..." %>.
            List<KeyValuePair<string, Type>> services = WebFormsDirectives.Find(app.Environment.ContentRootPath, ".asmx").ToList();
            Type uploadService = WebFormsDirectives.ResolveType("MixERP.Net.WebControls.AttachmentFactory.Handlers.UploadService");
            services.Add(new KeyValuePair<string, Type>("/FileUploadHanlder.asmx", uploadService));
            app.MapScriptServices(services);

            MapHubs(app);

            app.MapHttpHandlers()
                .RequireSystemWebAdapterSession();

            app.Run();
        }

        private static StaticFileOptions GetStaticFileOptions(IWebHostEnvironment environment)
        {
            FileExtensionContentTypeProvider contentTypes = new FileExtensionContentTypeProvider();

            // <staticContent><mimeMap fileExtension=".backup" mimeType="application/octet-stream" /></staticContent>
            contentTypes.Mappings[".backup"] = "application/octet-stream";

            // The site keeps its folder layout (Scripts, Stylesheets, Static, bundles, Modules, ...), so static
            // files are served from the content root. Only known content types are served, so markup (.aspx),
            // code (.cs) and configuration (.config) are never returned as files.
            return new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(environment.ContentRootPath),
                ContentTypeProvider = contentTypes
            };
        }

        private static void RegisterSessionKeys(JsonSessionSerializerOptions options)
        {
            options.RegisterKey<string>("Challenge");
            options.RegisterKey<string>("ex");
            options.RegisterKey<bool>("FirstStepsPending");
            options.RegisterKey<int>("InvalidPasswordAttempts");
            options.RegisterKey<Collection<ProductDetail>>("StockTransactionFactory");

            // The merge model belongs to the stock transaction view control, which only the modules reference.
            Type mergeModel = WebFormsDirectives.ResolveType("MixERP.Net.WebControls.StockTransactionViewFactory.Data.Models.MergeModel");

            if (mergeModel != null)
            {
                options.KnownKeys["Product"] = mergeModel;
            }
        }

        private static void AddHttpHandler(ISystemWebAdapterBuilder builder, Type handlerType, string path)
        {
            MethodInfo method = typeof(Microsoft.Extensions.DependencyInjection.HandlerServicesExtensions)
                .GetMethod(nameof(Microsoft.Extensions.DependencyInjection.HandlerServicesExtensions.AddHttpHandler))
                .MakeGenericMethod(handlerType);

            method.Invoke(null, new object[] {builder, path});
        }

        /// <summary>
        ///     ASP.NET SignalR exposed every hub under /signalr; each hub now has its own endpoint,
        ///     /signalr/{camelCaseHubName}, which Scripts/mixerp/hubs.js connects to.
        /// </summary>
        private static void MapHubs(IEndpointRouteBuilder endpoints)
        {
            MethodInfo mapHub = typeof(HubEndpointRouteBuilderExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == nameof(HubEndpointRouteBuilderExtensions.MapHub) && m.IsGenericMethod &&
                             m.GetParameters().Length == 2);

            IEnumerable<Type> hubs = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && a.GetName().Name.StartsWith("MixERP.Net.", StringComparison.Ordinal))
                .SelectMany(GetLoadableTypes)
                .Where(t => t.IsClass && !t.IsAbstract && typeof(Hub).IsAssignableFrom(t));

            foreach (Type hub in hubs)
            {
                string name = char.ToLowerInvariant(hub.Name[0]) + hub.Name.Substring(1);
                mapHub.MakeGenericMethod(hub).Invoke(null, new object[] {endpoints, "/signalr/" + name});
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
    }
}
