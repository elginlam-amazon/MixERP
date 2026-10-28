# MixERP .NET Framework 4.5.1 → .NET 10 migration summary

## Result

- All 54 requested projects build with `dotnet build` (exit code 0, 0 errors). `MixERP.Net.sln` builds with 0 errors and 0 warnings on a clean build, except pre-existing CLS-compliance warnings in `MixERP.Net.Entities` (PetaPoco types) and one pre-existing CS0649.
- New test project `Tests/MixERP.Net.Common.Tests` (xUnit, 8 tests) covers the helpers whose implementation changed. All tests pass (`dotnet test MixERP.Net.sln`).
- I did not start the application. Runtime behaviour of the Web Forms pages is not verified (see Next steps).

## Approach

- **Web Forms:** a Razor or Blazor rewrite of about 250 `.aspx`/`.ascx` pages and roughly 150 server-control classes was out of scope for one cycle. The existing code-behind and server controls now compile against **CoreWebForms 0.5.0** (MIT, `System.Web.UI` ported to ASP.NET Core) on top of **Microsoft.AspNetCore.SystemWebAdapters 2.3.0**. The business logic is unchanged.
  - I rejected **CoreForms.Web** because it is under a trial-only licence.
- **Target frameworks:**
  - Libraries: `net10.0;netstandard2.0`. Libraries that were single-target `netstandard2.0` were completed to this pair.
  - Applications: `net10.0`. This covers `MixERP.Net.FrontEnd` (Web SDK), SqlBundler, and the core module UI projects (`MixERP.Net.Core.Modules.*`), which reference the web app.
  - Deployment installer: `net10.0-windows` with `EnableWindowsTargeting`.
- **Web Forms and netstandard2.0:** CoreWebForms targets net9.0+, so no `System.Web.UI` exists on netstandard2.0. In libraries that contain Web Forms code, CoreWebForms is referenced only for `net10.0`. The Web Forms source files are removed from the netstandard2.0 compile with conditional `<Compile Remove>`, and mixed files use `#if NET`. The netstandard2.0 builds therefore expose only the non-UI helpers.
- **Packages:**
  - Npgsql 2.2.3 → 10.0.3 (net10.0) and 8.0.8 (netstandard2.0).
  - Serilog prerelease → 4.4.0, plus Serilog.Sinks.File 7.0.0.
  - MarkdownDeep → Markdig.Signed 1.5.0.
  - `System.Security.Cryptography.Pkcs` pinned to 10.0.14 to fix the NU1902/NU1903 advisories that PDFsharp pulled in.
  - Removed: unused `SystemWebAdapters.CoreServices` (from libraries), Microsoft.VisualBasic, and redundant Microsoft.CSharp.
- **Linux builds:** fixed path casing in ProjectReferences and the `.sln` (e.g. `MixERP.net.Common`, `DbFactory`). Added `GenerateAssemblyInfo=false` where `AssemblyInfo.cs` is kept.

## Code changes worth noting

- **Npgsql API changes:**
  - `UserName` → `Username`.
  - Removed the `SyncNotification` and `SSL` options; `SslMode.Prefer` is kept.
  - `NpgsqlException.Code` → `PostgresException.SqlState`, `ConstraintName` taken from `PostgresException`, `NpgsqlError` → `PostgresNotice`.
  - PetaPoco `Factory` now passes `NpgsqlFactory.Instance` instead of relying on a `DbProviderFactories` registration.
- **ASMX script services (36 services):** ASMX does not exist on .NET 10. I added `MixERP.Net.Framework.Services` (`WebService` base class plus `ScriptService`/`WebMethod`/`ScriptMethod` attributes) and a host dispatcher, `FrontEnd/Application/ScriptServiceEndpoints.cs`. The dispatcher maps every `*.asmx` file's `Class` to the route `{path}.asmx/{method}`.
  - The wire contract is kept: a JSON body returns `{"d": ...}` with MS-AJAX dates, and form or query calls return XML.
  - Errors come back as HTTP 500 with `{Message, ExceptionType}`. Stack traces are no longer exposed.
- **`JavaScriptSerializer`:** replaced by `JSONHelper.DeserializeDynamic`, which returns the same shape (`object[]` / `Dictionary<string, object>` / primitives) so callers are unchanged, and by `JsonConvert.SerializeObject`.
- **Authentication:** Forms authentication → ASP.NET Core cookie authentication (`Application/FormsAuthentication.cs`), with the same login URL, 60-minute sliding expiration, and the global login id as the identity name.
- **Startup:** `Global.asax` logic now runs through `AddHttpApplication<Global>()`. `Program.cs` now handles:
  - session, routing, and the handlers previously registered in `Web.config`;
  - the SignalR hubs;
  - static files, with an allow-list of asset types so binaries and config files are never served.
- **Module assemblies:** the modules are still built into the site bin folder, and `ModuleLoader` loads them at startup.
- **SignalR 2 → ASP.NET Core SignalR:** dynamic client calls are now `SendAsync("name", ...)`. Hubs are mapped at `/signalr/{hubName}`. The OWIN `Startup` class was removed.
- **Configuration:**
  - The `Web.config` appSettings moved to `App.config` (read by `ConfigurationManager` as `MixERP.Net.FrontEnd.dll.config`). `App.Release.config` replaces the `Web.Release.config` transform.
  - `appsettings*.json` now holds hosting and logging settings.
  - `Web.config` now contains only IIS request limits and MIME settings.
- **Imaging:**
  - System.Drawing → SkiaSharp for thumbnails (`ImageHelper`, `ImageHandler.ashx`). The output is still JPEG at quality 75.
  - The WinForms HtmlRenderer and IE WebBrowser serializers cannot run in a .NET 10 library. The checklist email snapshot is now rendered with HtmlRenderer.PdfSharp and **attached as a PDF instead of an image**. The `EmailImageExtension` parameter is no longer used.
- **Partial postbacks:** ASP.NET AJAX `ScriptManager`/`UpdatePanel` are not available. ScrudForm uses a `Panel` with the same id, so its commands now do **full postbacks**.
- **Server.Transfer, Server.Execute and Server.TransferRequest:** these are not supported on ASP.NET Core, so they are replaced with `Response.Redirect` to the same pages. The browser URL now changes when this happens.
- **Smaller changes:**
  - Code Access Security attributes removed.
  - `FileIOPermission` check → real write probe.
  - VB `FileSystem.CopyDirectory` → recursive copy.
  - `SHA512CryptoServiceProvider` → `SHA512.Create()`; the hash output is identical, and a test covers it.
  - `WebClient` → `HttpClient`.
  - Dead CodeDOM `ExpressionHelper` removed: it was unused, does not work on .NET, and was a code-injection risk.

## Next steps

- **Web Forms runtime (blocking for production):** CoreWebForms 0.5.0 compiles the code-behind but does not compile `.aspx`/`.ascx` markup at runtime. Its page compiler is not published on NuGet, so pages will not render yet. Choose one of:
  - adopt CoreWebForms' build-time page compilation from source;
  - plan the KB-recommended per-page rewrite to Razor Pages or Blazor (KB 25/26).
- **Session state:** `AddWrappedAspNetCoreSession` serializes values to JSON. The keys `Challenge`, `UserName`, `FirstStepsPending` and `InvalidPasswordAttempts` are registered. `Session["ex"]` (an Exception) and `Session["Product"]` (a model) are not serializable as-is and need a decision: store an id or message, or register custom serializers.
- **Client JavaScript:**
  - Replace the SignalR 2 jQuery client (`$.connection.*`, `/signalr/hubs`) with `@microsoft/signalr` connecting to `/signalr/{hubName}`.
  - Verify that `getAjaxErrorMessage` handles the new error JSON.
- **Netstandard2.0 surface:** the netstandard2.0 builds of the server-control libraries contain only non-UI helpers. Confirm whether any .NET Framework consumer really needs them. If none does, consider making those libraries `net10.0`-only (needs a human decision).
- **Checklist emails:** confirm that a PDF attachment is acceptable for checklist emails. PDFsharp needs fonts; Windows fonts are used on Windows, but Linux hosts need a font resolver.
- **Configuration and logging:**
  - Secrets still live in `Resource/Configs/*.config` (e.g. DbServer.config). Consider environment variables or user-secrets.
  - Review `IsIDE`/`DisplayErrorDetails` per environment.
  - The Serilog file-name pattern changed slightly (`RollingFile` → `File` with daily rolling).
- **Authorization:** the cookie and session settings mirror the old `Web.config`. Authorization is still enforced by the pages and services themselves, as before; there is no global `[Authorize]`. The script-service endpoints are unauthenticated at the transport level, exactly like the old ASMX services, which rely on `AppUsers.GetCurrent()`.
- **Database:** Npgsql 10 uses `timestamp with time zone` semantics for `DateTime` (UTC kind required). Run the database-backed flows against a real PostgreSQL instance.
- **Cleanup:** the module `Web.config`/`Web.*.config` files under `Modules/*` are now unused leftovers of the old web-project type and are excluded from publish.
- **Tests:** there are no automated tests for the script-service dispatcher. Add integration tests with `WebApplicationFactory` once pages can render.
