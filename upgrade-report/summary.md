# 🚀 MixERP: .NET Framework 4.5.1 → .NET 10 Upgrade Executive Report  
  
## 1. 📋 Executive Summary  
  
MixERP is a PostgreSQL-backed ASP.NET Web Forms ERP made of 54 projects. In iteration 15 it was moved from .NET Framework 4.5.1 to **.NET 10 (`net10.0`)** by an automated pipeline: Microsoft Upgrade Assistant converted the project files, then Kiro CLI fixed the remaining build errors and migrated the code.  
  
- ✅ **Outcome:** all **54 projects build with 0 errors**, and so does `MixERP.Net.sln`.  
- 🧪 **Tests:** a new unit-test project passes **8/8 tests**.  
- ⚠️ **Warnings:** 29 remain after a clean rebuild. All of them existed before the migration: CLS-compliance warnings in the PetaPoco entity models, plus one unused field.  
- ⏱️ **Duration:** about 1 h 38 min end to end (00:22 → 02:00 UTC). The Kiro CLI session took 66 min 9 s, used 272.11 credits, needed one attempt and had no out-of-memory kills.  
- 🟡 **Not yet production-ready:** the code compiles, but Web Forms pages do not render on .NET 10 yet. A page compiler or a Razor/Blazor rewrite is still needed (see Next Steps).  
  
## 2. 🏗️ Application Changes  
  
- 📦 **Project files:** all 54 projects now use the modern SDK-style format.  
  - 🤖 Upgrade Assistant converted 45 of them. Every one of those runs finished with 0 failed steps.  
  - 🛠️ Upgrade Assistant could not open the other 9 (`InvalidProjectFileException` on the legacy Web Application targets). Kiro rewrote those by hand: `MixERP.Net.FrontEnd` and the 8 `Core.Modules.*` UI projects.  
- 🎯 **Target frameworks:**  
  - 📚 Libraries target `net10.0;netstandard2.0`. Projects that only targeted netstandard2.0 were completed to this pair.  
  - 🌐 The FrontEnd web app (Web SDK), the module projects and SqlBundler target `net10.0`.  
  - 🪟 The Deployment installer targets `net10.0-windows` and can now be built on Linux build agents.  
- 🧩 **Web Forms bridge:** the existing server controls and code-behind compile against **CoreWebForms 0.5.0** (MIT licence) running on **SystemWebAdapters 2.3.0**. Business logic is unchanged.  
- 🔐 **Hosting and security:**  
  - `Global.asax` and `Web.config` were replaced by `Program.cs` running on ASP.NET Core.  
  - Forms authentication became cookie authentication with the same login URL, 60-minute sliding expiry and login identity.  
  - The static-file allow-list means binaries and config files are never served.  
- ⚙️ **Configuration:**  
  - The `appSettings` values moved to `App.config`, with `App.Release.config` replacing the old `Web.Release.config` transform.  
  - `appsettings*.json` holds hosting and logging settings.  
  - `Web.config` now contains only IIS request limits and MIME types.  
- 🐧 **Linux builds:** path-casing errors in project references and the solution file were fixed, so the solution builds on Linux.  
  
## 3. 🧰 Tools Used  
  
- 🟣 **.NET SDK 10.0.403:** build, restore and `dotnet test`. SDKs 5–8 were also installed.  
- 🔄 **Microsoft .NET Upgrade Assistant 1.0.518.26217:** in-place framework upgrade, SDK-style conversion and package upgrades. It ran 45 times with 0 failures.  
- 🤖 **Kiro CLI 2.14.2 with model `claude-opus-5.5`:** fixed the build errors, converted the 9 legacy web projects, migrated the code, cleaned up warnings and wrote the tests.  
- 🧠 **Supporting tools:**  
  - The migration knowledge base: prebuild cleanup, the Web Forms guides, System.Drawing, cryptography, the test stack, and blocked packages.  
  - CodeKB, an index of the pre-upgrade code, used to map how the old code worked.  
  
## 4. 💻 Code Changes  
  
- 🐘 **Npgsql 2.2.3 → 10.0.3** (8.0.8 for the netstandard2.0 builds):  
  - Connection-string option renames, such as `UserName` → `Username`.  
  - Database error codes are now read from `PostgresException.SqlState` (Npgsql 2 exposed them as `NpgsqlException.Code`).  
  - Server notices use `PostgresNotice`.  
  - PetaPoco now receives `NpgsqlFactory.Instance` directly.  
- 🌐 **36 ASMX script services:** these now run through a new dispatcher, `ScriptServiceEndpoints`, plus a small attribute and base-class set in the Framework library. URLs and the JSON response format (`{"d":…}`) are unchanged.  
- 🧾 **`JavaScriptSerializer` replaced:** a new `JSONHelper.DeserializeDynamic` returns data in the same shape, and serialization uses Newtonsoft.Json 13.0.4.  
- 📡 **SignalR 2 / OWIN → ASP.NET Core SignalR:** client calls now use `SendAsync`, and the hubs (including those in the modules) are mapped at `/signalr/{hub}`.  
- 🖼️ **Imaging:**  
  - Thumbnail generation moved from System.Drawing to **SkiaSharp**, which works on Linux as well as Windows.  
  - The checklist email snapshot is now generated as a **PDF** (HtmlRenderer.PdfSharp). The old image renderers depended on Windows Forms, which a .NET 10 library cannot use.  
- 🔁 **Replacements for APIs that don't exist on .NET 10:**  
  - `UpdatePanel`/`ScriptManager` → plain `Panel`, so the CRUD forms now do full postbacks.  
  - `Server.Transfer`/`Execute`/`TransferRequest` → `Response.Redirect`.  
  - Code Access Security attributes removed.  
  - `WebClient` → `HttpClient`.  
  - `SHA512CryptoServiceProvider` → `SHA512.Create()`. The hash output is identical, and a test checks it.  
- 📦 **Packages:**  
  - Serilog 4.4.0 and Serilog.Sinks.File 7.0.0 (the original was 1.5).  
  - MarkdownDeep replaced by Markdig.Signed 1.5.0.  
  - System.Security.Cryptography.Pkcs pinned to 10.0.14, which removes the NU1902/NU1903 vulnerability warnings.  
- 🗑️ **Removed:** the dead CodeDOM `ExpressionHelper`, which was unused, cannot run on .NET 10 and was a code-injection risk. Also removed the WinForms `WebBrowserImageSerializer`, the OWIN `Startup` class, and all `packages.config` files.  
- 🧹 **Warnings:** the active warning count dropped from 465 to 29.  
- 🧪 **New test project:** `Tests/MixERP.Net.Common.Tests` (xUnit 2.9.3) covers JSON parsing, the SHA-512 hash, directory copying and image resizing.  
  
## 5. ⏳ Time Savings Estimate  
  
- 📊 **Scope:**  
  - 54 projects.  
  - About 1,000 C# files, about 500 of which use `System.Web`.  
  - About 250 Web Forms markup files.  
  - 36 ASMX services, 4 SignalR hubs, and a database-driver jump from Npgsql 2 to 10.  
- 👷 **Manual estimate:** about **12–18 developer-days** to reach the same state:  
  - project conversion: about 2 days;  
  - fixing compile errors and API migrations: about 5–7 days;  
  - the ASMX, authentication, SignalR and hosting work: about 3–4 days;  
  - imaging, packages, warnings and tests: about 2–3 days.  
- ⚡ **Automated:** about **1.6 hours** of pipeline time.  
- 💰 **Estimated saving:** roughly **90–95 %** of the engineering effort for the compile-ready milestone. This estimate does not cover the remaining Web Forms runtime work.  
  
## 6. 🔜 Next Steps  
  
- 🧱 **Make the pages render (blocking for production):** either build CoreWebForms' page compiler from source, or rewrite pages to Razor Pages or Blazor, starting with the high-traffic screens.  
- 💾 **Session state:** two session values (a stored exception and a product model) cannot be stored by the new session layer as-is. Store an id or message instead, or register custom serializers.  
- 🔌 **Browser scripts:**  
  - Move the real-time update scripts from the SignalR 2 jQuery client to `@microsoft/signalr`.  
  - Check that the AJAX error handler reads the new error JSON.  
- 🐘 **Database validation:** run all transaction flows against a real PostgreSQL instance. Pay particular attention to how Npgsql 10 handles `DateTime` time zones (it expects UTC).  
- 📧 **Business sign-off:**  
  - PDF attachments for checklist emails.  
  - Full postbacks instead of partial page updates.  
  - The URL changes caused by redirects.  
- 🔒 **Security:**  
  - Move the secrets in `Resource/Configs/*.config` to environment variables or a secret store.  
  - Review the `DisplayErrorDetails` setting for each environment.  
  - Consider adding global authorization.  
- 🧪 **Testing:**  
  - Add integration tests (`WebApplicationFactory`) for the ASMX dispatcher and the login flow.  
  - Run a smoke test on Windows/IIS and on Linux, which needs SkiaSharp's Linux native library (included) and fonts for PDF generation.  
- 🧹 **Cleanup:**  
  - Decide whether the server-control libraries still need their netstandard2.0 builds; they only contain non-UI helpers.  
  - Remove the leftover `Web*.config` files under `Modules/*`.  
  - Address the CLS-compliance warnings that predate the migration.  

## Kiro credit usage

💳 Kiro CLI credits used: 297.20  
Includes 0.85 for job setup (startup smoke test and knowledge-base indexing).  

## Codebase index refresh

- Outcome: Index current
- Reason: Index current at bb756c5092fc63f420b173a18975d9ec6e3f702c.
- Previously indexed commit: bb756c5092fc63f420b173a18975d9ec6e3f702c
- Source commit at transformation start: bb756c5092fc63f420b173a18975d9ec6e3f702c
- Duration: 0.0s
