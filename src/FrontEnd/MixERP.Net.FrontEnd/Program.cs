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
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MixERP.Net.ApplicationState;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Common.Helpers;
// The eight module Razor Class Libraries. Only their ENDPOINT and HUB aggregator namespaces are
// imported: the 194 ported components are never referenced by tag name from host markup (the host
// renders them through DynamicComponent, resolved by type), so the host's
// Components/_Imports.razor deliberately gains no module @using entries.
using MixERP.Net.Core.Modules.BackOffice.Endpoints;
using MixERP.Net.Core.Modules.BackOffice.Hubs;
using MixERP.Net.Core.Modules.Finance.Endpoints;
using MixERP.Net.Core.Modules.Finance.Hubs;
using MixERP.Net.Core.Modules.Inventory.Endpoints;
using MixERP.Net.Core.Modules.Purchase.Endpoints;
using MixERP.Net.Core.Modules.Sales.Endpoints;
using MixERP.Net.CurrencyConversion.Currencylayer;
using MixERP.Net.CurrencyConversion.OpenExchangeRates;
using MixERP.Net.Framework;
using MixERP.Net.Framework.Errors;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.FrontEnd.Components;
using MixERP.Net.FrontEnd.Endpoints;
using MixERP.Net.Framework.Security;
using MixERP.Net.FrontEnd.Services.Authentication;
using MixERP.Net.FrontEnd.Services.Layout;
using MixERP.Net.Messaging.Email.Helpers;
using MixERP.Net.WebControls.AttachmentFactory;
using MixERP.Net.i18n;
using Npgsql;
using PetaPoco;
using Serilog;

// ---------------------------------------------------------------------------------------------
// MixERP.Net.FrontEnd — Blazor Server host composition.
//
// This file replaces Global.asax / Global.asax.cs (Application_Start, Application_Error) and
// Hubs/Startup.cs ([assembly: OwinStartup] + app.MapSignalR()). All three are deleted.
//
// The ORDER of the initialization block after builder.Build() is load-bearing and is dictated by
// the dependency projects' migration summaries: the static configuration / cache / HTTP-context
// seams of MixERP.Net.Common, MixERP.Net.ApplicationState and MixERP.Net.Framework must be fed
// BEFORE any other MixERP call, or the configured pipeline is bypassed for the lifetime of the
// process (and degrades silently rather than throwing).
// ---------------------------------------------------------------------------------------------

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------------
// Request / response limits.
// Former httpRuntime maxRequestLength="1048576" (KB = 1 GB) and
// system.webServer/security/requestFiltering/requestLimits maxAllowedContentLength="1073741824".
// ---------------------------------------------------------------------------------------------
const long maxRequestBodySize = 1073741824L;

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = maxRequestBodySize);

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = maxRequestBodySize;
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartHeadersLengthLimit = int.MaxValue;
});

// Only relevant when reverse-proxied through IIS; harmless otherwise.
builder.Services.Configure<IISServerOptions>(o => o.MaxRequestBodySize = maxRequestBodySize);

// ---------------------------------------------------------------------------------------------
// Core seams: HttpContext, caching, session.
// (MixERP.Net.Common / MixERP.Net.ApplicationState / MixERP.Net.Framework host obligations.)
// ---------------------------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Former <sessionState mode="InProc" cookieless="UseCookies" timeout="60" />.
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(60);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// ---------------------------------------------------------------------------------------------
// Data protection with PERSISTED keys.
// Without persisted keys every restart invalidates the ProtectedSessionStorage payloads, which
// breaks the "Product" merge handoff between StockTransactionView (writer) and
// StockTransactionFactory (reader) SILENTLY - the reader simply gets null and the merge is lost.
// SetApplicationName pins the purpose string so the two sides agree across restarts/instances.
// ---------------------------------------------------------------------------------------------
string dataProtectionKeyPath = builder.Configuration["DataProtection:KeyDirectory"];

if (string.IsNullOrWhiteSpace(dataProtectionKeyPath))
{
    dataProtectionKeyPath = Path.Combine(builder.Environment.ContentRootPath, "Resource", "Temp", "keys");
}

Directory.CreateDirectory(dataProtectionKeyPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath))
    .SetApplicationName("MixERP.Net.FrontEnd");

// ---------------------------------------------------------------------------------------------
// Blazor Server.
// ---------------------------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

// AddInteractiveServerComponents already registers the protected browser storage services;
// TryAddScoped makes the StockTransactionView/StockTransactionFactory obligation explicit
// without double-registering it.
builder.Services.TryAddScoped<ProtectedSessionStorage>();

// ---------------------------------------------------------------------------------------------
// Authentication / authorization (checklist tasks 49-60).
// Former <authentication mode="Forms" loginUrl="~/SignIn.aspx" timeout="60"
// slidingExpiration="true" defaultUrl="~/Dashboard/Index.aspx" />.
// Every value below is the Web.config value carried over verbatim; the constants live on
// MixERPAuthentication so the pages and endpoints cannot drift from the scheme.
// NOTE the deliberate discrepancy that is PRESERVED from the legacy code: the cookie scheme
// expires in 60 minutes with sliding expiration, but SetAuthenticationTicket issued a 30 minute
// ticket (MixERPAuthentication.TicketExpiry). That difference is original behaviour.
// ---------------------------------------------------------------------------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = MixERPAuthentication.LoginPath;
        o.ReturnUrlParameter = MixERPAuthentication.ReturnUrlParameter;
        o.AccessDeniedPath = "/Site/AccessIsDenied";
        // The sign-out path is an endpoint, not a component: the cookie can only be deleted while
        // a real HTTP response is open (never over a Blazor circuit).
        o.LogoutPath = AuthEndpoints.SignOutRoute;
        o.ExpireTimeSpan = MixERPAuthentication.CookieExpiry;
        o.SlidingExpiration = true;
        o.Cookie.HttpOnly = true;
        o.Cookie.IsEssential = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// The MixERPAdminOnly policy. Admin status is NOT a claim: the sign-in principal carries only the
// globalLoginId subject (exactly as the legacy FormsAuthenticationTicket name did), so the policy
// resolves the login from the cache and applies the SAME rule MixERPAuthorization applies for
// AccessLevel.AdminOnly on a control. It exists because a handful of module service endpoints were
// reachable in the legacy app only through an admin-gated control, while the service itself was
// protected by nothing more than <deny users="?" /> — see the BackOffice ReportWriter endpoints.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(MixERPPolicies.AdminOnly, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            long globalLoginId = CurrentLoginProvider.GetGlobalLoginId(context.User as ClaimsPrincipal);

            if (globalLoginId <= 0)
            {
                return false;
            }

            try
            {
                return AppUsers.GetCurrent(globalLoginId).View.IsAdmin.ToBool();
            }
            catch (Exception ex)
            {
                //Fail CLOSED, and never silently: an unreadable login is not an administrator.
                Log.Warning("Could not evaluate the {Policy} policy for login {GlobalLoginId}. {Exception}.",
                    MixERPPolicies.AdminOnly, globalLoginId, ex);
                return false;
            }
        });
    });
});

// The authorization surface itself. PageAuthorizationState + CurrentLoginProvider are SCOPED, so
// each Blazor circuit (and each plain HTTP request) gets its own - which is what makes the
// current login readable over the circuit, where HttpContext is null.
builder.Services.AddScoped<PageAuthorizationState>();
builder.Services.AddScoped<CurrentLoginProvider>();
builder.Services.AddScoped<MixERPAuthorization>();
builder.Services.AddScoped<UserAuthenticationService>();

// The SAME two scoped instances, also resolvable through the MixERP.Net.Framework.Security
// contracts. This is what lets the eight Modules/* Razor Class Libraries inject the host's
// authorization and current-login services WITHOUT referencing this host project: their shared
// base class (MixERP.Net.Framework.Controls.MixERPUserControl) injects ICurrentLogin /
// IMixERPAuthorization, and MixERP.Net.WebControls.ScrudFactory.Scrud injects ICurrentLogin.
//
// The concrete classes could NOT be moved down into MixERP.Net.Framework with the base class:
// they resolve the login through MixERP.Net.ApplicationState.Cache.AppUsers and read
// MixERP.Net.Common / MixERP.Net.Entities types, and all three of those projects reference
// MixERP.Net.Framework, so moving them would close a project reference CYCLE.
//
// Each registration RESOLVES THE CONCRETE SERVICE rather than constructing a second one, so an
// interface consumer and a concrete consumer in the same scope share ONE instance. That matters:
// CurrentLoginProvider captures the remote IP address once, in its constructor, while the initial
// HTTP request is still open - a second instance created later in the circuit would capture
// nothing, and PageAuthorizationState denials recorded through one instance must be visible to
// the AuthorizationOutlet reading the other.
builder.Services.AddScoped<ICurrentLogin>(sp => sp.GetRequiredService<CurrentLoginProvider>());
builder.Services.AddScoped<IMixERPAuthorization>(sp => sp.GetRequiredService<MixERPAuthorization>());

// ---------------------------------------------------------------------------------------------
// The layout surface (checklist tasks 61-66). MasterPageScriptService replaces
// Base/MixERPMasterPage.RegisterJavascript() + Update(); every layout
// (Components/Layout/*.razor) calls RegisterAsync on its first render, which is the same
// coverage the master page's OnLoad had. SCOPED because the FirstStepsPending session value is
// only readable while the scope's HttpContext exists (the initial render).
// ---------------------------------------------------------------------------------------------
builder.Services.AddScoped<MasterPageScriptService>();

// ---------------------------------------------------------------------------------------------
// SignalR (replaces the OWIN app.MapSignalR() of the deleted Hubs/Startup.cs).
// Hubs/BaseHub.cs + Hubs/UpdateHub.cs are ASP.NET Core hubs (checklist tasks 100-102) and the
// single hub route is mapped below: app.MapHub<UpdateHub>("/hubs/update").
//
// The legacy <location path="Modules/Update.aspx"><httpRuntime executionTimeout="40000" /> was
// scoped to that ONE page request; the legacy SignalR hub was never covered by it. AddSignalR
// configures GLOBAL HubOptions, which also apply to Blazor Server's own component hub, so
// raising ClientTimeoutInterval here would stop dead circuits (and their scoped services and DB
// connections) from ever being reaped. The global value therefore stays at its 30 s default and
// the long-operation tolerance is scoped to UpdateHub only, via AddHubOptions<UpdateHub> below;
// the actual install/release waits are already bounded by WaitAsync inside UpdateHub and
// Update.razor, and the installer runs on a worker thread so keep-alives keep flowing.
// ---------------------------------------------------------------------------------------------
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
})
.AddHubOptions<MixERP.Net.FrontEnd.Hubs.UpdateHub>(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(40000);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
})
// BackOffice's DbHub (/hubs/backoffice/db) needs its own tolerance, for the same PER-HUB reason:
// BackupDatabase awaits pg_dump without blocking its dispatch thread, but the
// ZipFile.CreateFromDirectory and the recursive Resource/ copy that follow it ARE synchronous and
// routinely hold the invocation for minutes - far past the 30 s default ClientTimeoutInterval.
// Modules/migration-summary.md sections 12 and 13(d). Finance's DayOperationHub explicitly does
// NOT need one (it offloads the synchronous EOD run to a worker thread), so it is not configured.
// This MUST stay per-hub: the same values on the global HubOptions would also govern Blazor
// Server's component hub and stop dead circuits from being reaped - the exact regression the
// host's post-review fix F3 removed.
.AddHubOptions<MixERP.Net.Core.Modules.BackOffice.Hubs.DbHub>(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// ---------------------------------------------------------------------------------------------
// MixERP.Net.Framework: error state + handler (+ the per-circuit binding of the handler).
// ---------------------------------------------------------------------------------------------
builder.Services.AddScoped<MixERPErrorState>();
builder.Services.AddScoped<IMixERPErrorHandler, MixERPErrorHandler>();
builder.Services.AddScoped<CircuitHandler, MixERPErrorHandlerCircuitHandler>();

// ---------------------------------------------------------------------------------------------
// Localization (MixERP.Net.i18n) and data access (PetaPoco).
// ---------------------------------------------------------------------------------------------
builder.Services.AddMixErpI18n(builder.Configuration, builder.Environment);
builder.Services.AddPetaPoco(builder.Configuration, builder.Environment);

// ---------------------------------------------------------------------------------------------
// Currency conversion providers. Both are created by reflection
// (CurrencyConverter.GetAll / CurrencyData.GetExchangeRates), so the host must register them or
// their configured HTTP-client/configuration pipeline is bypassed permanently.
// ---------------------------------------------------------------------------------------------
builder.Services.AddCurrencylayer();
builder.Services.AddOpenExchangeRates();

// ---------------------------------------------------------------------------------------------
// Messaging (MixERP.Net.Messaging.Email): EmailOptions from the Messaging:Email section.
// ---------------------------------------------------------------------------------------------
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Messaging:Email"));

// ---------------------------------------------------------------------------------------------
// Request localization.
// Former <globalization culture="auto" uiCulture="auto" />, plus the per-user
// logins.View.Culture override that MixERPWebpage.InitializeCulture/SetCulture performed. The
// supported-culture list is the "Cultures" MixERP parameter - the same list the SignIn language
// selector offers - so a user culture outside it is not silently coerced away.
// ---------------------------------------------------------------------------------------------
builder.Services.AddLocalization();

builder.Services.AddSingleton<IConfigureOptions<RequestLocalizationOptions>>(
    new ConfigureRequestLocalization());

// ---------------------------------------------------------------------------------------------
// Background / startup work (replaces the Application_Start body).
// ---------------------------------------------------------------------------------------------
builder.Services.AddSingleton<IUpdateAvailabilityState, UpdateAvailabilityState>();
builder.Services.AddHttpClient(UpdateCheckService.HttpClientName);

// Ordering matters: the startup tasks run to completion before the server serves requests, and
// the update check starts afterwards.
builder.Services.AddHostedService<MixERPStartupTasks>();
builder.Services.AddHostedService<UpdateCheckService>();

WebApplication app = builder.Build();

// =============================================================================================
// INITIALIZATION ORDER - BLOCKING. Nothing MixERP may be called before this block completes.
// =============================================================================================
IHttpContextAccessor httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();

// 1. MixERP.Net.Common: configuration + content root + HttpContext seam. Replaces
//    ConfigurationManager, HostingEnvironment.MapPath and HttpContext.Current. Skipping this
//    bypasses the config + handler pipeline permanently (it degrades silently, it does not throw).
ConfigurationHelper.Initialize(app.Configuration, app.Environment.ContentRootPath, httpContextAccessor);
MixERPConfiguration.Initialize(app.Configuration, app.Environment.ContentRootPath, httpContextAccessor);

// 2. MixERP.Net.Framework: share the container's IMemoryCache with the static CacheFactory the
//    logic libraries use. Must happen before anything touches the cache.
CacheFactory.UseCache(app.Services.GetRequiredService<IMemoryCache>());

// 3. MixERP.Net.ApplicationState: the ambient-user seam behind AppState.CurrentUser /
//    AppUsers.GetCurrent(). Without it every login resolves as "no signed-in user".
AppState.UseHttpContextAccessor(httpContextAccessor);

// 4. Npgsql provider factory. PetaPoco resolves the provider by INVARIANT NAME, so this replaces
//    the deleted <system.data><DbProviderFactories> registration and must run before the first
//    database call.
DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);

// 5. Runtime-written directories IIS used to provide. Resource/ is at the content root (NOT
//    wwwroot) because it is written at runtime and must never be served as a static file.
foreach (string relative in new[]
         {
             Path.Combine("Resource", "Configs"),
             Path.Combine("Resource", "CustomReports"),
             Path.Combine("Resource", "Static", "Attachments"),
             Path.Combine("Resource", "Static", "Emails"),
             Path.Combine("Resource", "Temp"),
             Path.Combine("Resource", "Temp", "Images")
         })
{
    Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, relative));
}

// 6. Serilog, exactly as Application_Start's LogManager.IntializeLogger() did: level from the
//    MinimumLogLevel MixERP parameter, rolling file under ApplicationLogDirectory (falling back
//    to Resource/Temp) with the <date>/log.txt layout, and the "Application started." entry.
//    It is initialized AFTER the configuration/cache/provider seams because it reads a database
//    parameter through them.
LogManager.IntializeLogger(app.Environment.ContentRootPath);

// 7. Assembly discovery. Replaces AppDomain.CurrentDomain.GetAssemblies(), which does not see a
//    referenced-but-not-yet-loaded assembly on modern .NET - the bug behind
//    ItemSelector.GetAssemblyByName returning null and ICurrencyConverter / FirstStep items
//    silently disappearing from the UI. Every referenced module/control assembly is named
//    EXPLICITLY; RegisterFrom then adds the 8 Modules/* assemblies that are loaded dynamically.
DiscoveryAssemblies.Register(typeof(Program).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.FrontEnd.Data.Core.Widget).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Entities.Office.GlobalLogin).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Framework.MixERPException).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Common.Conversion).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.ApplicationState.Cache.AppUsers).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.i18n.ResourceManager).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.ReportManager.Repository).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Updater.UpdateManager).Assembly);
DiscoveryAssemblies.Register(typeof(PetaPoco.Database).Assembly);
DiscoveryAssemblies.Register(typeof(AttachmentFactoryEndpoints).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.WebControls.ReportEngine.Helpers.ParameterHelper).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.WebControls.ScrudFactory.ScrudForm).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.WebControls.TransactionChecklist.TransactionChecklistForm).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.CurrencyConversion.Currencylayer.Converter).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.CurrencyConversion.OpenExchangeRates.Converter).Assembly);

// The EIGHT module UI assemblies. These carry the 194 ported MixERPUserControl components and
// the 8 MixERPWidget dashboard widgets, which Services/Modules/ModuleComponentResolver.cs resolves
// BY TYPE NAME + TRAILING NAMESPACE SEGMENTS out of this very discovery set. They are named
// EXPLICITLY (not left to the RegisterFrom directory scan below) for the same reason as every
// entry above it: a referenced-but-not-yet-loaded assembly is invisible to a scan at the moment it
// runs, and an unregistered module assembly makes every /Modules/** route and the whole dashboard
// resolve to nothing. The anchor types are the modules' public endpoint aggregator / Index
// component types; RootNamespace and AssemblyName are preserved verbatim, so the resolver's
// namespace disambiguation keeps working.
DiscoveryAssemblies.Register(typeof(MixERP.Net.Core.Modules.Sales.Endpoints.SalesServiceEndpoints).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Core.Modules.Purchase.Endpoints.PurchaseServiceEndpoints).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Core.Modules.Inventory.Endpoints.InventoryServiceEndpoints).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Core.Modules.Finance.Endpoints.FinanceServiceEndpoints).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Core.Modules.BackOffice.Endpoints.BackOfficeServiceEndpoints).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Core.Modules.Manufacturing.Index).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Core.Modules.CRM.Index).Assembly);
DiscoveryAssemblies.Register(typeof(MixERP.Net.Core.Modules.POS.Index).Assembly);

DiscoveryAssemblies.RegisterFrom(AppContext.BaseDirectory);

// 8. MixERP.Net.Framework: process-wide fallback used when no circuit has bound its own handler
//    (e.g. a plain HTTP request). It MUST resolve from the CURRENT scope, never cache a scoped
//    instance - a cached handler would navigate another user's circuit.
MixERPErrorHandlerAccessor.UseFactory(() =>
{
    try
    {
        return httpContextAccessor.HttpContext?.RequestServices?.GetService<IMixERPErrorHandler>();
    }
    catch (Exception)
    {
        // MixERPErrorHandler needs a NavigationManager, which only exists inside a Blazor
        // scope. Outside one there is no handler; MixERPException.Handle then logs, and the
        // exception-handling middleware performs the redirect instead.
        return null;
    }
});

// 9. OpenExchangeRates: hand the container's IHttpClientFactory and the configuration to the
//    static seam the reflection-created Converter instances read.
app.Services.UseOpenExchangeRates(app.Configuration);

// 10. Log the route set and warn about any legacy route template no component claims.
MixERP.Net.FrontEnd.Application.Routes.Verify(typeof(Program).Assembly);

// =============================================================================================
// MIDDLEWARE PIPELINE
// =============================================================================================

// Replaces Application_Error. ORDER IS LOAD-BEARING: registration order is pipeline order, so
// the FIRST-registered middleware is the OUTERMOST one. The dev/prod exception pages catch
// everything downstream and do NOT rethrow, so they must sit OUTSIDE (be registered BEFORE)
// MixERPExceptionHandlingMiddleware — otherwise the MixERP handler's catch block never runs and
// ApplicationError.Handle, MixERPException.Handle, the Session["ex"] + MixERPErrorState handoff,
// the RuntimeError redirect and the /services/** raw-message (customErrors mode="Off") parity
// are all silently dead. ApplicationError.Handle rethrows anything that is not a
// MixERPException, so a genuinely unhandled exception still falls through to the page below.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(MixERPErrorHandler.RuntimeErrorRoute);
}

app.UseMiddleware<MixERPExceptionHandlingMiddleware>();

// Static files BEFORE routing/authorization, so the three former anonymous <location> blocks
// (bundles, static/images and Scripts -> wwwroot/js) stay reachable without authentication.
// The .backup mime map replaces the deleted staticContent/mimeMap + StaticHandler registration.
FileExtensionContentTypeProvider contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".backup"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = contentTypeProvider });

// Session before routing, so it is available to every endpoint and to SessionHelper /
// PageUtility.InvalidPasswordAttempts.
app.UseSession();

app.UseRouting();

app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// =============================================================================================
// ENDPOINTS
// =============================================================================================

// The non-circuit authentication seam (checklist tasks 51, 96): /_auth/request-login performs the
// full RequestLoginPage() behaviour (sign out, log the sign-off, clear every cookie, redirect to
// /SignIn?ReturnUrl=...) and /_auth/signout performs the plain sign-out. Interactive component
// code force-loads these because SignInAsync/SignOutAsync/cookie writes cannot run over a circuit.
app.MapMixErpAuthEndpoints();

// The five former Services/*.asmx [ScriptService] web services, now plain JSON endpoints
// (checklist tasks 75-80). They keep the ScriptService { "d": ... } response envelope and its
// { Message, StackTrace, ExceptionType } error payload - every javascript caller, including the
// eight Modules/* subprojects, reads those - answer both GET and POST as the old
// <webServices><protocols> block allowed, and enforce the former 5 MB maxJsonLength ceiling.
app.MapChecklistsServiceEndpoints();
app.MapInstallServiceEndpoints();
app.MapMenuServiceEndpoints();
app.MapOfficeServiceEndpoints();
app.MapUserServiceEndpoints();

// The 30 former Modules/**/Services/*.asmx web services and the one former
// Modules/BackOffice/Handlers/ImageHandler.ashx handler, aggregated by the five module projects
// that have any (Manufacturing, CRM and POS have none) — Modules/migration-summary.md section
// 13(b). They keep the same ScriptService {"d": ...} envelope and error payload as the host's own
// five, are .DisableAntiforgery() + .RequireAuthorization() (matching the deleted
// Modules/web.config <deny users="?" />), and therefore belong HERE: after
// UseAuthentication/UseAuthorization/UseAntiforgery and before MapRazorComponents<App>().
//
// MapBackOfficeServiceEndpoints also maps the ex-.ashx image proxy on its PRESERVED legacy url
// /Modules/BackOffice/Handlers/ImageHandler.ashx (built by hand in two places by
// Modules/BackOffice/wwwroot/js/AttachmentManager.ascx.js). Nothing shadows it: no host component
// claims a /Modules/BackOffice/** route (ModuleHost's /Modules/{*path} is a component route
// evaluated by MapRazorComponents, which is registered AFTER this and loses to a more specific
// literal endpoint anyway), and static-file middleware cannot intercept it either — Modules/ is
// not under wwwroot/, so neither this path nor the committed binary at
// Modules/Inventory/Services/Code is reachable through UseStaticFiles. Module static assets are
// served only from each RCL's wwwroot/, at _content/<AssemblyName>/..., and db/ stays unserved.
app.MapSalesServiceEndpoints();
app.MapPurchaseServiceEndpoints();
app.MapInventoryServiceEndpoints();
app.MapFinanceServiceEndpoints();
app.MapBackOfficeServiceEndpoints();

// /Resources.js - the exact legacy url and verbs of the deleted
// MixERP.Net.i18n.ResourcesHandler "JavascriptResources" handler registration.
app.MapResourcesEndpoints();

// POST /FileUploadHanlder.ashx and POST /FileUploadHanlder.asmx/UndoUpload - replaces the two
// deleted <handlers> entries (the misspelling is part of the contract: the client urls come from
// the attachment configuration table, which seeds exactly those paths).
app.MapAttachmentFactoryEndpoints();

// The embedded component-script route. Replaces ScriptResource.axd / WebResource.axd and
// ClientScript.GetWebResourceUrl for every control that still ships its JS as an
// <EmbeddedResource>: AttachmentFactory, ScrudFactory, ReportEngine (barcode/QR),
// StockTransaction*, Flag, PartyControl and TransactionChecklist. Without it those scripts 404
// and the forms render but do nothing.
app.MapGet(MixERP.Net.Common.Helpers.JSUtility.WebResourceRoutePrefix + "/{assemblyName}/{resourceName}",
    (string assemblyName, string resourceName) =>
    {
        try
        {
            Assembly assembly = Assembly.Load(assemblyName);
            string script = MixERP.Net.Common.Helpers.JSUtility.GetEmbeddedScript(resourceName, assembly);

            if (string.IsNullOrEmpty(script))
            {
                Log.Warning("Embedded script {ResourceName} was not found in {AssemblyName}.",
                    resourceName, assemblyName);
                return Results.NotFound();
            }

            return Results.Text(script, "application/javascript");
        }
        catch (Exception ex)
        {
            Log.Warning("Could not serve embedded script {ResourceName} from {AssemblyName}. {Exception}.",
                resourceName, assemblyName, ex);
            return Results.NotFound();
        }
    }).AllowAnonymous();

// The ONE SignalR hub route (checklist task 101). Replaces the OWIN app.MapSignalR() of the
// deleted Hubs/Startup.cs; the url is the one the javascript clients build their connection
// against (wwwroot/js/mixerp/master-page/notification.js and eod.js, reused by
// wwwroot/js/Pages/Modules/Update.js through window.updateHubConnection).
// UpdateHub derives from BaseHub, so "terminate" (eod.js) and "InstallUpdates" (the Update page)
// are both served by this single connection.
app.MapHub<MixERP.Net.FrontEnd.Hubs.UpdateHub>("/hubs/update");

// The two MODULE hub routes (Modules/migration-summary.md section 13(c)). Each module owns its
// MapXHubs() extension, so the route string lives next to the hub it maps:
//   MapFinanceHubs()    -> /hubs/finance/dayoperation  (DayOperationHub.PerformEOD, getNotification)
//   MapBackOfficeHubs() -> /hubs/backoffice/db         (DbHub.BackupDatabase, getNotification /
//                                                       backupCompleted / backupFailed)
// Those urls are exactly what Modules/Finance/wwwroot/js/EODOperation.js and
// Modules/BackOffice/wwwroot/js/admin/DatabaseBackup.ascx.js connect to. Together with
// /hubs/update above these are the only three hub routes in the solution; EODOperation.js's
// `terminate` deliberately reuses the host's /hubs/update connection so the host's eod.js modal
// and the page's progress bar stay in step.
app.MapFinanceHubs();
app.MapBackOfficeHubs();

// Blazor root. AddAdditionalAssemblies is intentionally NOT called. RE-VERIFIED at execution time
// over ALL EIGHT module projects now that the host references them:
// `grep -rn "^@page" --include=*.razor Modules/` returns NOTHING, and the earlier passes verified
// the same by reflection (0 RouteAttribute types in the built module assemblies). All 194 ported
// module components and all 8 dashboard widgets are NON-ROUTABLE: they are reached through the
// host's own /Modules/{*path} ModuleHost page and the dashboard, which resolve them BY TYPE through
// Services/Modules/ModuleComponentResolver. No referenced control RCL carries an @page either.
// OBLIGATION (keep this comment): if a @page is ever added to ANY referenced RCL — module or
// control — it must be listed BOTH here via .AddAdditionalAssemblies(typeof(X).Assembly) AND in
// Components/Routes.razor's <Router AdditionalAssemblies="..."> - one without the other produces
// intermittent 404s that depend on how the page was reached.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>
///     Supplies the request-localization options. Expressed as an
///     <see cref="IConfigureOptions{TOptions}" /> rather than an inline lambda so the supported
///     culture list is read when the options are first materialized (after the configuration
///     seams are initialized), not while the service collection is still being built.
/// </summary>
internal sealed class ConfigureRequestLocalization : IConfigureOptions<RequestLocalizationOptions>
{
    public void Configure(RequestLocalizationOptions options)
    {
        System.Collections.Generic.IList<System.Globalization.CultureInfo> cultures = SupportedCultures.Get();

        if (cultures.Count > 0)
        {
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.SetDefaultCulture(cultures[0].Name);
        }

        options.ApplyCurrentCultureToResponseHeaders = true;

        // The per-user logins.View.Culture override wins; everything else falls through to the
        // Accept-Language header, which is what culture="auto" uiCulture="auto" did.
        options.RequestCultureProviders.Insert(0, new MixERPUserCultureProvider());
    }
}
