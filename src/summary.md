# MixERP.Net: .NET Framework 4.5/4.5.1 to net10.0 upgrade, cycle summary

## Result

- All 53 projects in scope are SDK-style and target `net10.0`. The one exception is the Windows Forms installer `MixERP.Net.Utility.Deployment`, which targets `net10.0-windows` because it uses Windows Forms, the registry, NTFS ACLs and IIS administration.
- `dotnet build MixERP.Net.sln --no-incremental` finishes with 0 errors and 0 warnings. Each of the 53 `.csproj` files also builds on its own with exit code 0.
- The repository has no test projects, so no unit tests could be run.
- The Web Forms user interface is **not** migrated yet. About 600 Web Forms source files (server controls, `.aspx`/`.ascx`/`.Master` code-behind and designer files) are still in the tree unchanged, but they are excluded from compilation. Each project lists them in an `ItemGroup Label="PendingWebFormsRewrite"`. The build is green because these files are excluded, not because they were converted. See "Next steps".

## What the old code did and what changed

CodeKB, the index of the pre-upgrade code, was used to map each component before it was changed.

### Shared libraries

- **MixERP.Net.Framework**
  - New `WebContext` static facade. It is bridged at startup by `UseMixERPFramework()` and replaces `HttpContext.Current`, `ConfigurationManager.AppSettings`, `HostingEnvironment.MapPath` and `Control.ResolveUrl` for library code.
  - New `Web/ScriptServiceController` and `ScriptServiceValueProviderFactory`. They keep the ASP.NET AJAX script-service wire contract, so the existing jQuery code keeps working:
    - same `/…/X.asmx/Method` URLs;
    - named JSON body parameters;
    - `{"d": result}` responses with PascalCase names and `\/Date()\/` dates;
    - HTTP 500 with `{Message, ExceptionType}` on errors.
  - The obsolete serialization constructor of `MixERPException` was removed (SYSLIB0051).
- **MixERP.Net.Common**
  - `PageUtility` works on the ASP.NET Core `HttpContext`/`ISession` instead of `Page`. `RegisterJavascript` became `GetScriptBlock`, because scripts are now emitted with the markup. `CleanUrl` uses `HttpClient` instead of `WebClient`.
  - `SessionHelper` stores values as JSON in `ISession`.
  - `ConfigurationHelper` reads and writes the external `*.config` appSettings files with `XDocument`. The existing files work unchanged.
  - `HtmlControlHelper` returns ASP.NET Core `TagBuilder` instead of `HtmlGenericControl`.
  - `ImageHelper` was ported from System.Drawing to SkiaSharp. System.Drawing is Windows-only on modern .NET.
  - `JSUtility.GetJSReference` inlines embedded scripts. WebResource.axd no longer exists.
  - `jQueryUI.GetjQueryUIDatePicker` returns the script.
  - `Conversion.TryCastUnit` returns a CSS length.
  - The password hash uses `SHA512.Create()`.
  - New `JSONHelper.DeserializeDynamic`, a drop-in for `JavaScriptSerializer.Deserialize<dynamic>`.
  - New `Models.ListItem`, a drop-in for `System.Web.UI.WebControls.ListItem` in JSON responses.
  - Removed unused or impossible helpers:
    - `ExpressionHelper`: it compiled C# at runtime with CodeDOM, which is not supported on .NET Core, and nothing called it.
    - `DropDownListHelper` and `GridViewHelper`: Web Forms only.
- **Npgsql 2.2 to 8.0.5** (DbFactory, FrontEnd.Data, Deployment, PetaPoco):
  - `UserName` was renamed to `Username`.
  - `SyncNotification` and `SSL` were removed; `SslMode.Prefer` is kept.
  - `NpgsqlError` became `PostgresNotice` (`MessageText`).
  - `NpgsqlException.Code` became `SqlState`.
  - `ConstraintName` is now read through `PostgresException`.
  - In PetaPoco, the obsolete `TrustServerCertificate` was removed. With `Prefer`, Npgsql 8 does not validate the certificate, which matches the 2.x behaviour.
- **Serilog** 1.5.7 to 4.4.0. `RollingFile` was replaced by `Serilog.Sinks.File` in the host.
- **Newtonsoft.Json** 6.0.4 to 13.0.4. **Microsoft.AspNet.WebApi.Client** 5.2.3 to 6.0.0.
- **MixERP.Net.Updater**
  - MarkdownDeep was replaced by Markdig (advanced extensions).
  - `WebClient` was replaced by streaming `HttpClient` with progress reporting.
  - `HostingEnvironment` and `ConfigurationManager` were replaced by `WebContext`.
- **MixERP.Net.ReportManager**: `WebClient` was replaced by `HttpClient`, and `MapPath` by `WebContext`.
- **MixERP.Net.HtmlParser**: see "Behaviour changes".
- **MixERP.Net.Messaging.Email**, **Entities**, the **TransactionGovernor** projects, **FirstSteps**, the **CurrencyAPI** projects, **SqlBundler** and every `*.Data` project: project conversion plus the `System.Web` and Npgsql fixes above.
- **Deployment**:
  - targets `net10.0-windows` with `UseWindowsForms`;
  - Microsoft.Web.Administration 7 was upgraded to 11.1.0;
  - `App.config` is kept and read through System.Configuration.ConfigurationManager;
  - `[SupportedOSPlatform("windows")]` was added;
  - the dead file `Certificate.cs` is still excluded, as it was before.
- For every project:
  - `packages.config` and the library `app.config` files (binding redirects only) were removed;
  - `Properties/AssemblyInfo.cs` is kept, with `GenerateAssemblyInfo=false`;
  - `[assembly: CLSCompliant(true)]` was removed, because the ASP.NET Core types in the public APIs are not CLS-compliant;
  - case mismatches in project and solution paths were fixed so the solution builds on Linux.

### Host (MixERP.Net.FrontEnd, Web SDK)

- `Global.asax`, the OWIN/SignalR `Startup` and `Web.config` were replaced by `Program.cs` and `appsettings.json`. `appsettings.json` keeps the same appSettings keys. `appsettings.Production.json` carries the `Web.Release.config` transforms.
- Equivalents for the old `Web.config` behaviour:
  - Forms authentication became cookie authentication, with `/SignIn.aspx`, 60 minutes and sliding expiration. `Application/AuthenticationTicket` replaces `FormsAuthenticationTicket`.
  - The InProc session (60 minutes) became the ASP.NET Core session.
  - The 1 GB request limit is kept.
- Static content is served from the content root, keeping the original layout. `ContentProtectionMiddleware` takes over from IIS request filtering and the folder rules:
  - `*.config`, `bin`, `obj`, `db` and `Resource/Configs` are never served;
  - `/Resource` and `/Backups` require a signed-in user.
- The five `Services/*.asmx` became `ScriptServiceController` controllers at the same URLs.
- SignalR 2 hubs became ASP.NET Core SignalR hubs. They are mapped at `/signalr/{HubName}`, and module hubs are found by reflection. Calls such as `Clients.X.method(...)` became `SendAsync("method", ...)`.
- `Resources.js` is served by the existing `MapResourcesEndpoint()`.
- The module assemblies reference the host, so the host cannot reference them back. `Modules/Directory.Build.targets` copies them next to the host, and `ModuleLoader` loads them and registers their controllers and hubs. Before, they were built into the site's `bin` folder.
- The module services became `ScriptServiceController` controllers at the same URLs:
  - 30 `.asmx` services in BackOffice, Finance, Inventory, Purchase and Sales;
  - `BackOffice/Handlers/ImageHandler.ashx`, which now uses SkiaSharp and only serves files inside the site;
  - `FileUploadHanlder.ashx` and `FileUploadHanlder.asmx` in AttachmentFactory.

### Web Forms controls converted to Blazor

- `MixERP.Net.WebControls.PartyControl` is now `PartyControl.razor`. The markup, the static element ids and the inline `PartyControl.js` are the same as before. This control is the pattern for the remaining render-only controls.

## Behaviour changes

- **The transaction checklist e-mail attachment is now an HTML file instead of an image.** The old code rasterised the HTML with the Internet Explorer WebBrowser control or HtmlRenderer.WinForms, and both are Windows-only. `HtmlImageSerializer` now writes a `.html` file, and the `EmailImageExtension` setting is no longer used. `ParserType`, `WebBrowserImageSerializer` and `HtmlRendererImageSerializer` were removed.
- **`ImageHelper.GetResizedImage` now takes `byte[]` instead of a System.Drawing `Image`.** It still returns a JPEG.
- **`ImageHandler` refuses paths outside the content root.** The old `MapPath` refused them, but `Path.Combine` would not.
- **`FileSystemHelper.IsDirectoryWritable` now tests by creating a probe file.** Code Access Security no longer exists.
- **Session values are strings.** `FirstStepsPending` is stored as `"True"`/`"False"`, and `Challenge` must be written with `SetString` when the sign-in page is rewritten.
- **SignalR URLs changed** from `/signalr` with generated proxies to `/signalr/{Hub}`.
- **The database backup (`DbHub`) runs `pg_dump` directly.** It used to generate a `.bat` file containing `SET PGPASSWORD=...`. Now `pg_dump` (or `pg_dump.exe` on Windows) is started with an argument list, and the password is passed only through the process environment. This works on Linux and no longer writes the password to disk.
- **Unhandled errors return a plain 500 response**, with details only when `DisplayErrorDetails` is true. Before, they redirected to `Site/RuntimeError.aspx`, which is still a Web Forms page and not converted.

## Next steps

1. **Web Forms to Blazor rewrite.** This is the main remaining work, and it decides whether the UI works at runtime. The files to rewrite are listed in each project's `PendingWebFormsRewrite` item group:

   | Project | Files |
   | --- | --- |
   | Core.Modules.BackOffice | 98 |
   | Core.Modules.Sales | 86 |
   | Core.Modules.Inventory | 71 |
   | Core.Modules.Finance | 60 |
   | WebControls.StockTransactionFactory | 43 |
   | FrontEnd | 43 |
   | WebControls.ScrudFactory | 36 |
   | Core.Modules.Purchase | 36 |
   | WebControls.StockTransactionViewFactory | 30 |
   | Core.Modules.CRM | 18 |
   | WebControls.TransactionViewFactory | 14 |
   | WebControls.ReportEngine | 14 |
   | WebControls.TransactionChecklist | 13 |
   | WebControls.AttachmentFactory | 11 |
   | WebControls.StockAdjustmentFactory | 9 |
   | WebControls.Flag | 5 |
   | WebControls.Common | 5 |
   | Core.Modules.POS | 4 |
   | Core.Modules.Manufacturing | 4 |

   There are 214 `.aspx`/`.ascx`/`.Master` markup files in total. The recommended order:
   1. Convert the shared server controls (WebControls.*) to Razor components, following the `PartyControl.razor` pattern. Render-only controls first: Common/DateTextBox, AttachmentFactory, StockAdjustmentFactory, StockTransactionFactory and ReportEngine.
   2. Convert the controls that post back. These need component state and `@onclick` handlers in place of postbacks and hidden fields: ScrudFactory (save, edit, delete), StockTransactionView, TransactionViewFactory, TransactionChecklist and Flag.
   3. Rewrite `FrontEnd/Base` (MixERPWebpage, MixERPUserControl, MixERPMasterPage) on top of the Blazor `MixERPWebPageBase`/`MixERPUserControlBase`. Turn the master pages into layouts, and the pages (SignIn, Dashboard, Modules/Default, Reports, Site/*) into routable components. Then add `AddRazorComponents().AddInteractiveServerComponents()`, `App.razor`/`Routes.razor`, and `AddAdditionalAssemblies(moduleAssemblies)` in `Program.cs`.
   4. Port the module `.ascx` controls. Most Setup screens are thin wrappers around ScrudForm.
   5. Delete each original Web Forms file once its replacement is wired up.
2. **SignalR JavaScript client.** Pages that use hubs (Update, Database backup, EOD operation) must move from the SignalR 2 jQuery client to `@microsoft/signalr` (`new signalR.HubConnectionBuilder().withUrl("/signalr/UpdateHub")`). Hub-to-client method names are unchanged.
3. **Auth hardening (needs a product decision).** The script services stay anonymous at the transport level, as they were in Web Forms. Each method checks `AppUsers` itself. Consider `[Authorize]` on all services except `User.Authenticate` and `Office.GetOffices`. Also review the `../` path handling in `Attachments.DeleteImage` and `UploadService.UndoUpload`; both keep the old behaviour.
4. **Culture.** `<globalization culture="auto">` has no direct equivalent in the pipeline yet. Consider `UseRequestLocalization` with an Accept-Language provider. The script-service binder already converts JSON strings with the current culture and numbers with the invariant culture.
5. **Data access verification.** Run integration tests against PostgreSQL for the Npgsql 2 to 8 change. Type mapping (for example `timestamp` and `timestamptz`) and the notice events used by the EOD hub cannot be checked at compile time.
6. **Error page.** Once `Site/RuntimeError` is a component, register `MixERPException.SetHandler` per circuit, as described in the AWS Transform notes for MixERP.Net.Framework (iteration artifacts, `migration-summary.md`), and redirect from the exception handler.
7. **Cleanup that needs a decision.**
   - The legacy `src/packages/` folder (81 MB of NuGet 2 packages) and `.nuget/` are no longer used by any project and can be deleted.
   - The stray, case-colliding folder `FrontEnd/MixERP.net.FrontEnd/` (it holds only `Resource/Static/Attachments/web.config`) should be merged or removed.
8. **Npgsql version.** Npgsql 8.0.5 was kept for consistency with the projects atx migrated. Moving to Npgsql 10.x, as planned, is optional.

## Carry-over from iteration 20

Each item from the iteration 20 "Next steps" was checked against the current tree. Some of those notes described a different state. For example, they mention Npgsql 10, an `ExpressionHelper` that throws, and `/data/code/summary.md`, which does not exist. The current tree uses Npgsql 8.0.5 in all 25 references, `ExpressionHelper` is deleted, and this file is the record.

Resolved in this cycle:
- **ASMX services:** all 35 were rewritten as controllers at the same URLs (see "Host").
- **HTML-to-image serializers:** replaced by an HTML attachment (see "Behaviour changes").
- **`DbHub` backup:** now calls `pg_dump` directly, with no `.bat` file.
- **`ExpressionHelper.Eval`:** deleted. Nothing called it, and runtime C# compilation from configuration is a security risk. Bringing it back would need Roslyn scripting and a product decision.
- **`Session["ex"]`:** replaced by the scoped `MixERPErrorState`. The session holds only strings and ints (`Challenge`, `FirstStepsPending`, `InvalidPasswordAttempts`), so no custom session serializer is needed.
- **Password hash:** `SHA512CryptoServiceProvider` became `SHA512.Create()`. It is the same algorithm over the same UTF-8 input, so existing hashes still match. Confirm by signing in with an existing account.
- **Module DLL deployment for `dotnet build`:** `Modules/Directory.Build.targets` copies the module DLLs next to the host, so their controllers and hubs load.

Still open, added to "Next steps":
9. **Module DLLs on publish.** The copy runs only after `Build`. `dotnet publish` of the host does not include the modules. Either publish each module into the host's publish folder, or add a publish target.
10. **Forwarded headers.** `PageUtility.GetUserIpAddress` still trusts the `X-Forwarded-For` header from any client, as the Web Forms code did. Configure `UseForwardedHeaders` with the real proxy's `KnownProxies`/`KnownNetworks`, then read `Connection.RemoteIpAddress`. This needs the deployment topology.
11. **Runtime validation.** Run the host against a test PostgreSQL database. Check DI, middleware order and session behaviour, sign-in with existing accounts, the Npgsql 8 data layer, and the four hubs with the `@microsoft/signalr` client.
12. **Image output.** SkiaSharp resampling and JPEG encoding (quality 90) are not byte-identical to GDI+. Compare thumbnails visually.
13. **Tests and CI.** The solution has no tests. Add a regression suite, starting with the script-service JSON contract and `ConfigurationHelper`. Build in CI on .NET 10 with `TreatWarningsAsErrors` to keep the 0-warning state.
14. **`src/.nuget/`.** It holds `NuGet.Config`, `NuGet.targets` and a `packages.config` for `xunit.runners`, and no project uses them. They can be deleted, together with the solution folder entry, along with `src/packages/` (item 7).
15. **atx configuration.** Shorten it to under 4,096 characters before the next AWS Transform Custom run. This is outside the code base.
