# .NET Framework → net10.0 migration: completion cycle summary

## Starting state
AWS Transform Custom had already moved 52 of the 53 requested projects to SDK-style `net10.0`
(Blazor Server host plus Razor class libraries). `dotnet build MixERP.Net.sln` passed with 0 errors
and 179 warnings. The one requested project atx skipped was
`Utilities/MixERP.Net.Utility.Deployment`. Its log called it "not part of the 53-project solution
scope", but it is on the requested list. It was still a legacy .NET 4.5.1 project using `packages.config`.

## Changes in this cycle

### MixERP.Net.Utility.Deployment (newly migrated)
Before the upgrade (per CodeKB), this was a Windows Forms installer. It installed PostgreSQL
(`Installer/PostgreSQL`), created the database and roles through Npgsql (`Installer/Database`,
`DbScript`), and provisioned IIS with `Microsoft.Web.Administration`: app pool, site, binding,
application and NTFS ACLs (`Installer/MixERP.Install`).
- Converted to an SDK-style `net10.0-windows` WinExe with `UseWindowsForms`. It sets
  `EnableWindowsTargeting=true` so it also builds on Linux CI; it still only runs on Windows.
- Replaced `packages.config` and the vendored `packages/` binaries (Npgsql 2.2.4.3,
  Microsoft.Web.Administration 7.0) with `PackageReference`s: Npgsql 10.0.3 (the solution's
  version), Microsoft.Web.Administration 11.1.0 and System.Configuration.ConfigurationManager 10.0.0.
- Npgsql API change: `NpgsqlConnectionStringBuilder.UserName` → `Username`.
- Removed `RuntimeHelpers.PrepareConstrainedRegions()` (SYSLIB0004; CERs are a no-op on .NET Core).
- Moved `Properties/AssemblyInfo.cs` into csproj metadata and removed the `<startup>`
  `supportedRuntime` element from `App.config`. The `appSettings` stay, read by `ConfigurationManager`.
- The SQL resources keep their manifest names (`MixERP.Net.Utility.Installer.Configs.SQL.*`).
- It has its own `MixERP.Net.Utility.Deployment.sln` and was not added to `MixERP.Net.sln`.

### Warning cleanup (179 → 0 compiler/analyzer warnings)
- **CS3001/3002/3003/3009/3010 (CLS compliance, about 105):** the assemblies keep
  `CLSCompliant(true)`, which some atx files rely on (see `DbFactory/Properties/AssemblyAttributes.cs`).
  Fixes:
  - Added `CLSCompliant(true)` to PetaPoco, which builds clean under it, so Entities can expose `Sql` and `Page<T>`.
  - Marked members that expose third-party types without CLS markings (ASP.NET Core, ImageSharp,
    MailKit, Npgsql) as `[CLSCompliant(false)]`.
  - Marked the `ComponentBase`-derived types the same way: the Framework control base classes,
    ScrudForm/Scrud and the affected `.razor` components (`@attribute`).
- **ASP0006 (75):** replaced `sequence++` and `ref int sequence` in hand-written render trees with
  literal sequence numbers. This affects ScrudFactory `CommandPanel` and `ScrudPager` (helpers now
  return `RenderFragment`s) and the two `GridViewColumnHelper` action templates. The markup is the
  same; Blazor's diffing is now stable across renders.
- **CS0618:** PuppeteerSharp `SetContentAsync(string, NavigationOptions)` → `SetContentOptions`
  (Load) plus `WaitForNetworkIdleAsync()`. The old call's networkidle0 condition was ignored for
  SetContent, so this restores the intended "wait for images/CSS" behavior before the screenshot.
- **CS0649 / behavioral fix:** `StockTransactionView.razor` filter fields (office, party, user,
  reference number, statement reference) now start as `string.Empty` instead of `null`.
  `transactions.get_product_view` filters with `LIKE '%' || lower(x) || '%'`, so a NULL filter
  matched no rows on first load. WebForms `TextBox.Text` was never null, so this restores the
  original behavior.
- **BL0007:** `MixERPUserControlBase.ValidDateRequest` is now an auto-property (same default, `Inherit`).
- **CA2255:** kept the deliberate `[ModuleInitializer]` Npgsql provider registration in
  Entities and suppressed the warning locally with a justification comment.

## Build result
- `dotnet build MixERP.Net.sln` (Debug): 0 errors, 0 compiler/analyzer warnings.
- `dotnet build Utilities/MixERP.Net.Utility.Deployment/MixERP.Net.Utility.Deployment.csproj`:
  0 errors, 0 warnings.
- The only remaining build output is the SixLabors ImageSharp license notice (see below).

## Next steps
1. **[Blocking for Release] ImageSharp license.** `SixLabors.ImageSharp` 3.2.0 (Common,
   HtmlParser, BackOffice) checks for a license at build time. Debug builds only warn, but
   **`dotnet build -c Release` fails** with "No Six Labors license found". I tested 3.1.12, the
   last version without the build-time check, and it raises NU1903 (known high-severity
   vulnerabilities), so I kept 3.2.0. A product decision is needed:
   - provide `SixLaborsLicenseKey` (CI secret) or a `sixlabors.lic` file; or
   - check eligibility under the Six Labors Split License; or
   - port the three image helpers to SkiaSharp (MIT), as KB doc 21 suggests.
2. The Deployment installer still provisions IIS and the .NET Framework handler path
   (`RegisterDotNetInIIS`, app pool settings). The migrated FrontEnd is now an ASP.NET Core / Kestrel
   app, so this flow needs redesign (ASP.NET Core Hosting Bundle / reverse proxy) before it can deploy
   the new app. It now compiles and runs on .NET 10, but it targets the old hosting model.
3. The Deployment `App.config` still has hard-coded default passwords (`PostgresPassword` etc.),
   as in the original. Move them to user input or a secret store.
4. The repository-root `packages/` folder (legacy NuGet binaries, 368 files) is no longer referenced
   by any SDK project. Remove it once nothing outside the solution needs it.
5. There are no automated .NET test projects. Use the manual smoke-test list in atx's
   `migration-summary.md`, plus the ScrudFactory pager/command panel (ASP0006 refactor) and the
   StockTransactionView initial grid load (null-filter fix).
