# MixERP: .NET Framework 4.5 / 4.5.1 → .NET 10 migration

## Result

- `dotnet build MixERP.Net.sln`: **0 errors**, 37 warnings (34 × CA1416, 2 × CS3008, 1 × CS0649; all explained below).
- `dotnet build Utilities/MixERP.Net.Utility.Deployment/MixERP.Net.Utility.Deployment.csproj` (not in the main solution): **0 errors, 0 warnings**.
- The solution has no .NET test projects (the only tests are the QUnit files under `FrontEnd/MixERP.Net.FrontEnd/Tests`), so there were no unit tests to run.

## Carry-over from iteration 3

- **Why iteration 3 failed:** its post-upgrade verification hit `NETSDK1100` on `Utilities/MixERP.Net.Utility.Deployment` (a `net10.0-windows` WinForms project built on Linux). This tree sets `EnableWindowsTargeting=true` in that project. The verification sequence from `/app/perform-upgrade.sh` was reproduced exactly (per-project `dotnet build -p:WarningLevel=0 --configuration Release`, then `dotnet test --no-build` on each project, from clean `bin/obj`). Every build and test step exits 0.
- **This tree does not use iteration 3's approach.** It has no `MixERP.Net.WebForms` project, SkiaSharp, ImageSharp/`sixlabors.lic`, MailKit or Puppeteer, so the carry-over items about those don't apply. This tree runs the Web Forms app on WebFormsForCore (see below); the `System.Drawing`/CA1416 item in Next steps is the equivalent open decision.
- **Carry-over items applied:**
  - `StockAdjustmentFactory`: `validateSides` was emitted as the string `'true'`, and `'true' == true` is false in JavaScript, so the Dr/Cr side check in `FormView.js` never ran. This bug predates the migration. It is now emitted as a boolean (`JSUtility.GetVar(..., false)`). **Behaviour change:** stock adjustments with `ValidateSides=true` are now validated on the client, as originally intended.
  - Removed the legacy root `packages/` folder (368 tracked files from the old `packages.config` restore). No project, solution or config references it any more.
  - `WebClient` usages are already replaced with `HttpClient` in this tree (`PageUtility.CleanUrl`, `DownloadUpdate`, ReportManager `Client`); no `SYSLIB0014` suppressions exist.
- **Carry-over items still open** (need a person, data or an environment): PostgreSQL provisioning and connection settings, secrets (SMTP, currency API keys), the manual smoke tests, the Deployment installer redesign, the TLS settings (`SslMode.Prefer` → `VerifyFull`, `AllowedHosts`), the xUnit/CI project, and the `policy.menus` data review. Each is listed in Next steps.

## Approach

MixERP is an ASP.NET Web Forms application: 246 `.aspx/.ascx/.master/.asmx/.ashx` files and about 360 C# files built on `System.Web.UI` server controls. The modern .NET runtime has no `System.Web`, and rewriting every page and server control as Razor Pages or Blazor would rewrite the whole UI. To keep the business logic and the UI as they are, the migration uses **WebFormsForCore 1.6.8** (MIT, `WebFormsForCore.Web`, `.Web.Extensions`, `.Web.Services`). It is a port of `System.Web` (Web Forms, ASMX/ScriptServices, Forms authentication, routing, `HttpApplication`/Global.asax) to ASP.NET Core on net8.0/net10.0. The pages, code-behind, Web.config and Global.asax keep working with no source changes. ASP.NET Core hosts the app in place of IIS.

### Shared build files (new)
- `Directory.Build.props`: `LangVersion=latest`, the Npgsql version for each TFM (10.0.3 on net10.0; 8.0.9 on netstandard2.0, because Npgsql 9+ dropped netstandard2.0), and a `NoWarn` for the CLS-compliance family CS3001/CS3002/CS3003/CS3009 (reason below). It also turns off the SDK's assembly-info attributes, because every project keeps its original `Properties\AssemblyInfo.cs`; the Web.config handler registrations depend on the `Version=1.2.0.1` assembly versions in those files.
- `Directory.Build.targets`: a project opts in with `<UseSystemWeb>true</UseSystemWeb>`.
  - net10.0 gets WebFormsForCore.
  - netstandard2.0 gets `Microsoft.AspNetCore.SystemWebAdapters` 2.3.0, which type-forwards to the real System.Web when a .NET Framework app loads the library.
  - It pins `System.Security.Cryptography.Pkcs/Xml` 10.0.14 to clear the transitive advisories GHSA-2fmv-rv2c-59x7, GHSA-846f-f764-76mx and GHSA-7hqp-7c5m-4vq6.
  - It excludes the compile asset of `System.Web.Services.Description`, which comes in through CoreWCF and otherwise collides with WebFormsForCore's types (CS0433).

### Project files
- All 54 projects are SDK-style. The automated pre-conversion had left cruft, which was removed: `Configurations` copied from PetaPoco, `SolutionDir`/`RestorePackages`, `Service` items, `<Reference Include="System.Web"/>`, and SystemWebAdapters `.CoreServices`.
- Fixed ProjectReference casing that only works on Windows (`MixERP.net.Common`, `MixERP.Net.DbFactory.csproj` → `MixERP.Net.DBFactory.csproj`), plus the same paths in `MixERP.Net.sln`.
- Restored `AssemblyName=MixERP.Net.DbFactory`. The SDK would otherwise have taken `DBFactory` from the file name.
- Restored the `Properties/AssemblyInfo.cs` files the pre-conversion had deleted (Common, HtmlParser, Deployment).
- The pre-conversion had also made broken code edits, such as `this.Headers["User-Agent"].ToString() = userAgent;` in Currencylayer and ReportManager. They were reverted.
- `Microsoft.CSharp` and `System.Data.DataSetExtensions` are referenced on the netstandard2.0 leg only, since net10.0 includes them.
- Serilog `4.4.1-dev` (prerelease) was replaced with 4.4.0.
- Target frameworks:
  - **`net10.0;netstandard2.0`**: all Logic libraries (Framework, i18n, PetaPoco, Common, Entities, DbFactory, ApplicationState, Updater, Messaging.Email, HtmlParser, TransactionGovernor(.Data)), FrontEnd.Data, every `*.Data` project, FirstSteps and both CurrencyAPI plug-ins. Libraries that the pre-conversion had left on `netstandard2.0` only were brought up to the dual target.
  - **`net10.0` only**: the 11 `MixERP.Net.WebControls.*` server-control libraries and the 8 `Modules/*` projects, which are Web Forms UI libraries (see Next steps). ReportManager also stays on net10.0 only, under the NU1201 rule, because it references the ReportEngine server control.
  - **Applications**: FrontEnd is a `Microsoft.NET.Sdk.Web` Exe on `net10.0`. SqlBundler is `net10.0`. The Deployment installer is `net10.0-windows` WinForms, with `EnableWindowsTargeting` so it builds on Linux.
- Build output layout: WebFormsForCore, like classic ASP.NET, loads assemblies from `FrontEnd/MixERP.Net.FrontEnd/bin`. FrontEnd and the modules therefore keep `bin\` with `AppendTargetFrameworkToOutputPath=false`. The plug-ins (FirstSteps, Currencylayer, OpenExchangeRates) put their net10.0 build there in Debug, as before; the plug-in discovery uses `AppDomain.GetAssemblies()`.
- FrontEnd publishing: the former `FilesToIncludeForPublish=AllFilesInProjectFolder` is now an explicit Content glob, which excludes sources, project files and bin/obj. `IsTransformWebConfigDisabled=true` keeps the classic Web.config from being rewritten.

### Code changes, by component (OLD behaviour → new)
- **Npgsql 2.2 → 10 / 8**
  - `NpgsqlConnectionStringBuilder`: `UserName` → `Username`. `SyncNotification` and `SSL=true` were removed; `SslMode.Prefer` is kept. This applies to i18n ConnectionStringHelper, DbFactory DbConnection, PetaPoco Factory, FrontEnd.Data DbConnection and the Deployment DatabaseHelper.
  - `NpgsqlException.Code` (SQLSTATE) → `(ex as PostgresException)?.SqlState`. The same "P*" custom error-code mapping to `DbErrors` resources is kept in PetaPoco Factory and DbOperation, and the catch types are unchanged.
  - `NpgsqlError` notices → `PostgresNotice` (`Message` → `MessageText`) in DbOperation and Finance.Data EODOperation.
  - `ConstraintName` is read from `PostgresException` in ScrudFactory.Data FormHelper.
- **PetaPoco**: `DbProviderFactories.GetFactory("Npgsql")` needs registration on .NET, so MixERP's `Factory` now passes `NpgsqlFactory.Instance` (same provider). On netstandard2.0, the provider-name constructor throws `PlatformNotSupportedException` because that registry does not exist there. Reflection.Emit packages were added for netstandard2.0.
- **SignalR 2 / OWIN → ASP.NET Core SignalR** (UpdateHub, BaseHub, BackOffice DbHub, Finance DayOperationHub)
  - Dynamic `Clients.X.method(...)` calls → `Clients.X.SendAsync("method", ...)`. Both are fire-and-forget.
  - `AppUsers.GetCurrentUserDB()`, which reads `HttpContext.Current`, was replaced inside the hubs with `AppUsers.GetCurrent(<Context.User.Identity.Name>)`.
  - `Hubs/Startup.cs` (OwinStartup + `MapSignalR`) was removed. `Program.cs` discovers every `Hub` in `bin\MixERP.Net.*.dll`, as SignalR 2 did, and maps it to `/hubs/{hubName}`.
  - New `Application/FormsTicketAuthenticationHandler.cs` decrypts the Forms authentication cookie, so `Context.User.Identity.Name` is still the global login id.
  - Client side: `/signalr/hubs` was replaced by `Scripts/microsoft-signalr/signalr.min.js` (@microsoft/signalr 10.0.11) plus `Scripts/mixerp/signalr-hubs.js`. That script keeps the `$.connection.<hub>.client/server` and `$.connection.hub.start()` surface the existing page scripts use (Master pages, Update.aspx, EODOperation.ascx, DatabaseBackup.ascx.js).
- **Hosting**
  - New `Program.cs` sets up SignalR and the hubs, then hands everything else to `UseWebForms(UseIntegratedMode().HandleAllRequestsWithWebForms())`. All requests go to Web Forms because of the extensionless page routes in `Application/Routes.cs` and the Web.config authorization rules.
  - New `Application/RequestFiltering.cs` rebuilds the IIS request-filtering defaults that Kestrel lacks: hidden `bin/obj/App_*` segments, and `.config/.cs/.ascx/.master/.snk/.sql/...` plus `appsettings*.json` return 404. Without it, Kestrel could serve `Resource/Configs/DbServer.config`, which holds the DB password.
  - New `appsettings.json` keeps the 1 GB upload limit from `requestLimits maxAllowedContentLength` (Kestrel `MaxRequestBodySize`).
  - Web.config and Global.asax are intentionally **kept**. WebFormsForCore reads `ConfigurationManager.AppSettings`, `<authentication>`, `<sessionState>` and handlers from them exactly as before, so moving them to appsettings.json would break every `ConfigurationManager` caller.
- **Logging**: Serilog 1.5 `WriteTo.RollingFile(path)` → Serilog.Sinks.File `WriteTo.File(path, rollingInterval: Day)`.
- **Updater**: MarkdownDeep (.NET Framework only) → Markdig.Signed with `UseAdvancedExtensions()`, which is equivalent to MarkdownDeep's ExtraMode. Raw HTML is still allowed. The signed package keeps the strong-named netstandard2.0 build loadable on .NET Framework.
- **Common**
  - `FileSystemHelper.IsDirectoryWritable` used a Code Access Security check, which always returned true under full trust. It now probes the directory with a temporary file, and still returns true when the directory does not exist. **Behaviour change**: the attachment page now shows a "write protected" warning when the directory really is read-only.
  - `ExpressionHelper.cs` was **deleted**. It was unused (no callers anywhere) and compiled C# with CodeDom against System.Windows.Forms, which .NET does not support.
  - The SHA512 password hash now uses `SHA512.Create()` instead of `SHA512CryptoServiceProvider`. The output is identical.
  - `WebClient` → `HttpClient` in `PageUtility.CleanUrl` (HEAD probe), ReportManager `Client.GetContent` and Updater `DownloadUpdate`. `DownloadUpdate` keeps the same progress and completion messages, and errors are still rethrown.
- **HtmlParser** (**behaviour change**): `HtmlRendererImageSerializer` rasterized the transaction-checklist e-mail HTML to an image with HtmlRenderer.WinForms (GDI+, STA thread). That package ships only for Windows desktop TFMs, so the serializer now saves the HTML as an `.html` attachment. `WebBrowserImageSerializer` (IE ActiveX control, unused) was deleted.
- **CAS attributes**: `[AspNetHostingPermission]`, `[PermissionSet]` and `[SecurityPermission]` were removed; .NET ignores them (SYSLIB0003). The `MixERPException` serialization constructor and `GetObjectData` are now netstandard2.0-only (SYSLIB0051).
- **netstandard2.0 leg**: the Web Forms types (`System.Web.UI`) do not exist there. The Framework `Controls/*` base classes and the Common UI helpers are excluded by conditional `Compile Remove`: DropDownList, GridView, HtmlControl, JSUtility and jQueryUI. The UI members of `PageUtility` and `Conversion` (`ResolveUrl`, `TryCastUnit`) sit behind `#if !NETSTANDARD2_0`. `DirectoryHelper.CopyDirectory` has a managed fallback in place of `Microsoft.VisualBasic.FileIO`.
- **Deployment installer**: `Certificate.cs` was never part of the original project. It stays on disk but is excluded from compilation, as before.
- Removed an unused label (CS0164) in `ApplicationError.cs`.

## Next steps

- **Runtime verification (highest priority)**: the build is clean, but the app has not been run (per instructions). Smoke-test the following:
  - sign-in (Forms authentication and session state on WebFormsForCore);
  - extensionless routes (`/Modules/...`, `/Reports/...`);
  - ASMX/ScriptService JSON calls;
  - `FileUploadHanlder.ashx`, `Resources.js` and `ScriptResource.axd` handlers. Their Web.config types say `System.Web.Extensions, Version=3.5/4.5`; check that they resolve to WebFormsForCore;
  - the SignalR features: EOD, database backup and update.
- **Forms ticket in hubs**: `FormsTicketAuthenticationHandler` assumes WebFormsForCore has initialized `FormsAuthentication` and the machine key before the first hub call. If decryption fails, add an explicit `<machineKey>` to Web.config.
- **NU1201 / UI-library target removals (netstandard2.0 not added)**: these have no netstandard2.0 implementation of System.Web.UI.
  - The 11 `MixERP.Net.WebControls.*` server-control projects and the 8 `Modules/*` projects. The modules also reference the `MixERP.Net.FrontEnd` web application.
  - `MixERP.Net.ReportManager`, because it references `MixERP.Net.WebControls.ReportEngine` (net10.0 only).
  - Revisit if the UI is rewritten (Razor Pages/Blazor, KB 25/26).
- **Publish layout**: `dotnet publish` puts the assemblies in the publish root and does not include the module and plug-in assemblies, but WebFormsForCore expects them in `bin\`. Until a publish target copies the assemblies into `bin\`, deploy from the build output folder.
- **Hosting on IIS**: the classic Web.config is application configuration now, so it cannot also carry the `aspNetCore` handler for the IIS ASP.NET Core module. Prefer Kestrel behind a reverse proxy or a Linux container (KB 19). The Deployment installer still configures a classic IIS site and needs updating.
- **CA1416 (34 warnings)**: System.Drawing is Windows-only at run time. It is used in `ImageHelper`, `Conversion.TryCastByteArray/ConvertImageToByteArray` and the BackOffice `ImageHandler.ashx` thumbnails. Running on Linux needs a decision between SkiaSharp (MIT, native assets) and ImageSharp (Six Labors license). See KB 21.
- **HTML instead of image in checklist e-mails** (see above): this needs a product decision on a cross-platform HTML→image renderer (for example headless Chromium) if an image is required.
- **Npgsql 2 → 10 behaviour**: Npgsql 6+ is stricter about `timestamp` vs `timestamptz` and maps `DateTime.Kind` differently. Error `Message` text now has the form `SQLSTATE: text`. Regression-test the date-heavy transaction screens. `SyncNotification` no longer exists; notices still arrive through `Notice` events.
- **Bundles**: `bundles/scripts/master-page.js` still contains the old `jquery.signalR-2.2.0` client. It is harmless, because `signalr-hubs.js` replaces `$.connection`, but remove it from the `.bundle` definitions and regenerate the bundles.
- **Build race**: modules and plug-ins copy into the shared `FrontEnd/bin`. Parallel builds occasionally log MSB3026 copy retries. If it ever escalates to MSB3027, build with `-m:1` or set `Private=false` on references FrontEnd already copies.
- **Remaining warnings left in place**:
  - CS3008: `_downloadUrl` and `_release` in `Update.aspx.cs` are referenced by name from `Update.aspx` markup, so renaming them affects the page.
  - CS0649: `MixERPWebpage.menus` is never assigned; this predates the migration. It looks like a latent null reference in the menu rendering and needs an owner decision.
- **CLS NoWarn**: CS3001/3002/3003/3009 are suppressed globally. They appear because WebFormsForCore's System.Web and Npgsql are not marked `[CLSCompliant]`; on .NET Framework, System.Web was.
- The untracked `../.nuget/NuGet.Config` was created by the conversion tooling and was left untouched.
- **Provisioning and secrets (from iteration 3)**: provide a PostgreSQL server, set it in `Resource/Configs/DbServer.config` (still read through `ConfigurationManager`), and run the `db/` SQL bundles. Keep the DB password, SMTP credentials and currency API keys out of source control, for example by generating `DbServer.config` at deploy time from environment variables.
- **TLS (from iteration 3)**: the connection builders still use `SslMode.Prefer`, as the original did. `VerifyFull` needs a trusted server certificate and is an environment decision. Narrow `AllowedHosts` in `appsettings.json` for production.
- **Tests and CI (from iteration 3)**: no .NET test project exists. Add an xUnit project for the data layer (PetaPoco `Factory`, `DbOperation` error mapping) and ScrudFactory, and a CI build of `dotnet build -c Release`.
- **`policy.menus` data (from iteration 3)**: that review referred to iteration 3's tree; re-check the menu rows against the page paths when smoke-testing navigation.
- **MSB3026**: a clean parallel build logs a few copy retries into the shared `FrontEnd/bin` (see "Build race" above). They are retries, not failures.
