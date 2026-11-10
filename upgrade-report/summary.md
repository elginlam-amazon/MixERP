# 📊 Executive Report: MixERP.Net upgrade from .NET Framework 4.5/4.5.1 to .NET 10 (iteration 21)  
  
## 1. 📝 Executive summary  
  
Iteration 21 moved all 54 MixERP.Net projects from .NET Framework 4.5/4.5.1 to SDK-style projects on .NET 10. The solution now builds on Linux with **0 errors and 0 warnings**. The run started at 09:25 UTC and ended at 11:21 UTC on 10 Nov 2026.  
  
AWS Transform Custom (atx) migrated the first 7 projects and committed them in 3 commits. It then stopped (exit 1) because the account's monthly `AWS/dotnet-modernization` quota ran out (`SERVICE_QUOTA_EXCEEDED`). Kiro CLI took over and completed the other 47 projects.  
  
All shared libraries, the data layer, the 35 ASMX web services, the SignalR hubs and the web host now run on ASP.NET Core. The **Web Forms user interface is not migrated yet**:  
  
- ⚠️ About 600 Web Forms source files (`.aspx`/`.ascx`/`.Master` code-behind, designer files and server controls) are still in the tree, unchanged.  
- ⚠️ They are excluded from compilation and listed in each project's `PendingWebFormsRewrite` item group.  
- ⚠️ The build is green because these files are excluded, not because they were converted. The app's pages will not render until they are rewritten in Blazor.  
  
## 2. 🏗️ Application changes  
  
These descriptions of the old behaviour come from CodeKB, the index of the pre-upgrade code.  
  
- 📦 **Projects:**  
  - All 54 `.csproj` files are SDK-style.  
  - 53 target `net10.0`.  
  - The Windows Forms installer targets `net10.0-windows`.  
- 🌐 **Web host (`MixERP.Net.FrontEnd`):**  
  - **Before:** an IIS Web Forms site with `Global.asax`, OWIN SignalR 2, Forms authentication and an InProc session, configured in `Web.config`.  
  - **Now:** an ASP.NET Core app with `Program.cs` and `appsettings.json`. The appSettings keys are unchanged.  
  - Cookie authentication keeps the same login URL, timeout and sliding expiration.  
  - The ASP.NET Core session keeps the same timeout, and the 1 GB upload limit is kept.  
- 🔌 **Web services:**  
  - **Before:** ASP.NET AJAX `[ScriptService]` services called from jQuery.  
  - **Now:** the 35 `.asmx` services and 2 `.ashx` handlers are MVC controllers at the **same URLs**.  
  - They keep the same JSON contract: named body parameters and `{"d": …}` responses.  
- 📡 **SignalR:** the hubs (BaseHub, UpdateHub, DbHub, DayOperationHub) use ASP.NET Core SignalR and are mapped at `/signalr/{Hub}`.  
- 🧩 **Modules:**  
  - **Before:** the module DLLs were built into the site's `bin` folder.  
  - **Now:** they are copied next to the host after each build, and `ModuleLoader` registers their controllers and hubs.  
- 🐘 **Data access:**  
  - Npgsql 2.2 was upgraded to 8.0.5 in all 25 package references.  
  - PetaPoco, the database factory and the 15 data projects were ported to the new API.  
- 🎨 **UI:**  
  - `PartyControl` is now a Blazor component, `PartyControl.razor`, with the same markup.  
  - It is the pattern for the remaining Web Forms controls.  
  
## 3. 🛠️ Tools used  
  
- ⚙️ **.NET SDK 10.0.403:** builds, NuGet restore and the per-project build checks.  
- ☁️ **AWS Transform Custom CLI (atx):**  
  - Wrote the migration plan: 9 dependency levels and 18 batches.  
  - Migrated Framework, i18n, PetaPoco, CRM.data, Manufacturing.Data, POS.Data and part of ApplicationState.  
  - Exited with code 1 on `SERVICE_QUOTA_EXCEEDED`.  
- 🤖 **Kiro CLI 2.35.1 with the claude-opus-5.5 model:**  
  - Completed the remaining 47 projects.  
  - Fixed all build errors and warnings.  
  - Carried over and checked the open items from iteration 20.  
  - Each Kiro run succeeded on the first of its 5 allowed attempts.  
- 🧠 **CodeKB:** the index of the pre-upgrade code, used to understand the old components before changing them.  
- 📚 **Migration knowledge base:** reference for project files, System.Web, Web Forms to Blazor, System.Drawing and cryptography.  
  
## 4. 💻 Code changes  
  
- 📊 **Size:**  
  - atx's 3 commits: 45 files changed.  
  - Kiro's uncommitted changes: 323 tracked files changed (+2,971/−17,192 lines) and 50 new files.  
  - Most of the deleted lines come from the verbose legacy `.csproj` files and the removed `Web.config`, `app.config` and `packages.config` files.  
- 🧱 **New shared infrastructure:**  
  - `WebContext` takes over from `HttpContext.Current`, `ConfigurationManager` and `HostingEnvironment.MapPath`.  
  - `ScriptServiceController` and its value provider keep the ASMX JSON contract.  
  - `JSONHelper.DeserializeDynamic` is a drop-in for `JavaScriptSerializer.Deserialize<dynamic>`.  
  - `Models.ListItem` is a drop-in for the Web Forms `ListItem`.  
- 🔐 **Security:**  
  - Sign-in issues an ASP.NET Core cookie instead of a `FormsAuthenticationTicket`.  
  - `ContentProtectionMiddleware` replaces IIS request filtering: config files, `bin` and `obj` are never served, and `/Resource` and `/Backups` require sign-in.  
  - `ImageHandler` only serves files inside the site.  
  - The database backup starts `pg_dump` directly. It no longer writes the password into a generated `.bat` file.  
  - The password hash uses `SHA512.Create()`. It is the same algorithm, so existing hashes still match.  
- 🐘 **Npgsql 8 API changes:**  
  - `UserName` is now `Username`.  
  - `SyncNotification` and `SSL` were removed.  
  - `NpgsqlError` became `PostgresNotice`.  
  - `NpgsqlException.Code` became `SqlState`.  
  - `TrustServerCertificate` was removed because it is obsolete.  
- 📦 **Packages:**  
  - Serilog 1.5.7 → 4.4.0, with `Serilog.Sinks.File` 7.0.0.  
  - Newtonsoft.Json 6.0.4 → 13.0.4.  
  - Microsoft.AspNet.WebApi.Client 5.2.3 → 6.0.0.  
  - MarkdownDeep → Markdig 1.6.0.  
  - System.Drawing → SkiaSharp 4.155.0.  
  - Microsoft.Web.Administration 7 → 11.1.0.  
- 🌍 **Network calls:** `WebClient` was replaced by `HttpClient` in the Updater, ReportManager and `PageUtility.CleanUrl`.  
- 🖼️ **Images:**  
  - `ImageHelper` was ported to SkiaSharp.  
  - The Windows-only HTML-to-image serializers were removed. The checklist e-mail now attaches an HTML file.  
- 🧹 **Cleanup:**  
  - Removed `packages.config`, the library `app.config` files (binding redirects only), `Global.asax`, `Web.config`, the OWIN `Startup` and `CLSCompliant(true)`.  
  - Removed the unused `ExpressionHelper`, which compiled C# at runtime.  
  - Fixed case mismatches in project and solution paths so the solution builds on Linux.  
- ✅ **Verification:**  
  - `dotnet build MixERP.Net.sln --no-incremental` finishes with 0 errors and 0 warnings.  
  - All 54 projects also build on their own (exit 0).  
  - The solution has no test projects, so no unit tests were run.  
  
## 5. ⏱️ Time savings estimate  
  
The estimate compares the work done here with a developer doing the same migration by hand.  
  
| Work | Manual estimate | Automated |  
| --- | --- | --- |  
| 📁 Converting 54 projects (packages, references, assembly info, signing keys) | 3–4 days | minutes |  
| 🐘 Npgsql, Serilog, Newtonsoft and HTTP API ports across 25+ projects | 3–5 days | ~1 h |  
| 🌐 Host rewrite: `Program.cs`, auth, session, static files, SignalR, module loading | 3–4 days | ~30 min |  
| 🔌 35 ASMX services and 2 handlers moved to controllers with the same contract | 4–6 days | ~30 min |  
| 🛡️ System.Drawing, cryptography, configuration, path-case fixes, warning cleanup | 2–3 days | ~20 min |  
  
- 💡 **Total:** about **15–22 developer-days** of work done in about **2 hours of wall-clock time**. That is roughly a 90 % saving for this part of the work.  
- ⚠️ **Not included:** the Web Forms → Blazor rewrite of 214 markup files and about 600 source files. It is the largest remaining piece of work, estimated at a further **6–10 developer-weeks**.  
  
## 6. 🚀 Next steps  
  
### 🧪 Validation  
- 🏃 **Run the host against a test PostgreSQL database.** Check dependency injection, middleware order and the session.  
- 🔐 **Sign in with existing accounts** to confirm the password hashes still match.  
- 🐘 **Run integration tests for the Npgsql 2 → 8 change.** Type mapping, notices and error codes cannot be checked at compile time.  
- 📡 **Test the four SignalR hubs** after moving the JavaScript to `@microsoft/signalr`.  
- 🔌 **Smoke-test the JSON contract** of the 35 services with the existing jQuery pages.  
- 🖼️ **Compare SkiaSharp thumbnails** with the old GDI+ output. They are not byte-identical.  
  
### 🏗️ Recommendations  
- 🎨 **Do the Web Forms → Blazor rewrite.** Follow the `PartyControl.razor` pattern, in this order:  
  1. Shared WebControls.  
  2. `FrontEnd/Base`, the master pages and the main pages.  
  3. The module `.ascx` controls.  
  
  Then wire up `AddRazorComponents` and `AddAdditionalAssemblies`, and delete each original file once its replacement works.  
- 📦 **Include the module DLLs in `dotnet publish`.** Today they are copied only on build.  
- 🛡️ **Configure `UseForwardedHeaders`** with the real proxy addresses. Code still trusts `X-Forwarded-For` from any client.  
- 🔒 **Add `[Authorize]` to the script services**, except sign-in and the office list. This needs a product decision.  
- 🌍 **Add `UseRequestLocalization`** to replace `<globalization culture="auto">`.  
- 🧪 **Add a regression test suite and CI on .NET 10** with warnings treated as errors.  
- 🧹 **Delete the unused `src/packages/` (81 MB) and `src/.nuget/` folders**, and the stray `FrontEnd/MixERP.net.FrontEnd/` folder whose name differs only in case.  
- ☁️ **Before the next AWS Transform run:** request a higher `AWS/dotnet-modernization` quota, and shorten the atx configuration to under 4,096 characters.  
- 📄 **Detailed record:** `/data/code/src/summary.md`. Run log: `/data/iteration-21/code-upgrade.log`.

## Verification results

Copied from this iteration's report.json, progress.json and runtime-validation.log after the final build. This section is not written by the AI.

- ⏭️ Initial build of the original code: SKIPPED (The solution contains .NET Framework projects, which cannot be built in the Linux upgrade container, so the original code was not built before the upgrade.)
- 🛠️ Post-upgrade verification build: SUCCESS
- 🧪 Automated tests: test execution not recorded (see post-upgrade-verification.log)
- ⏱️ Transformation duration: 123 minute(s)
- ⏳ Estimated time saved: 0 minute(s)
- 🔎 CodeKB calls (atx): 0

## Kiro credit usage

💳 Kiro CLI credits used: 228.43  
Includes 1.18 for job setup (startup smoke test and knowledge-base indexing).  

## Codebase index refresh

- Outcome: Index current
- Reason: Index current at bb756c5092fc63f420b173a18975d9ec6e3f702c.
- Previously indexed commit: bb756c5092fc63f420b173a18975d9ec6e3f702c
- Source commit at transformation start: bb756c5092fc63f420b173a18975d9ec6e3f702c
- Duration: 0.0s
