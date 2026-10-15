# MixERP: .NET Framework 4.5.1 to .NET 10 migration summary

## Result

- All 54 projects are SDK-style and build with **0 errors**. `dotnet build MixERP.Net.sln` reports 1 warning (CS0649, which was already there; see Next steps). `Utilities/MixERP.Net.Utility.Deployment/MixERP.Net.Utility.Deployment.sln` builds with 0 warnings.
- Every project listed in the task was also built on its own, and each returned exit code 0.
- There are no test projects in the solution, so no unit tests were run. `FrontEnd/.../Tests` holds QUnit JavaScript tests only. The application was not started, as the task rules require.

## Approach

The application is ASP.NET Web Forms: 247 `.aspx`/`.ascx`/`.master` files, about 240 server-control classes and 35 ASMX script services. A page-by-page rewrite to Razor Pages could not be finished in one cycle without dropping functionality. So the Web Forms architecture stays, hosted on ASP.NET Core:

- **System.Web adapters** (`Microsoft.AspNetCore.SystemWebAdapters` 2.3.0) supply `HttpContext.Current`, the request and response, session, cache and `HostingEnvironment`. In libraries this is the netstandard2.0-compatible package. In the web host it is `.CoreServices`.
- **CoreWebForms** 0.5.0 (`System.Web.UI.*`: Page, MasterPage, UserControl, WebControls, HtmlControls, routing) and **CoreWebForms.Compiler.Dynamic** compile `.aspx`/`.ascx`/`.master` markup at runtime. CoreWebForms is a pre-1.0 community port, so runtime behaviour has to be validated (see Next steps).

## Project and target-framework layout

| Projects | Target |
|---|---|
| Logic libraries (PetaPoco, Framework, Common, i18n, ApplicationState, DBFactory, Entities, Messaging.Email, TransactionGovernor(.Data), Updater, HtmlParser), FirstSteps, CurrencyAPI x2, all `*.Data` projects (server controls, FrontEnd.Data, Modules.*.Data) | `net10.0;netstandard2.0` |
| Server-control projects (`MixERP.Net.WebControls.*`), module UI projects (`MixERP.Net.Core.Modules.*`) | `net10.0` (Web Forms only exists there) |
| `MixERP.Net.ReportManager` | `net10.0`. It was collapsed from multi-target because of **NU1201**: it references the Web Forms `ReportEngine` control. |
| `MixERP.Net.FrontEnd` | `Microsoft.NET.Sdk.Web`, `net10.0` (application) |
| `MixERP.Net.Utility.SqlBundler` | `net10.0` console |
| `MixERP.Net.Utility.Deployment` (WinForms/IIS installer) | `net10.0-windows` |

- `src/Directory.Build.props` holds the shared settings: LangVersion, `GenerateAssemblyInfo=false` (every project keeps `Properties/AssemblyInfo.cs`), Debug/Release only, `EnableWindowsTargeting`, and the CLS-compliance NoWarn.
- In Common and Framework, the Web Forms-only files are excluded from the netstandard2.0 build, or wrapped in `#if !NETSTANDARD2_0` (members of `PageUtility` and `Conversion`).
- `packages.config`, `.nuget` imports and the legacy `HintPath` references were removed. Wrong-case project paths in the solution and in project references were fixed for Linux.
- The module projects output to `FrontEnd/bin/<Configuration>/net10.0/`, which replaces the old `~/bin` layout. FrontEnd loads `MixERP.Net.Core.Modules.*.dll` at startup, and a `PublishModuleAssemblies` target copies them on publish.

## Package changes (pinned)

| Before | After |
|---|---|
| Npgsql 2.2.x + Mono.Security | Npgsql 10.0.3 (net10.0) / 7.0.10 (netstandard2.0), plus System.Text.Json 10.0.13 on netstandard2.0 to fix the NU1903 advisory |
| Serilog 1.5.7 (+FullNetFx, RollingFile) | Serilog 4.4.0 + Serilog.Sinks.File 7.0.0 (`WriteTo.File(..., rollingInterval: Day)`) |
| Newtonsoft.Json 6.0.4 | 13.0.4 |
| Microsoft.AspNet.WebApi.Client 5.2.3 | 6.0.0 |
| MarkdownDeep.NET-Signed | Markdig.Signed 1.4.0 (advanced extensions, raw HTML allowed, same as MarkdownDeep "Extra" mode) |
| Microsoft.AspNet.SignalR 2.2 / Owin | ASP.NET Core SignalR (shared framework) |
| System.Drawing (GDI+) | SkiaSharp 4.153.1 (+ `SkiaSharp.NativeAssets.Linux.NoDependencies` in FrontEnd) |
| HtmlRenderer.WinForms (lib folder) | HtmlRenderer.PdfSharp 1.6.1; `System.Security.Cryptography.Pkcs` pinned to 10.0.13 (the 8.0.1 version pulled in transitively had NU1902/NU1903 advisories) |
| System.Configuration / System.Runtime.Caching | 10.0.13 packages |

## Code migration (what changed and why)

The "before" behaviour of these components was confirmed with CodeKB against the pre-upgrade code.

- **Npgsql 2 to 10**:
  - Connection builders: `UserName` became `Username`, and `SyncNotification`/`SSL` were removed (`SslMode.Prefer` kept).
  - `NpgsqlException.Code` became `PostgresException.SqlState`. Catch blocks that rethrow now filter on `PostgresException`. `ListenNonQuery` and `IsServerAvailable` still catch `NpgsqlException`, so connection errors are handled as before.
  - `NpgsqlError` notices became `PostgresNotice` (`MessageText`).
  - `ex.ConstraintName` now goes through `PostgresException`.
  - PetaPoco resolves the "Npgsql" provider directly to `NpgsqlFactory.Instance`, because there is no machine.config `DbProviderFactories` registration any more.
- **ASMX script services**: `System.Web.Services` does not exist on .NET.
  - Services now derive from `MixERP.Net.Framework.WebServices.WebService` and use `[WebMethod]` from the same namespace. The decorative `[WebService]`, `[WebServiceBinding]` and `[ScriptService]` attributes were removed.
  - `FrontEnd/Application/LegacyEndpoints.cs` maps every `*.asmx` file it finds (it reads the `Class="..."` directive) to `~/x.asmx/{Method}`. It serves the ASP.NET AJAX contract the JavaScript uses: a JSON body of named arguments in, `{"d": result}` out with `\/Date()\/` dates, and HTTP 500 `{"Message"}` on error.
  - The same file maps `*.ashx` handlers. `FileUploadHanlder.asmx` (`UploadService`) is mapped explicitly; its `IHttpHandlerFactory` reflection hack was removed.
- **JavaScriptSerializer** was replaced by `JSONHelper.DeserializeDynamic`, which produces the same untyped `Dictionary`/`object[]`/int/decimal graph, and by `JsonConvert.SerializeObject`.
- **Forms authentication** became ASP.NET Core cookie authentication (`FrontEnd/Application/Authentication.cs`):
  - The user name is still the global login id.
  - The ticket lasts 30 minutes, `rememberMe` makes the cookie persistent, and the cookie uses a 60-minute sliding expiration.
  - The login URL is `/SignIn.aspx`.
- **Nested web.config authorization** is now middleware: `~/db` is denied to everyone, and `~/Modules` and `~/Resource` require sign-in.
- **Session**: `FrontEnd/Application/InProcSessionManager.cs` is an in-memory `ISessionManager`, equivalent to `<sessionState mode="InProc" timeout="60">`. Objects such as `MergeModel`, collections and exceptions stay live in session without serialization.
- **Global.asax**:
  - The `Global.asax` markup file was removed. `Global : HttpApplication` is registered with `AddHttpApplication<Global>()`, so `Application_Start` and `Application_Error` run as before.
  - Routes are registered through `AddRouting(Routes.RegisterRoutes)`. `routes.Ignore("{resource}.axd")` was dropped because there is no `.axd` handler any more.
  - The `<pages clientIDMode="Static" enableViewState="false" enableEventValidation="false">` defaults are applied in `PreRequestHandlerExecute`.
- **Web.config** became `Program.cs` plus `appsettings.json` (session timeout, a 1 GB request body limit that replaces maxRequestLength/maxAllowedContentLength), and `App.config`/`App.Release.config`:
  - The legacy `appSettings` are still read through `ConfigurationManager.AppSettings`.
  - `App.Release.config` reproduces the Web.Release.config transform: IsIDE=false, SupressMissingResourceException=true, DisplayErrorDetails=false.
  - All legacy `Web*.config` files were deleted.
  - Static files are served from the content root. `.config`, `.cs`, `.snk`, `.sql`, `.dll` and `.pdb` are never served, and `.backup` files are served as `application/octet-stream`.
- **ScriptManager/UpdatePanel**: these are not available.
  - The `<asp:ScriptManager>` tags and designer fields were removed.
  - The ScrudForm `UpdatePanel` became a `Panel` with the same `ScrudUpdatePanel` id, so the buttons now do full postbacks instead of partial ones.
  - `ScriptManager.RegisterStartupScript` became `ClientScript.RegisterStartupScript`.
  - Unguarded `Sys.WebForms` JavaScript in `Scrud.js` and `ApiAccess.ascx` is now guarded with `typeof Sys`.
- **SignalR**:
  - Hubs now use ASP.NET Core SignalR through the `MixERPHub` base class, which captures client proxies so that notifications from background database/process callbacks still reach the client.
  - `Clients.X.method(...)` calls became `SendAsync("method", ...)`.
  - `/signalr/hubs` now serves the hub metadata, and `Scripts/mixerp/signalr-hubs.js` recreates `$.connection.<hub>.client/.server` and `$.connection.hub.start()` on `@microsoft/signalr` 10.0.11, which is vendored in `Scripts/microsoft-signalr/`. The page scripts are unchanged.
- **System.Drawing to SkiaSharp**:
  - `ImageHelper.GetResizedImage` (high-quality bicubic, same crop and scale logic, JPEG output) and `Conversion.ConvertImageToByteArray`/`TryCastByteArray` now take an `SKBitmap`. The default JPEG quality stays 75.
  - `ImageHelper.GetImageFormat`/`GetFileExtension` now use `SKEncodedImageFormat`. The unused `GetEncoder` was removed.
  - `ImageHandler.ashx` decodes images with SkiaSharp.
- **HtmlParser**: the email attachment for transaction checklists was a PNG made with the WinForms HTML renderer, which is Windows/GDI-only. It is now a **PDF** made with HtmlRenderer.PdfSharp. **This is a behaviour change.** The unused IE `WebBrowserImageSerializer` was removed.
- **Removed or replaced APIs**:
  - CAS attributes (`AspNetHostingPermission`, `PermissionSet`, `SecurityPermission`) were removed.
  - `FileIOPermission` became a real write probe.
  - `Server.Transfer`/`Server.Execute` became `Response.Redirect`, and `Server.TransferRequest` in `MixERPException.Handle` became a redirect.
  - `Server.HtmlDecode` became `HttpUtility.HtmlDecode`.
  - `WebClient` became `HttpClient`. The updater download progress is now computed from the response stream.
  - `SHA512CryptoServiceProvider` became `SHA512.Create()`. The password hash output is unchanged.
  - `Microsoft.VisualBasic` `CopyDirectory` became a recursive copy.
  - `HttpBrowserCapabilities.Browsers` in log messages became the user agent.
  - The formatter-serialization members of `MixERPException` are now `#if !NET8_0_OR_GREATER`.
- **Removed dead code**: `Common/Helpers/ExpressionHelper.cs`. It was unused, and CodeDom compilation is not supported on .NET.
- Bad automated rewrites from the earlier pass (`UserAgent` turned into `Headers["User-Agent"]`) were reverted. The deleted `AssemblyInfo.cs` files and `Certificate.cs` were restored (`Certificate.cs` is excluded from the build, as it was originally).

## Next steps (non-blocking)

1. **Runtime validation of CoreWebForms 0.5.0**: smoke-test sign-in, the dashboard, Scrud pages, reports and entry forms. Check dynamic `.aspx` compilation of module pages, master pages, `Register` directives and postbacks. CoreWebForms is a preview community port, so in the long term plan to move the pages to Razor Pages or Blazor (KB 25/26).
2. **SignalR**: verify EOD, database backup and update notifications end to end, including `HttpContext.Current`/`AppUsers` inside hub calls. The bundled `jquery.signalR.js` in `bundles/*.bundle` is now superseded and can be removed from the bundles.
3. **ASMX dispatcher**: confirm that GET-style calls and complex argument types used by the JavaScript bind correctly. Error responses include `StackTrace` only in Development.
4. **Server.Transfer/Execute replacements** now change the browser URL (redirect). Confirm this is acceptable for the AccessIsDenied and RestrictedTransactionMode pages.
5. **Email attachment format** changed from image to PDF (HtmlParser). The `EmailImageExtension` setting no longer affects the file type. A product decision is needed.
6. **Session state** is in-process only. A web farm would need a distributed session manager.
7. **Pre-existing bug, left as is**: `MixERPWebpage.menus` is never assigned (CS0649), so the menu-policy check iterates over null. This is unchanged business logic and needs review by the owning developer.
8. **Strong naming**: `HtmlParser` references unsigned HtmlRenderer assemblies, so CS8002 is suppressed in that project.
9. **Hosting**: the `MixERPPublish.pubxml` WebDeploy profile and the `xsp4.bat` Mono script are legacy. Review IIS (ASP.NET Core Module) or container deployment (KB 05/19).
10. **The `vNext/` folder** in the repository was out of scope and was not migrated.
