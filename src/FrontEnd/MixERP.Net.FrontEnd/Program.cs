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
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MixERP.Net.ApplicationState;
using MixERP.Net.Common;
using MixERP.Net.Entities.Office;
using MixERP.Net.Framework;
using MixERP.Net.Framework.Web;
using MixERP.Net.FrontEnd;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.FrontEnd.Hubs;
using MixERP.Net.i18n;
using MixERP.Net.ReportManager;
using Serilog;

// Replaces Global.asax (Application_Start / Application_Error), the OWIN SignalR startup and
// the system.web / system.webServer sections of Web.config. Settings that used to live in
// <appSettings> are in appsettings.json under the same key names.

WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // The site's static content (Scripts, Stylesheets, Static, bundles, Modules/*/Scripts, ...)
    // keeps its Web Forms layout at the content root instead of moving to wwwroot.
    WebRootPath = "."
});

// <httpRuntime maxRequestLength> / <requestLimits maxAllowedContentLength> (1 GB uploads).
const long maxRequestLength = 1073741824;
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxRequestLength);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxRequestLength);

builder.Services.AddMixERPFramework();
builder.Services.AddMixERPLocalization(builder.Configuration);
builder.Services.AddMixERPApplicationState();

// <sessionState mode="InProc" timeout="60" />
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// <authentication mode="Forms"><forms loginUrl="/SignIn.aspx" timeout="60" slidingExpiration="true" />
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/SignIn.aspx";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddSignalR();

// The module assemblies (MixERP.Net.Core.Modules.*) reference this host, so they are not
// project references; they are copied next to it and loaded here, as the Web Forms site
// loaded them from its bin folder.
Assembly[] moduleAssemblies = ModuleLoader.LoadModules(AppContext.BaseDirectory);

builder.Services.AddControllers(options =>
    {
        options.ValueProviderFactories.Insert(0, new ScriptServiceValueProviderFactory());
    })
    .ConfigureApplicationPartManager(manager =>
    {
        foreach (Assembly assembly in ModuleLoader.GetControllerAssemblies(moduleAssemblies))
        {
            manager.ApplicationParts.Add(new Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart(assembly));
        }
    });

WebApplication app = builder.Build();

app.Services.UseMixERPFramework();
app.Services.UseMixERPLocalization();
app.Services.UseMixERPApplicationState();

// Application_Error -> ApplicationError.Handle
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    Exception exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    Log.Error("Exception occurred. {Exception}.", exception);

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "text/plain; charset=utf-8";

    bool displayDetails = Conversion.TryCastBoolean(WebContext.GetAppSetting("DisplayErrorDetails"));
    await context.Response.WriteAsync(displayDetails && exception != null ? exception.Message : "An error occurred.");
}));

// Authentication runs first so the folder rules below can see the signed-in user.
app.UseAuthentication();

// IIS request filtering and the folder-level Web.config authorization rules.
app.UseMiddleware<ContentProtectionMiddleware>();

FileExtensionContentTypeProvider contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".backup"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(app.Environment.ContentRootPath),
    ContentTypeProvider = contentTypes
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.MapResourcesEndpoint();

// SignalR 2 (OWIN app.MapSignalR) -> ASP.NET Core SignalR.
app.MapHub<BaseHub>("/signalr/BaseHub");
app.MapHub<UpdateHub>("/signalr/UpdateHub");
ModuleLoader.MapModuleHubs(app, moduleAssemblies);

// Application_Start
LogManager.IntializeLogger();
_ = UpdateManager.CheckForUpdates();
GlobalLogin.CreateTable();
Repository.DownloadAndInstallReports();

app.Run();
