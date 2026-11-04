# MixERP: .NET Framework 4.5.1 → .NET 10 upgrade

## Result

All 54 requested projects build with **0 errors**, both one at a time and through `MixERP.Net.sln`
(`Utilities/MixERP.Net.Utility.Deployment` has its own solution and builds on its own).
Three warnings remain, all in `MixERP.Net.FrontEnd` and all present before the upgrade (see "Next steps").

No test projects exist in the solution, so there were no unit tests to run. The application was not started.

## What the old code was (from CodeKB and the pre-upgrade sources)

- **FrontEnd** (`MixERP.Net.FrontEnd`) was an ASP.NET Web Forms site on .NET Framework 4.5.1. It had master pages,
  16 `.aspx` pages and 194 `.ascx` user controls. `Global.asax` handled `Application_Start` and `Application_Error`.
  Forms authentication stored the global login id as the user name. In-process session was used, and an OWIN
  `Startup` mapped ASP.NET SignalR 2. `web.config` held the `appSettings`, the `<pages>` defaults
  (`clientIDMode="Static"`, view state off, event validation off), the HTTP handlers (`Resources.js`,
  `FileUploadHanlder.ashx/.asmx`) and folder `<authorization>` rules.
- **Modules** (`MixERP.Net.Core.Modules.*`) were Web Forms class libraries. They held the code-behind of the
  module `.ascx` files, 30 ASMX script services (called from jQuery as `POST /…/X.asmx/Method` with a JSON body,
  answered with `{ d: … }`), an `.ashx` image handler and two SignalR hubs. They referenced the FrontEnd and
  were built into its `bin` folder, where ASP.NET loaded them.
- **Server controls** (`MixERP.Net.WebControls.*`) were composite Web Forms controls (ScrudForm, stock
  transaction forms, report engine and others). ScrudForm used an ASP.NET AJAX `UpdatePanel`.
- **Logic and data libraries** used Npgsql 2.2, PetaPoco, `System.Web` helpers (`HttpContext.Current`,
  `HostingEnvironment.MapPath`), `JavaScriptSerializer`, `System.Drawing` for thumbnails, and
  HtmlRenderer.WinForms to e-mail transactions as images.

## Approach

Rewriting about 210 Web Forms pages and controls to Razor in one pass was not feasible without changing
behaviour everywhere. The Web Forms code was kept and moved onto ASP.NET Core with **CoreWebForms 0.5.0**,
a port of `System.Web.UI` on top of the Microsoft System.Web adapters, plus `CoreWebForms.Compiler.Dynamic`,
which compiles `.aspx`/`.ascx`/`.Master` markup at runtime. The other `System.Web` APIs come from
`Microsoft.AspNetCore.SystemWebAdapters` 2.3.0.

### Target frameworks

| Projects | Target | Why |
|---|---|---|
| Logic and data libraries, `FirstSteps`, currency converters, `FrontEnd.Data`, `Modules/*.Data` | `net10.0;netstandard2.0` | Multi-targeted as required |
| `MixERP.Net.Common`, `MixERP.Net.Framework` | `net10.0;netstandard2.0` | Web Forms helpers are wrapped in `#if NET`, because CoreWebForms exists only on `net10.0` |
| `Libraries/Server Controls/Project/*` | `net10.0` only | Every type derives from `System.Web.UI` controls. No `netstandard2.0` implementation of Web Forms exists (NU1202 on CoreWebForms), so this is the same situation as the NU1201 escape hatch. |
| `Modules/*` (BackOffice, CRM, Finance, …) | `net10.0` only | They reference the FrontEnd application, which is NU1201 by definition |
| `MixERP.Net.FrontEnd` | `net10.0`, `Microsoft.NET.Sdk.Web` | Application |
| `MixERP.Net.Utility.SqlBundler` | `net10.0` | Console application |
| `MixERP.Net.Utility.Deployment` | `net10.0-windows` | WinForms and IIS installer, Windows only by design. `EnableWindowsTargeting` lets it build on Linux. |

### Main changes

- **Project files.** All projects are SDK-style. Project-reference casing was fixed for Linux
  (`MixERP.net.Common`, `DbFactory`/`DBFactory`, `MixERP.net.FrontEnd` in the `.sln`). `CoreServices` was removed
  from the libraries. Npgsql is 10.0.3 on `net10.0` and 8.0.8 on `netstandard2.0`. Serilog moved from a dev
  build to 4.4.0. `MarkdownDeep` (no assemblies for modern .NET) was replaced by `Markdig.Signed`. The
  `Microsoft.CSharp` packages that NU1510 flagged were removed from `net10.0`-only projects.
  A `Directory.Build.props` turns off generated assembly info where `Properties/AssemblyInfo.cs` exists
  (this keeps version 1.2.0.1 and `CLSCompliant`). The deleted `AssemblyInfo.cs` files of Common, HtmlParser
  and Deployment were restored.
- **Host (`Program.cs`, replaces `Global.asax` and the OWIN `Startup`).** Program.cs sets up:
  - Kestrel and form limits of 1 GB.
  - Session with a 60-minute timeout. The JSON session keys are registered: `Challenge`, `ex`,
    `FirstStepsPending`, `InvalidPasswordAttempts`, `StockTransactionFactory`, `Product`.
  - Cookie authentication: login path `/SignIn.aspx`, 30 minutes, sliding expiration.
  - SignalR and the System.Web adapters, with `AddHttpApplication<Global>()`. `Global.cs` keeps
    `Application_Start` and `Application_Error`, which the adapters call by name.
  - The page routes, through CoreWebForms routing.
  - The HTTP handlers from `web.config`, plus every `.ashx` file found by its `<%@ WebHandler Class %>` directive.
  - Web Forms with dynamic page compilation.
  - Static files from the content root, so the existing URLs keep working.
  - `PluginLoader`, which loads the `MixERP.Net.*.dll` plugins (modules, checklists, converters) from the
    output folder, as ASP.NET did with `bin`.
  - The Npgsql legacy timestamp switch.
- **Configuration.** `appSettings` moved to `App.config`, which becomes `MixERP.Net.FrontEnd.dll.config`, so
  every `ConfigurationManager.AppSettings` caller is unchanged. `appsettings.json` was added for the host.
  `Web.config` is now the ASP.NET Core Module (IIS) file. The `<pages>` defaults are set in the
  `MixERPWebPageBase` constructor; SignIn and Install now derive from it. The folder rules are in
  `Application/AccessRules.cs`: `/Modules` and `/Resource` need a signed-in user, `/db`, `/bin`, `/obj` and
  `/Properties` are blocked, and the root `*.json` files are never served. The module `Web.config`,
  `Web.Debug.config` and `Web.Release.config` files and the `packages.config` files were removed.
- **ASMX → script service endpoint.** `MixERP.Net.Framework.WebServices` provides `WebService`
  (Context/Session/Server/User), `[WebMethod]` and `[ScriptService]`. `Application/ScriptServiceEndpoints.cs`
  maps each `.asmx` file (by its `Class=` directive), plus `/FileUploadHanlder.asmx`, to
  `GET|POST {path}.asmx/{method}`. It binds JSON body properties to parameters the way `JavaScriptSerializer` did
  (strings converted with invariant TypeConverters) and returns `{ "d": … }` with Microsoft-format dates.
  Errors return HTTP 500 with the ASMX fault shape. Form posts and query strings get XML replies.
  `ListItem` is serialized as Text/Value/Selected/Enabled.
- **JavaScriptSerializer** was replaced by `JSONHelper.DeserializeUntyped`/`Serialize` (Newtonsoft). The helper
  returns the same `object[]`/`Dictionary` shapes, so the `dynamic` code is unchanged.
- **Forms authentication → cookie authentication.** `MixERPWebpage.SetAuthenticationTicket`, `SignOut` and
  `RequestLoginPage` were rewritten for cookie authentication. The user name is still the global login id.
- **SignalR 2 → ASP.NET Core SignalR.** There are four hubs (`BaseHub`, `UpdateHub`, `DayOperationHub`, `DbHub`),
  each mapped at `/signalr/{hubName}`. `Clients.X.y(...)` became `SendAsync("y", ...)`. The JS client is
  `@microsoft/signalr` 10.0.11 (`Scripts/signalr`). `Scripts/mixerp/hubs.js` recreates
  `$.connection.<hub>.client/server` and `$.connection.hub.start()`, so the page scripts are unchanged.
- **ASP.NET AJAX removal.** `<asp:ScriptManager>`/`ScriptManagerProxy` were removed. `ScriptManager.RegisterStartupScript`
  became `ClientScript.RegisterStartupScript`. The ScrudForm `UpdatePanel` became a `Panel` with the same ID.
  Unguarded `Sys.WebForms` calls are now guarded, and `pageLoad()` is called on document ready when `Sys` is absent.
- **Unsupported `Server.Transfer`/`Execute`/`TransferRequest`** calls became `Response.Redirect` to the same pages.
- **Npgsql 2 → 10:**
  - Connection strings use `Username`, and `SyncNotification`/`SSL` were removed (`SslMode=Prefer` stays).
  - `NpgsqlException.Code` became `PostgresException.SqlState`, and `ConstraintName` is read from `PostgresException`.
  - `NpgsqlError` notices became `PostgresNotice` (`MessageText`).
  - PetaPoco resolves `NpgsqlFactory` directly, because there is no `DbProviderFactories` registry.
- **System.Drawing → SkiaSharp 4.154.0** for thumbnails (`ImageHelper`, `ImageHandler`). The FrontEnd adds the
  Linux native assets.
- **Other API replacements:**
  - `SHA512CryptoServiceProvider` → `SHA512.Create()`.
  - `WebClient` → `HttpClient`, with download progress kept in the updater.
  - `FileIOPermission` check → a real write probe.
  - `Microsoft.VisualBasic.FileIO.CopyDirectory` → `DirectoryHelper.CopyDirectory`.
  - The CAS attributes (`AspNetHostingPermission`, `PermissionSet`, `SecurityPermission`) were removed, as was
    formatter-based serialization of `MixERPException`.
  - `RollingFile` → `Serilog.Sinks.File` with daily rolling.
- **Reverted three broken rewrites** left by the previous automated pass. They had assigned to
  `Headers["User-Agent"].ToString()` in `Currencylayer/Converter.cs`, `Processor.cs` and `ReportManager/Client.cs`.

### Behaviour changes worth knowing

- ScrudForm save, edit and delete now do full postbacks instead of UpdatePanel partial postbacks.
- Access-denied, restricted-mode and runtime-error pages are now reached by redirect, so the URL changes.
  `Server.Transfer` used to keep the original URL.
- The runtime error page gets the exception message from session (the full exception is logged), because
  session values are now serialized.
- The transaction checklist e-mail attaches the transaction as an `.html` document (`HtmlDocumentSerializer`).
  It used to be a PNG rendered by HtmlRenderer.WinForms or the IE `WebBrowser` control, both Windows-only and
  removed. The unused, unsupported `ExpressionHelper` (CodeDom runtime compilation) was deleted.
- Database backup downloads under `/Backups` now require a signed-in user.
- Each SignalR hub uses its own connection. The base hub and any hub the page subscribes to are opened.

## Next steps

- **Runtime validation (highest priority).** CoreWebForms 0.5.0 is an early, community-maintained port.
  Smoke-test SignIn, the dashboard, every module page and the ScrudForm and stock transaction forms against a
  PostgreSQL database. Pay attention to dynamic markup compilation, `<%@ Register %>` tag prefixes, `LoadControl`
  of module `.ascx` files and async pages (`Update.aspx`). Decide whether to keep CoreWebForms or plan a
  Razor Pages/Blazor rewrite (KB 25/26).
- `HttpContext.Current` (`AppUsers.GetCurrentUserDB()`) is used inside SignalR hub methods. It is not
  guaranteed there on ASP.NET Core; pass the catalog from `Context.User` if it is null at runtime.
- `DbHub` runs `pg_dump.exe` through a `.bat` file, which works on Windows only. A Linux build needs a `pg_dump` command.
- Session uses the JSON serializer, which throws on keys it does not know. Register any new `Session[...]` keys
  in `Program.RegisterSessionKeys`.
- The old jQuery SignalR 2 client is still bundled in `bundles/scripts/master-page.js` and `libraries.js`. It is
  inert because `hubs.js` replaces `$.connection`. Remove it when the bundles are rebuilt.
- Modules are copied into the FrontEnd output (`OutputPath`) because they reference the FrontEnd. Publishing has
  to build them and copy `MixERP.Net.Core.Modules.*`, `MixERP.Net.FirstSteps` and the currency converters next
  to the published FrontEnd.
- `netstandard2.0` was not kept for `Libraries/Server Controls/Project/*` and `Modules/*`, for the reasons in the
  table above.
- Remaining warnings, all from before the upgrade:
  - CS0649: `MixERPWebpage.menus` is never assigned, which makes `VerifyAccess` unusable.
  - CS3008: `Update.aspx.cs` has protected fields `_downloadUrl`/`_release` that are referenced from markup.
- CLS warnings CS3001/3002/3003/3009 are suppressed in `Directory.Build.props`. They only report that
  CoreWebForms, SkiaSharp and Npgsql types do not declare CLS compliance.
- `ImageHandler` maps any `Path` query value with `MapPath`. This path-traversal risk existed before the
  upgrade and should be restricted to the attachments folder.
