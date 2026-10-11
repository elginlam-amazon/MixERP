# MixERP: .NET Framework 4.5.1 → .NET 10 migration (cycle summary)

## Result
- All 55 projects build with **0 errors**: the 54 upgraded projects, the new `MixERP.Net.Web.UI` library, and a new test project. `dotnet build src/MixERP.Net.sln` exits 0. The Windows-only installer `Utilities/MixERP.Net.Utility.Deployment` builds on its own too, because it is not part of the main solution.
- 1 warning is left: CS0649 `MixERPWebpage.menus`. It is old dead code that was there before the migration (see Next steps).
- `dotnet test src/Tests/MixERP.Net.Web.UI.Tests` runs 17 tests and all pass. They cover rendering, the page lifecycle, post-back values and events, validators, master pages, `Server.Transfer` and `Response.Redirect`, all running through an in-memory ASP.NET Core host with System.Web adapters. The tests caught 3 bugs, which were fixed: a `Unit` default that rendered `width:0px`, an empty form `action`, and duplicate `<title>` tags.
- I did not run the application, as instructed. No runtime check against a PostgreSQL database was done.

## How the Web Forms application was migrated
Web Forms does not exist on .NET 10. MixERP builds its UI as a server-side control tree: modules are `.ascx` files that add server controls to placeholders, and the browser talks to ASMX JSON services. So the existing code was kept and moved onto a new rendering substrate, rather than rewriting about 45k lines into Razor in one cycle:

| Before (OLD code) | After |
|---|---|
| `System.Web.UI`, `HtmlControls`, `WebControls`, ASP.NET AJAX (`ScriptManager`/`UpdatePanel`) | **New `Libraries/Logic/MixERP.Net.Web.UI`** (net10.0;netstandard2.0). It is a working HTML control tree with a Web Forms-compatible lifecycle (FrameworkInitialize → PreInit → master pages → Init → post data → Load → post-back events → PreRender → Render). It includes AutoEventWireup (`Page_Load`/`Page_Init`), static client IDs, `ClientScriptManager`, validators, `GridView`/`BoundField`/`TemplateField`, and `WebResource.axd`. HTTP access goes through `Microsoft.AspNetCore.SystemWebAdapters` 2.3.0. The namespaces moved from `System.Web.UI*` to `MixERP.Net.Web.UI*`. |
| `.aspx`/`.ascx`/`.Master` markup (214 files) | Converted once into the `*.designer.cs` partials. The control tree is now built in `FrameworkInitialize`, which is what the ASP.NET page compiler used to generate. `<%= %>` expressions became render delegates and `<script runat=server>` code was moved into the class. When several markups shared one code-behind, each one became its own derived class (`BuildControlTree`). The markup files were deleted. Each class carries `[VirtualPath("~/…ascx")]` so that `LoadControl`, `MasterPageFile` and module routing still resolve. |
| ASMX `[ScriptService]` (36), `.ashx`, Web.config handlers | `FrontEnd/Hosting/ScriptServiceInvoker` keeps the ASMX JSON wire format (`{"d": …}`, `\/Date()\/` dates, `{"Message": …}` errors), so the client scripts did not change. The `.asmx`/`.ashx` stubs were replaced by `[VirtualPath]` attributes. |
| Global.asax, RouteTable, OWIN startup, Web.config | `Program.cs` plus `Hosting/*`: page routes (`/`, `/Reports/{path}`, `/Modules/{*path}`), an in-proc session manager (InProc, 60 min), cookie authentication that replaces Forms auth (`Base/FormsAuthentication.cs`), an allow-list for static files, and the "deny anonymous" rules for `/Modules` and `/Resource`. `appSettings` moved to `App.config`, which ships as `MixERP.Net.FrontEnd.dll.config` for `ConfigurationManager`. `Web.config` is now the ASP.NET Core Module config. |
| SignalR 2 (OWIN) | ASP.NET Core SignalR hubs at `/signalr/{hub}`. `Scripts/signalr/mixerp-hubs.js` provides the old `$.connection.<hub>.client/server` and `hub.start()` API on top of `@microsoft/signalr` 10.0.11. That library is vendored, MIT-licensed, and its npm integrity hash was verified. |

## Projects and packages
- Libraries target `net10.0;netstandard2.0`. Npgsql is conditional: 10.0.3 for net10 and 8.0.9 for netstandard2.0, the last version with netstandard2.0 support.
- FrontEnd uses `Microsoft.NET.Sdk.Web` and targets net10.0 only. The 8 UI module projects target net10.0 only, because they reference the FrontEnd application (the NU1201 exception). They build into the FrontEnd output folder, as the shared `bin` folder did before, and `Hosting/ModuleLoader` loads them at startup.
- Package replacements: Serilog 1.5 → 4.3.1 plus Serilog.Sinks.File; MarkdownDeep → Markdig.Signed; System.Drawing → SkiaSharp (Linux-safe); JavaScriptSerializer → Newtonsoft.Json; WebClient → HttpClient; CodeDom `ExpressionHelper.Eval` → `DataTable.Compute`, which is also safer and has no callers.
- Npgsql API changes: `PostgresException.SqlState`, `PostgresNotice.MessageText`, `Username`. The `SSL` and `SyncNotification` options were removed. PetaPoco now resolves the Npgsql provider directly, because there is no DbProviderFactories config.
- Removed: `packages.config`, binding-redirect `app.config` files, CAS attributes, and Mono.Security. The broken automated edits in `Currencylayer` and `ReportManager` were reverted, and the deleted `Certificate.cs` was restored.

## Behavioural changes
- UpdatePanel partial post-backs are now full post-backs. A small `Sys.WebForms.PageRequestManager` and `Sys.Application` shim keeps the existing scripts and `pageLoad()` working.
- Validators run on the server only. There is no WebUIValidation.js client validation any more.
- The transaction checklist e-mail attaches the transaction as an `.html` document instead of a PNG. HtmlRenderer.WinForms and the IE WebBrowser renderer are Windows-only and were removed, and the `EmailImageExtension` setting is no longer used.
- `Update.aspx` access-denied now uses `Transfer` instead of `Server.Execute`. Start-up DB tasks log their failure instead of crashing the process.

## Next steps
1. **Runtime verification.** Run the application against a MixERP PostgreSQL database and test sign-in, SCRUD post-backs, reports, attachments, EOD, backups and updates. Compare the rendered HTML with the old site. Only compile-time checks and unit tests were possible in this cycle.
2. **SignalR client.** Check that EOD, backup and update progress work through `mixerp-hubs.js`. The old `jquery.signalR-2.2.0.js` copies are now unused.
3. **Long-term UI direction (needs a product decision).** The UI now lives in generated C# (`*.designer.cs`). If you want editable markup, move the screens to Razor Pages or Blazor step by step (KB 25/26). The control library makes that possible one module at a time.
4. **Security review.** There is still no anti-forgery protection, because event validation was disabled before the migration as well. Session is in memory, so a farm deployment needs a distributed store. Confirm cookie `SecurePolicy` and HTTPS for production.
5. **Configuration.** The `ConfigurationManager` settings (App.config and Resource/Configs) could move to `IConfiguration`/appsettings per environment.
6. **Pre-existing dead code.** `MixERPWebpage.VerifyAccess`/`GetMenu`/`menus` (CS0649) were unused before the migration. Remove them or wire them up.
7. **System.Drawing on Windows only.** If image rendering of e-mails is still required, it needs a cross-platform HTML→image or PDF renderer.
