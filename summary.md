# MixERP – .NET Framework 4.5.1 → .NET 10 upgrade (cycle summary)

## Result

All 54 requested projects (`MixERP.Net.sln` plus `Utilities/MixERP.Net.Utility.Deployment`) build with `dotnet build`, exit code 0 and **0 errors**, after a clean build (bin/obj removed).
After a clean build of the solution, 18 unique warnings remain. All of them existed before the upgrade (see "Remaining warnings").

## Approach

MixERP is an ASP.NET Web Forms application: 245 `.aspx/.ascx/.master` files and about 210 server-control classes built on `System.Web.UI`. Re-authoring every page as Razor Pages or Blazor (KB 25/26) is a multi-week rewrite. The upgrade therefore keeps the Web Forms code and runs it on **WebFormsForCore 1.6.8**, a port of the .NET Framework `System.Web` stack to ASP.NET Core / .NET 10. With this choice, the page, control and business code compiles mostly unchanged.

- **`src/Directory.Build.targets`** contains the shared `System.Web` wiring. Projects opt in with `<UseSystemWeb>` (plus `UseSystemWebExtensions` / `UseSystemWebServices`):
  - `net10.0` builds use the `WebFormsForCore.Web` packages (`.Extensions` and `.Services` where needed).
  - `netstandard2.0` builds reference the .NET Framework 4.8 `System.Web*` reference assemblies, so the netstandard2.0 build still works for .NET Framework hosts. The targets also give `netstandard.dll` an alias for the `HttpUtility` CS0433 conflict, keep CoreWCF's duplicate `System.Web.Services.Description` out of compilation, suppress the CLS warnings that WebFormsForCore causes on net10.0 only, and pin `System.Security.Cryptography.Pkcs`/`Xml` to 10.0.13 (NU1902/NU1903).
- **`src/Directory.Build.props`** keeps the hand-written `AssemblyInfo.cs` files (versions, `CLSCompliant`, `WebResource`). It turns off only the conflicting SDK-generated attributes.
- The `Microsoft.AspNetCore.SystemWebAdapters` packages from the earlier pass were removed. They duplicate `System.Web` types and have no `System.Web.UI`.

### Project files
- **Libraries** (Logic, Server Controls, Server Controls Data, CurrencyAPI, Checklists, `FrontEnd.Data`, module `*.Data`): SDK-style, `net10.0;netstandard2.0`. ProjectReference paths now use the correct letter case (the old paths broke on Linux). Package changes:
  - Serilog `4.4.1-dev` prerelease → 4.4.0
  - Npgsql: 10.0.3 for net10.0, 8.0.9 for netstandard2.0 (Npgsql 10 has no netstandard2.0 build)
  - MarkdownDeep (.NET 3.5 only) → Markdig.Signed 1.4.0
  - `lib/HtmlRenderer` WinForms DLL → HtmlRenderer.PdfSharp 1.6.1
  - Added `System.Drawing.Common` and `System.CodeDom` 10.0.13, and `System.Reflection.Emit.*` for netstandard2.0
  - Removed `Microsoft.VisualBasic`
- **`MixERP.Net.FrontEnd`**: `Microsoft.NET.Sdk.Web` Exe, single TFM `net10.0`. Output goes to `bin\` with no TFM subfolder, as WebFormsForCore requires.
  - New **`Program.cs`** replaces IIS/OWIN hosting. It maps the SignalR hubs, which it discovers in the `bin` folder like SignalR 2 did, and passes all other requests to `UseWebForms(HandleAllRequestsWithWebForms)`.
  - New `appsettings.json` holds the Kestrel 1 GB request-size limit that used to be in `maxRequestLength` / `maxAllowedContentLength`.
  - `Web.config` and `Global.asax` stay in place because WebFormsForCore reads them. The stale Npgsql 2.2.3 `DbProviderFactories` block was removed.
  - Npgsql 2.x date/time behaviour is kept with `Npgsql.EnableLegacyTimestampBehavior`.
- **Web Forms modules** (BackOffice, Finance, Inventory, Purchase, Sales, CRM, POS, Manufacturing): SDK-style, single TFM `net10.0`. These are UI projects that reference the FrontEnd Exe, so they cannot multi-target. They still build into `FrontEnd\bin\`.
- **Utilities**: SqlBundler targets `net10.0`. The Deployment installer targets `net10.0-windows` (WinForms/IIS) with `EnableWindowsTargeting`.
- **`MixERP.Net.sln`**: project paths now use the correct letter case.

### Code changes (behaviour notes)
- **Npgsql 2 → 8/10:**
  - `UserName` → `Username` in all connection builders.
  - The removed `SyncNotification` and `SSL` options were dropped. `SslMode.Prefer` was already set and keeps the same intent.
  - `NpgsqlException.Code` → `PostgresException.SqlState`. Other `NpgsqlException`s are still re-thrown as before.
  - `NpgsqlError` → `PostgresNotice` (`MessageText`).
  - PetaPoco now gets `NpgsqlFactory` directly instead of looking it up through `DbProviderFactories`, which has no registration on .NET.
- **SignalR 2 → ASP.NET Core SignalR:**
  - Hubs call `Clients.*.SendAsync`. Each hub is served at `/signalr/{hubName}`.
  - The masters, `EODOperation.ascx`, `Update.aspx` and `DatabaseBackup.ascx.js` now use `@microsoft/signalr` 10.0.11 (`Scripts/signalr/signalr.min.js`) through a small `getHub(name)` helper.
  - The Forms Authentication ticket is decrypted for `/signalr` requests so that `Context.User` works as it did before.
- **Logging:** Serilog `RollingFile` → `File` sink with daily rolling (Serilog.Sinks.File 7.0.0).
- **`HtmlRendererImageSerializer`** (used by the TransactionChecklist e-mail) now attaches a **PDF** instead of a bitmap image. HtmlRenderer.WinForms only exists for Windows desktop targets. The unused, IE-based `WebBrowserImageSerializer` was deleted.
- **`FileSystemHelper.IsDirectoryWritable`:** Code Access Security checks always passed under full trust. The method now tests the directory by creating and deleting a temporary file.
- **`DirectoryHelper.CopyDirectory`:** the VB `FileSystem.CopyDirectory` call is replaced by an equivalent recursive copy. A local test of the copy and of the Markdig output passed.
- **Removed obsolete or unsupported APIs:**
  - CAS attributes (`AspNetHostingPermission`, `PermissionSet`, `SecurityPermission`)
  - The serialization scaffolding on `MixERPException` (SYSLIB0051)
  - `SHA512CryptoServiceProvider` → `SHA512.Create()` (same hash)
  - `WebClient` → `HttpClient` in `PageUtility.CleanUrl`, `ReportManager.Client` and `DownloadUpdate`. Progress is still reported, once per percentage point.
- **System.Drawing:** kept, because the app is deployed on Windows (KB 21). The image members are marked `[SupportedOSPlatform("windows")]`.
- **CLS:** members that expose Npgsql types are marked `[CLSCompliant(false)]`, following the existing `DbOperation` pattern.

## Remaining warnings (all existed before the upgrade)
- CS3008 on `Update.aspx.cs` (`_release`, `_downloadUrl`): the markup references these names.
- CS3001/CS3002/CS3009 in `Entities/PetaPocoDB.cs`: the PetaPoco assembly has never been marked CLS-compliant.
- CS0164 (unused label in `ApplicationError.cs`) and CS0649 (`MixERPWebpage.menus`).

## Next steps
1. **Runtime validation.** Per instructions, the app was not run in this cycle. Smoke-test sign-in (Forms auth and session), the routes in `Application/Routes.cs`, the ASMX services, `WebResource`/`ScriptResource.axd`, and the handlers declared under `system.webServer/handlers` (`Resources.js`, `FileUploadHanlder.*`). If WebFormsForCore does not honour those IIS-only handler registrations, move them to `system.web/httpHandlers`.
2. **WebFormsForCore risk.** It is a community package with a single maintainer and about 4k downloads. Plan an incremental move of pages to Razor Pages or Blazor (KB 25/26) and treat WebFormsForCore as a bridge.
3. **SignalR hubs still depend on `System.Web.HttpContext.Current`.** `AppUsers.GetCurrentUserDB()` and `AppUsers.GetCurrent()` read it, and it is null inside ASP.NET Core hub calls. Refactor `BaseHub.Terminate`, `UpdateHub`, `DbHub` and `DayOperationHub` to resolve the user and catalog from `Context.User`. Also check that the hub `Clients` are used only while the hub method is running.
4. **PDF output and fonts.** The TransactionChecklist e-mail now attaches a PDF, and the `EmailImageExtension` setting no longer affects the output. PDFsharp needs a font resolver on non-Windows hosts.
5. **Linux hosting** would require replacing System.Drawing in `ImageHelper` and `ImageHandler.ashx` with SkiaSharp (KB 21). It would also need path, case and `pg_dump.exe` fixes in `DbHub`, which uses a `.bat` file.
6. **`ExpressionHelper.Eval`** has no callers. It relies on CodeDom compilation, which .NET does not support, so it always returns an empty string. Delete it, or move it to Roslyn if it is ever needed.
7. **HtmlRenderer 1.6.x is not strong-named** (CS8002 is suppressed in HtmlParser). The old `lib/HtmlRenderer` build was strong-named.
8. **Cleanup:**
   - The old SignalR 2 / jQuery.signalR scripts in `bundles/` and `Scripts/` are now unused. Regenerate the bundles without them.
   - The legacy `src/packages` folder and `lib/HtmlRenderer` can be removed.
9. **Optional:** `Microsoft.CSharp` and `System.Data.DataSetExtensions` package references are only needed for the netstandard2.0 builds. They can be made conditional.
