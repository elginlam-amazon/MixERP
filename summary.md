# MixERP – .NET Framework 4.5.1 → .NET 10 upgrade

## Result

- All 54 requested projects (plus a new test project) build with **0 errors** (`dotnet build src/MixERP.Net.sln`, and each project individually).
- Remaining warnings: 37 (34 × CA1416 System.Drawing, 2 × CS8002, 1 × CS0649 – see Next steps). Before clean-up: ~750.
- New tests: `src/Tests/MixERP.Net.FrontEnd.Tests` (xUnit, 5 passing) cover the ASMX-compatible script-service endpoints.

## Approach

The application is ASP.NET Web Forms (≈210 .aspx/.ascx, ~100 server controls, 35 ASMX services, SignalR 2). Rather than rewriting every page, the existing Web Forms code keeps running on ASP.NET Core through **CoreWebForms 0.5.0** (`System.Web.UI` on .NET, incl. `CoreWebForms.SystemWebExtensions` for ScriptManager/UpdatePanel/JavaScriptSerializer and `CoreWebForms.Compiler.Dynamic` for runtime markup compilation) on top of **Microsoft.AspNetCore.SystemWebAdapters 2.3.0**.

### Project structure
- Non-UI libraries (Logic, Data, CurrencyAPI, FirstSteps, FrontEnd.Data, module `.Data` projects) target `net10.0;netstandard2.0`. Web Forms helpers inside `MixERP.Net.Common`/`MixERP.Net.Framework` are excluded from or `#if NET`-guarded on the netstandard2.0 leg.
- Web Forms server-control libraries (`MixERP.Net.WebControls.*`) and the FrontEnd modules target `net10.0` only (see Next steps).
- `MixERP.Net.FrontEnd` is now a `Microsoft.NET.Sdk.Web` app with `Program.cs`; `src/Directory.Build.props` disables SDK assembly-info generation where `Properties/AssemblyInfo.cs` exists.
- Modules remain runtime plug-ins: `Modules/Directory.Build.targets` copies module outputs into the FrontEnd output folder; `Application/ModuleLoader.cs` loads them and resolves their dependencies from the app folder (legacy bin probing).
- ProjectReference / solution paths fixed for case-sensitive file systems; legacy `packages.config`, `Web*.config` and OWIN `Startup.cs` removed.

### Package changes (selection)
| Old | New |
| --- | --- |
| Npgsql 2.2.3 | Npgsql 10.0.3 (net10.0) / 8.0.8 (netstandard2.0) |
| Microsoft.AspNet.SignalR 2.2 + OWIN | ASP.NET Core SignalR (shared framework) |
| Serilog 1.5.7 (RollingFile) | Serilog 4.3.0 + Serilog.Sinks.File 7.0.0 |
| MarkdownDeep.NET-Signed | Markdig.Signed 1.5.0 |
| HtmlRenderer.WinForms (lib folder) | HtmlRenderer.PdfSharp 1.6.1 (+ System.Security.Cryptography.Pkcs 10.0.14 pin for GHSA-846f-f764-76mx / GHSA-2fmv-rv2c-59x7) |
| System.Web / System.Web.Extensions | CoreWebForms 0.5.0, SystemWebAdapters 2.3.0 |
| Microsoft.VisualBasic (FileSystem.CopyDirectory) | `DirectoryHelper.CopyDirectory` (own implementation) |

### Code / configuration migration
- **Hosting** (`Program.cs`): SystemWebAdapters + wrapped ASP.NET Core session (60 min), `Global` registered via `AddHttpApplication<Global>()` (Application_Start/Error kept), System.Web routing, Web Forms + ScriptManager + dynamic pages, `.ashx` handlers (discovered from `<%@ WebHandler %>`), `FileUploadHanlder.ashx` and `Resources.js` handlers, SignalR hubs (`/signalr/{hubName}`), static files from the content root (server-side extensions never served), 1 GB request limit.
- **Web.config → App.config / appsettings.json**: `<appSettings>` moved to `App.config` (still read through `ConfigurationManager`); host logging in `appsettings*.json`. `<pages clientIDMode="Static" enableViewState="false" enableEventValidation="false">` applied in `Global.Application_PreRequestHandlerExecute`. Folder authorization (`Modules`, `Resource` → authenticated; `db` → denied) enforced by middleware.
- **Forms authentication → cookie authentication** (`MixERPWebpage.SetAuthenticationTicket/SignOut`), same login URL, 30-minute ticket, sliding 60-minute cookie.
- **ASMX → script-service endpoints** (`Application/ScriptServiceEndpoints.cs`, attributes/base class in `MixERP.Net.Framework/WebServices`): same URLs (`*.asmx/{method}`), JSON `{"d": …}` responses, `{"Message": …}` errors, XML for form posts; `EnableSession` honored.
- **SignalR 2 → ASP.NET Core SignalR**: hubs use `Clients.X.SendAsync(...)`; `Scripts/mixerp/core/mixerp-signalr.js` emulates the `$.connection.<hub>.client/server` API on `@microsoft/signalr` 8.0.7 (`Scripts/microsoft-signalr/signalr.min.js`, MIT).
- **Npgsql 2 → 10**: `Username`, removed `SyncNotification`/`SSL` (SslMode.Prefer kept), `NpgsqlException.Code` → `PostgresException.SqlState`, `NpgsqlError` → `PostgresNotice`, `ConstraintName` via `PostgresException`, PetaPoco resolves `NpgsqlFactory` directly (no machine.config provider registry), `Npgsql.EnableLegacyTimestampBehavior` switch set in both hosts.
- Removed/replaced unsupported APIs: CAS attributes (`AspNetHostingPermission`, `FileIOPermission` → real write probe), `Server.Transfer/TransferRequest/Execute` → `Response.Redirect`, `HttpBrowserCapabilities.Browsers` → user agent, `SHA512CryptoServiceProvider` → `SHA512.Create()`, `WebClient` → `HttpClient`, formatter-serialization members of `MixERPException` kept only for netstandard2.0.
- Restored files the automated pre-pass had broken (`Currencylayer/Converter.cs`, `Processor.cs`, `ReportManager/Client.cs`, deleted `AssemblyInfo.cs` files).

## Behavioral changes to review
- Transaction checklist e-mail attachment is a **PDF** (HtmlRenderer.PdfSharp) instead of a PNG/JPEG bitmap; the IE `WebBrowserImageSerializer` (unused, WinForms-only) was removed.
- Access-denied / runtime-error / restricted-mode pages are reached by redirect instead of server transfer (URL changes in the browser). The exception shown on `RuntimeError.aspx` is stored in session as its message only.
- `/Backups` (database backup downloads) now requires an authenticated user (previously anonymous).
- `ExpressionHelper.Eval` (CodeDom, unused) returns an empty string on .NET 10 (CodeDom compilation is not supported).
- `MixERP.Net.ReportManager` no longer references `WebControls.ReportEngine` (the reference was unused).
- Web Forms assemblies are now `CLSCompliant(false)` (CoreWebForms types are not CLS-marked); PetaPoco is marked `CLSCompliant(true)`.

## Next steps
- **Runtime verification (not done in this cycle – the app was not executed)**: smoke-test sign-in, a Scrud page, a module page (`/Modules/...` route + `LoadControl` of module .ascx), each ASMX call, file upload/undo, reports, and the SignalR features (EOD, update installer, DB backup, forced log-off). CoreWebForms 0.5.0 is a preview-quality community port; dynamic compilation of every .aspx/.ascx/.Master and `<%@ Register %>` directives must be checked page by page. Confirm `HttpContext.Current`/`AppUsers.GetCurrentUserDB()` inside SignalR hub invocations.
- **netstandard2.0 removed for UI projects**: `MixERP.Net.WebControls.*` (11) are Web Forms server controls – `System.Web.UI` has no netstandard2.0 surface, so they target net10.0 only; the 8 FrontEnd modules reference the web application (NU1201 rule). .NET Framework consumers must keep using the original builds.
- **CA1416 (34)**: `MixERP.Net.Common` image helpers / `ImageHandler.ashx` use System.Drawing (Windows-only on .NET 10). Linux hosting needs a move to SkiaSharp or ImageSharp (ImageSharp licensing is a product decision); public APIs expose `System.Drawing.Image/ImageFormat`.
- **CS8002 (2, netstandard2.0 leg of HtmlParser)**: HtmlRenderer packages are not strong-named; strong-named .NET Framework consumers may fail to load them.
- **CS0649**: `MixERPWebpage.menus` is never assigned (pre-existing; `VerifyAccess`/`GetMenu` would throw if reached).
- Cookie `SecurePolicy` is `SameAsRequest` to keep HTTP installs working; use `Always` behind HTTPS in production.
- The bundled `jquery.signalR.js` in `bundles/scripts/*` is now unused and can be removed when the bundles are regenerated.
- Consider replacing the ASMX compatibility layer with ASP.NET Core controllers and the Web Forms pages with Razor Pages/Blazor (KB 25/26) as a follow-up modernization.
