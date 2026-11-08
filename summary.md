# MixERP — .NET Framework 4.5–4.8 → net10.0 migration

## Result

| | |
|---|---|
| Projects migrated | 54 (all `.csproj` in the tree) |
| Target framework | `net10.0` (one exception, below) |
| Project format | SDK-style, all 54 |
| `dotnet build MixERP.Net.sln --no-incremental` | exit 0, **0 errors, 0 warnings** |
| `dotnet build <each project> --no-incremental` | exit 0, 0 errors, 0 warnings — 54/54 |
| `dotnet restore --force --no-cache` | exit 0, no `NU*` diagnostics |

`MixERP.Net.Utility.Deployment` targets `net10.0-windows`: it is a Windows Forms front end over
`Microsoft.Web.Administration` whose whole job is to create an IIS site and application pool.
`EnableWindowsTargeting=true` lets it build on Linux; it still only runs on Windows.

The application was **not executed** as part of this work. Everything below is verified by the
compiler only.

### Iteration 20

* AWS Transform Custom (atx) failed before changing any code (`clientMetadata.configuration`
  exceeded the 4096-character limit), so the working tree started at the original commit `bb756c50`.
* The source tree from branch `main-upgrade-itr-18` (the last iteration whose post-upgrade
  verification passed) was applied as the starting point instead of redoing the migration.
  The migrated `Conversion.HashSha512Hex` was checked against the pre-upgrade code in CodeKB.
  It still produces the same digest: lowercase hex SHA-512 of UTF-8(`password + salt`).
* Deleted 9 obsolete `packages.config` files left in `FrontEnd/MixERP.Net.FrontEnd` and its 8 UI
  modules. SDK-style projects ignore them. Every package they listed is either a
  `PackageReference` now (Newtonsoft.Json 13.0.4, Serilog 4.4.0) or was replaced on purpose
  (SignalR 2 / OWIN → ASP.NET Core SignalR, jQuery content package → static files already in the tree).
* Re-verified: `dotnet build MixERP.Net.sln --no-incremental` and a per-project build of all 54
  projects both exit 0 with 0 errors and 0 warnings.
* `src/.nuget/packages.config` (solution-level `xunit.runners` 1.9.2 for the old NuGet.targets restore)
  is not referenced by any project. It was left in place and is listed in Next steps.

---

## 1. Project and build infrastructure

* All 54 projects rewritten from `ToolsVersion="4.0"/"12.0"` MSBuild to SDK-style
  (`Microsoft.NET.Sdk`, `Microsoft.NET.Sdk.Web` for the front end).
* `src/Directory.Build.props` added: `GenerateAssemblyInfo=false` (the tree keeps its hand-written
  `AssemblyInfo.cs` files, which would otherwise collide with the generated attributes — CS0579),
  `LangVersion=latest`, `Nullable=disable`, `ImplicitUsings=disable`,
  `AutoGenerateBindingRedirects=false`, `SignAssembly=false`, `NoWarn=CS1591`.
* 30 `packages.config` files deleted; every dependency is a `PackageReference`.
* 59 `<ProjectReference>` paths and 3 `.sln` project paths repaired for case — MSBuild resolves
  paths case-sensitively on Linux, and the tree contains both `MixERP.Net.*` and `MixERP.net.*`
  spellings (there is even a real `FrontEnd/MixERP.net.FrontEnd/` directory holding one static
  resource, which is why the bad `.sln` entry resolved against the wrong folder).
* `MixERP.Net.Utility.Deployment` was missing from `MixERP.Net.sln` entirely; it is now a solution
  member, so the solution build covers all 54 projects.
* GAC/`<Reference>` entries for `System.Web`, `System.Web.Extensions`, `System.Configuration`,
  `System.Drawing`, `Microsoft.CSharp`, `System.Data.DataSetExtensions` and friends removed and
  replaced by packages or by the shared framework.

### Packages after migration

| Package | Version | Replaces |
|---|---|---|
| `Microsoft.AspNetCore.SystemWebAdapters` (+ `.CoreServices`) | 2.3.0 | `System.Web` (context, session, caching, hosting, `HttpUtility`, `IHttpHandler`) |
| `Npgsql` | 10.0.3 | `Npgsql` 2.x |
| `SkiaSharp` + `SkiaSharp.NativeAssets.Linux` | 4.150.1 | `System.Drawing` / `System.Drawing.Common` (Windows-only) |
| `Markdig` | 0.44.0 | `MarkdownDeep.NET-Signed` (net35 only — NU1202) |
| `Serilog` | 4.4.0 | `Serilog` 1.x |
| `Serilog.Sinks.File` | 7.0.0 | `Serilog.Sinks.RollingFile` (retired) |
| `Newtonsoft.Json` | 13.0.4 | 6.x |
| `System.Configuration.ConfigurationManager` | 10.0.14 | `System.Configuration` |
| `System.Runtime.Caching` | 10.0.14 | `System.Runtime.Caching` (GAC) |
| `Microsoft.AspNet.WebApi.Client` | 6.0.0 | same, retargeted |
| `Microsoft.Web.Administration` | 11.1.0 | same, retargeted |
| ASP.NET Core SignalR | shared framework (`<FrameworkReference Include="Microsoft.AspNetCore.App" />`) | `Microsoft.AspNet.SignalR` 2 + `Microsoft.Owin.*` |

`Microsoft.CSharp` was dropped from 44 projects (NU1510 — provided by the framework) and the
deprecated `System.Data.DataSetExtensions` package from 53 (its types are in the shared framework
now). Serilog was moved off a `-dev` prerelease onto stable 4.4.0.

---

## 2. Configuration: `Web.config` → `appsettings.json`, `Global.asax` → `Program.cs`

`FrontEnd/MixERP.Net.FrontEnd/appsettings.json` is new and carries everything the old
`Web.config` declared:

| `Web.config` | `appsettings.json` |
|---|---|
| `<appSettings>` (14 keys) | `AppSettings:*` |
| `<globalization culture="auto" uiCulture="auto" />` | `Globalization:Culture` / `:UICulture` |
| `<sessionState mode="InProc" cookieless="UseCookies" timeout="60" />` | `SessionState:*` |
| `<authentication mode="Forms">` (login page, default url, 60 min sliding) | `Authentication:*` |
| `<httpRuntime maxRequestLength>` / `<requestLimits maxAllowedContentLength>` | `RequestLimits:MaxRequestBodyBytes` |
| `<location>` anonymous paths | `AnonymousPaths` |
| `<staticContent><mimeMap fileExtension=".backup" />` | `StaticFiles:AdditionalContentTypes` |

`MixERP.Net.Framework.AppSettings` is a new single read path: the host calls
`AppSettings.UseProvider(key => configuration["AppSettings:" + key])` once, and every library keeps
reading settings by key without taking an `IConfiguration` dependency. It falls back to
`ConfigurationManager.AppSettings` so the class libraries still work outside the web host (the
deployment utility, SqlBundler).

`FrontEnd/MixERP.Net.FrontEnd/Program.cs` is new and replaces `Global.asax` / `Global.asax.cs`:

* `Application_Start` → straight-line startup before `app.RunAsync()`:
  `LogManager.IntializeLogger()`, `await UpdateManager.CheckForUpdates()`,
  `GlobalLogin.CreateTable()`, `Repository.DownloadAndInstallReports()`.
* `Application_Error` + `Server.GetLastError()` → `app.UseExceptionHandler` reading
  `IExceptionHandlerFeature.Error` and handing it to `ApplicationError.Handle`.
* Forms authentication → cookie authentication with the same login path, `ReturnUrl` parameter,
  expiry and sliding behaviour.
* Session → `AddDistributedMemoryCache` + `AddSession` + `AddSystemWebAdapters()
  .AddWrappedAspNetCoreSession()`, so `HttpContext.Current.Session` keeps working.
* `maxAllowedContentLength` → `options.Limits.MaxRequestBodySize` on Kestrel.
* `<handlers><add name="Resources" path="Resources.js" .../>` →
  `app.MapHttpHandler<ResourcesHandler>("/Resources.js")`.

The 11 `Web.config` files are left in the tree unreferenced, as a record for the pages that still
have to be re-authored.

---

## 3. SignalR 2 → ASP.NET Core SignalR

All four hubs were really migrated (knowledge base doc 32), not excluded:

| Hub | Route | Assembly |
|---|---|---|
| `BaseHub` | `/hubs/base` | `MixERP.Net.FrontEnd` |
| `UpdateHub` | `/hubs/update` | `MixERP.Net.FrontEnd` |
| `DayOperationHub` | `/hubs/dayoperation` | `MixERP.Net.Core.Modules.Finance` |
| `DbHub` | `/hubs/db` | `MixERP.Net.Core.Modules.BackOffice` |

* `Microsoft.AspNet.SignalR` + the OWIN `Startup` class are gone; `Hubs/Startup.cs` was kept as
  `Hubs/Startup.Owin.cs` (excluded from the compile) for reference.
* There is no dynamic client proxy: every `Clients.X.method(args)` became
  `await Clients.X.SendAsync("method", args…)`, and the hub methods became `async Task`.
* Handlers attached to void-returning delegates and events cannot become asynchronous
  (`UpdateInstaller.UpdateProgressHandler`, the Npgsql notice event, `Process.Exited`). Those call
  `.GetAwaiter().GetResult()`, which is safe here: ASP.NET Core has no synchronisation context, so
  the deadlock that made this illegal under `System.Web` cannot happen. Each site carries a remark
  saying so.
* The module hubs live in assemblies that reference the host, so the host cannot name them in a
  `MapHub<T>()` call. SignalR 2 solved the same problem by scanning assemblies from
  `MapSignalR()`; `Program.MapModuleHubs` does the equivalent — it probes
  `MixERP.Net.Core.Modules.*.dll` next to the host, finds every public concrete `Hub` and maps it
  to `/hubs/<name-without-Hub>`. **Deployment must keep placing the module assemblies beside the
  host**, which is what the old `<OutputPath>..\..\bin\</OutputPath>` did.

---

## 4. Behavioural changes worth reviewing

These are places where the migrated code does something observably different. None of them is a
compile problem; all of them are judgement calls that a reviewer should sign off on.

1. **Image pipeline: System.Drawing → SkiaSharp.** `System.Drawing.Common` is Windows-only on
   modern .NET (doc 21). `Conversion.ConvertImageToByteArray`, `Conversion.TryCastByteArray`,
   `ImageHelper.GetResizedImage/GetImageFormat/GetFileExtension` and
   `BackOffice/Handlers/ImageHandler.ashx.cs` now use `SKBitmap`/`SKCanvas`. Resampling quality and
   JPEG encoder output will not be byte-identical to GDI+. `SKBitmap.Decode` **returns null**
   where `Image.FromFile` threw, so `IsValidBitmap` tests for null instead of catching.
2. **`Server.TransferRequest` → redirect.** `MixERPException.Handle` could not keep
   `Server.TransferRequest` (SYSWEB0001 — unsupported on ASP.NET Core). It now issues
   `Response.Redirect("~/Site/RuntimeError.aspx", false)`. The exception object still travels in
   session, but the browser URL changes and the original form post is not preserved.
3. **Exception serialization removed.** `MixERPException` lost its
   `(SerializationInfo, StreamingContext)` constructor, its `GetObjectData` override and the
   `[SecurityPermission]` demand on it (SYSLIB0051 / SYSLIB0003 / CS0672, doc 28). BinaryFormatter
   does not exist on modern .NET, so that scaffolding was inert. Anything that round-tripped a
   `MixERPException` through a binary formatter must move to JSON.
4. **Code Access Security removed.** The `FileIOPermission` demand that guarded the writable-path
   check was replaced by an actual write probe; CAS is not honoured by the runtime.
5. **Browser capabilities → user agent string.** `HttpBrowserCapabilities.Browsers` no longer
   exists; `PageUtility.GetBrowser()` logs the raw `User-Agent` instead. Log output changes shape.
6. **Client IP.** The old code read the `HTTP_X_FORWARDED_FOR` / `REMOTE_ADDR` IIS server
   variables off the `Page`. `GetUserIpAddress()` now reads the `X-Forwarded-For` header and falls
   back to `UserHostAddress`. Behind a proxy this needs `UseForwardedHeaders` configured with the
   known-proxy list, otherwise `UserHostAddress` is the proxy.
7. **`Page.ResolveUrl` → `Request.ApplicationPath`.** `PageUtility.ResolveUrl` resolves `~/` by
   hand against the current request's application path, defaulting to `/`.
8. **`HttpApplicationState` → static state.** There is no `HttpApplication`, so the
   `Application["UpdateAvailable"]` slot became a static `UpdateManager.UpdateAvailable`. Process
   lifetime now bounds it instead of application lifetime, which is the same thing in practice but
   does not survive a restart.
9. **`WebClient` → `HttpClient`** (SYSLIB0014) in three places:
   * `PageUtility.CleanUrl` — a `WebClient` subclass that rewrote GET to HEAD became a plain
     `HttpRequestMessage(HttpMethod.Head, …)` on a shared `HttpClient`. Failure is now
     `HttpRequestException` rather than `WebException`.
   * `ReportManager.Client.GetContent()` — `GetStringAsync(...).GetAwaiter().GetResult()`.
   * `Updater/Installation/Tasks/DownloadUpdate` — rewritten to stream the response with
     `HttpCompletionOption.ResponseHeadersRead` and report progress from the copy loop, because
     `HttpClient` has no `DownloadProgressChanged`. **This also fixes a latent bug**: the old code
     returned the download task from inside the `using` block, disposing the `WebClient` while the
     download was still in flight. Progress is now only reported when the server sends
     `Content-Length`.
10. **`SHA512CryptoServiceProvider` → `SHA512.HashData`** (SYSLIB0021). Same digest, so stored
    password hashes remain valid.
11. **Rolling log file.** `WriteTo.RollingFile(...)` was retired; it is now
    `WriteTo.File(path, rollingInterval: RollingInterval.Day)`. File naming changes
    (`log-20260101.txt` style) and the old size-based roll is gone.
12. **Markdown rendering.** `MarkdownDeep` is net35-only; `Markdig` replaces it. Generated HTML
    will differ in details (attribute order, smart quotes, table handling).
13. **Npgsql 2 → 10.** `NpgsqlConnectionStringBuilder.UserName` → `Username`;
    `NpgsqlError` → `PostgresNotice` (notice text is `MessageText`); `SqlState` and
    `ConstraintName` moved onto `PostgresException`, so `catch (NpgsqlException)` became
    `catch (PostgresException)` where the constraint name is read.
14. **Runtime C# evaluation is disabled.** `ExpressionHelper.Eval` used
    `System.CodeDom.Compiler.CSharpCodeProvider`, which shelled out to `csc.exe`. The public API is
    preserved but throws `PlatformNotSupportedException`; the original is kept as
    `ExpressionHelper.CodeDom.cs` for a Roslyn port.
15. **HTML-to-image serializers are disabled.** `WebBrowserImageSerializer` (Windows Forms
    `WebBrowser` control) and `HtmlRendererImageSerializer` (`TheArtOfDev.HtmlRenderer`, GDI+)
    throw `PlatformNotSupportedException`; the originals are kept as `*.WinForms.cs`.
16. **Web Forms page routes dropped.** `Application/Routes.cs` used `RouteCollection.MapPageRoute`,
    which has no counterpart. For restoration later, it declared:
    `routes.Ignore("{resource}.axd")`; `DefaultRoute` `""` → `~/SignIn.aspx`;
    `Reporting` `Reports/{path}` → `~/Reports/ReportMaster.aspx`;
    `Modules` `Modules/{*path}` → `~/Modules/Default.aspx`.
17. **SignalR wire protocol changed** (see §3). Every existing JavaScript client is incompatible:
    `jquery.signalR` and the `/signalr/hubs` generated proxy must be replaced by
    `@microsoft/signalr`, and each hub now has its own route.
18. **`DbHub` database backup is still Windows-only.** It drives `pg_dump.exe` through a generated
    `.bat` file. The code compiles and runs on any platform, but the backup will fail on Linux
    until the process invocation is made cross-platform.
19. **`Certificate.cs` deleted** from the deployment utility. It was ~370 lines of CertEnroll COM
    interop behind `SecureString`/`Marshal` plumbing, and nothing in the solution referenced it —
    dead code in the original as well. If self-signed certificate generation is wanted back, use
    `System.Security.Cryptography.X509Certificates.CertificateRequest`.
20. **CLS compliance.** The assemblies keep `[assembly: CLSCompliant(true)]`. Members that
    unavoidably expose `Npgsql`, `SkiaSharp` or `System.Web` types are now marked
    `[CLSCompliant(false)]` individually (`IHtmlImageSerializer` is marked at interface level,
    because a compliant interface may not carry a non-compliant member — CS3010). The vendored
    `PetaPoco` project gained `[assembly: CLSCompliant(true)]`, which removed ~290 cascading
    CS3001/CS3002/CS3009 warnings at the root rather than papering over them in every consumer.

---

## 5. Presentation layer: what is **not** compiled

`System.Web.UI` (Web Forms), `System.Web.Services` (ASMX), `System.Web.Script`,
`System.Web.Handlers` and `System.Web.Routing` were never ported to modern .NET, and there is no
mechanical translation from a server control or an `.aspx` code-behind to anything that runs on
net10.0 (docs 25, 26, 33). Rather than delete this code or stub it out, **603 source files are kept
in the tree, shipped as `<None>` content, and excluded from the compile** with `<Compile Remove>`,
each project carrying a comment that says why. Nothing was silently dropped.

| Project | Files excluded |
|---|---|
| `MixERP.Net.Core.Modules.BackOffice` | 96 |
| `MixERP.Net.Core.Modules.Sales` | 83 |
| `MixERP.Net.Core.Modules.Inventory` | 69 |
| `MixERP.Net.Core.Modules.Finance` | 61 |
| `MixERP.Net.FrontEnd` | 53 |
| `MixERP.Net.WebControls.StockTransactionFactory` | 43 |
| `MixERP.Net.WebControls.ScrudFactory` | 37 |
| `MixERP.Net.Core.Modules.Purchase` | 36 |
| `MixERP.Net.WebControls.StockTransactionViewFactory` | 28 |
| `MixERP.Net.WebControls.TransactionViewFactory` | 13 |
| `MixERP.Net.Core.Modules.CRM` | 12 |
| `MixERP.Net.WebControls.TransactionChecklist` | 12 |
| `MixERP.Net.WebControls.AttachmentFactory` | 11 |
| `MixERP.Net.WebControls.ReportEngine` | 9 |
| `MixERP.Net.Common` | 8 |
| `MixERP.Net.WebControls.StockAdjustmentFactory` | 8 |
| `MixERP.Net.WebControls.Flag` | 5 |
| `MixERP.Net.WebControls.PartyControl` | 5 |
| `MixERP.Net.WebControls.Common` | 4 |
| `MixERP.Net.Core.Modules.POS` | 3 |
| `MixERP.Net.Framework` | 3 |
| `MixERP.Net.Core.Modules.Manufacturing` | 2 |
| `MixERP.Net.HtmlParser` | 2 |

Markup still in the tree and not served by the host: **194 `.ascx`**, **16 `.aspx`**,
**35 `.asmx`**, **4 `.Master`**. The one `.ashx` handler (`ImageHandler`) *was* migrated and is
compiled, because `IHttpHandler` is supported by the System.Web adapters.

The exclusion lists were produced compiler-first, not by guesswork: a seed list from grepping for
the dead namespaces, then `dotnet build` in a loop adding each erroring file until the project was
clean, then pruned so that nothing which actually compiles stays excluded. That pruning is why
`DbHub.cs`, `ImageHandler.ashx.cs` and `Program.cs` are compiled rather than excluded.

---

## 6. Next steps

Work that is genuinely blocked on a product or human decision, or that is out of scope for a
build-clean migration.

1. **Re-author the Web Forms presentation layer.** 194 `.ascx` user controls, 16 `.aspx` pages and
   4 master pages, plus the 11 server-control libraries that exist only to emit Web Forms controls
   (`ScrudFactory`, `StockTransactionFactory`, `TransactionViewFactory`, `AttachmentFactory`,
   `ReportEngine`, `TransactionChecklist`, `StockAdjustmentFactory`, `StockTransactionViewFactory`,
   `PartyControl`, `Flag`, `WebControls.Common`). This is a UI rewrite — Razor Pages, MVC or an
   API-plus-SPA — and the choice of target is a product decision, not a migration step. The
   excluded sources are the specification. The `.Data` halves of all those libraries are already
   migrated and compiling, so the data access does not need to be rewritten with the UI.
2. **Re-author the 35 ASMX services** as minimal API or controller endpoints (doc 33). The service
   contracts are in the excluded `Services/*.asmx.cs` files.
3. **Replace the SignalR JavaScript clients** with `@microsoft/signalr` and point them at the
   per-hub routes in §3. This has to happen together with (1), since the clients live in the
   excluded markup.
4. **Restore the page routes** from §4.16 as endpoints once the pages they pointed at exist.
5. **Decide how module assemblies are deployed.** `Program.MapModuleHubs` probes for
   `MixERP.Net.Core.Modules.*.dll` beside the host, which is where the old
   `<OutputPath>..\..\bin\</OutputPath>` put them. Redirecting eight SDK-style projects into the
   web application's output directory was deliberately *not* done: concurrent writes to one output
   folder cause intermittent file-locking build failures, which is a bad trade against a
   guaranteed-clean build. Options are a publish-time copy step, a real plugin host with
   `AssemblyLoadContext`, or inverting the dependency so the host can reference the modules
   directly. Until one is picked, the module hubs will not be reachable at runtime.
6. **Port `ExpressionHelper.Eval` to Roslyn** (`Microsoft.CodeAnalysis.CSharp.Scripting`). The
   CodeDOM original is preserved as `ExpressionHelper.CodeDom.cs`. Note that this is a
   compile-arbitrary-C#-at-runtime feature driven by configuration; whether it should come back at
   all is a security decision.
7. **Replace the HTML-to-image serializers.** Both implementations depended on Windows-only
   rendering. A cross-platform replacement means a headless browser (Playwright) or a native HTML
   renderer; either is a new dependency, so it needs sign-off.
8. **Make the `DbHub` backup cross-platform** — invoke `pg_dump` directly instead of generating a
   `.bat`, and drop the `.exe` suffix.
9. **Configure `UseForwardedHeaders`** with the real proxy topology, otherwise the client IP in
   §4.6 is the proxy's address.
10. **Register session keys for the System.Web adapters.** Session objects that are not primitives
    (`Session["ex"]`, the various cached entity objects) need an explicit serializer registration
    with `AddJsonSessionSerializer`. This cannot be enumerated from the compiling code alone,
    because most `Session[...]` writes are in the excluded pages.
11. **Re-verify the password hash against live data.** §4.10 is byte-identical by construction, but
    it guards sign-in, so it deserves a test against an existing database before release.
12. **The deployment utility stays Windows-only.** `net10.0-windows`, Windows Forms,
    `Microsoft.Web.Administration`. A cross-platform installer would be a different program; its
    `[assembly: SupportedOSPlatform("windows7.0")]` is declared by hand in
    `Properties/AssemblyInfo.cs`, because `GenerateAssemblyInfo=false` stops the SDK from deriving
    it from the target framework (without it, the platform-compatibility analyser reports CA1416 on
    all 24 Windows-only call sites).

13. **Remove `src/.nuget/`** (`NuGet.targets`-era solution packages: `xunit.runners` 1.9.2) once
    it is confirmed that no external build script still uses it. The SDK build does not use it.
14. **Fix the atx configuration length.** The iteration-20 atx run was rejected because
    `clientMetadata.configuration` was over 4096 characters. Shorten the plan context or
    additional prompt so future atx runs get past validation.

No warnings remain unresolved: the solution builds clean. Everything in this section is product
work, not a suppressed diagnostic.
