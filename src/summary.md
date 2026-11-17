# MixERP .NET Framework 4.5 / 4.5.1 → net10.0 upgrade (iteration 5)

## Outcome

- `dotnet build MixERP.Net.sln -c Release` exits 0 with **0 errors and 1 warning** (CS0649, explained below). The solution has 53 projects; the Deployment installer has its own `.sln` and also builds with 0 errors and 0 warnings.
- `dotnet test MixERP.Net.sln -c Release --no-build` exits 0. The solution has no test projects, so no tests ran.
- The application was not started, by design.

## Starting point for this iteration

- AWS Transform Custom (atx) did not change any file this time. It stopped at once with `SERVICE_QUOTA_EXCEEDED` (see `/data/iteration-5/code-upgrade.log`), so the working tree was still the original .NET Framework code at commit `bb756c50`.
- Iteration 4 had already done a complete migration of the same commit, and it passed post-upgrade verification. That work is on the local branch `main-upgrade-itr-4` (commit `1fdb5728`). I applied that diff onto this branch, without its two report files, instead of migrating 54 projects again from scratch. It covers:
  - SDK-style projects.
  - Web Forms hosted on WebFormsForCore 1.6.8.
  - Npgsql 10, ASP.NET Core SignalR, Serilog 4, Markdig and `HttpClient`.
  - The `Program.cs` host and the request-filtering middleware.
  - The `Directory.Build.props/targets` files.

  The iteration 4 report describes these changes in detail. I then built the tree again and fixed what had changed since iteration 4.

## Changes made in this iteration

1. **Security advisory GHSA-6527-9fp8-r4x6 (NU1902, 252 warnings).** This advisory was published after iteration 4. It affects `Microsoft.AspNetCore.WebUtilities` 2.3.10, which reaches the libraries through WebFormsForCore → CoreWCF.Http on net10.0 and through SystemWebAdapters on netstandard2.0. A request can exhaust server memory by sending multipart form data.
   - **Fix:** `Directory.Build.targets` now pins `Microsoft.AspNetCore.WebUtilities` to 2.3.14 for every `UseSystemWeb` library.
   - The FrontEnd web project is not pinned. It gets this assembly from the ASP.NET Core shared framework, which is not affected.
2. **System.Drawing → SkiaSharp 4.156.1 (CA1416, 68 warnings).** On .NET 10, `System.Drawing.Common` works only on Windows and throws `PlatformNotSupportedException` on Linux. Before the upgrade, `ImageHelper.GetResizedImage` and `Conversion.TryCastByteArray` produced the JPEG thumbnails that `BackOffice/Handlers/ImageHandler.ashx` serves for photos and attachments.
   - `MixERP.Net.Common` now references `SkiaSharp` and `SkiaSharp.NativeAssets.Linux` instead of `System.Drawing.Common`.
   - `ImageHelper.GetResizedImage(SKBitmap, w, h)` keeps the same scaling, centring and portrait-crop maths, the transparent canvas, high-quality bicubic sampling and JPEG output. It encodes at quality 75, the GDI+ default.
   - `ImageHandler.ashx` decodes images with `SKBitmap.Decode`. Validation still falls back to `~/Static/images/document.png` when a file is not a valid image.
   - Public API changes in `MixERP.Net.Common`:
     - `ImageFormat` → `SKEncodedImageFormat` in `GetImageFormat`, `GetFileExtension` and `Conversion.ConvertImageToByteArray`.
     - `Image` → `SKBitmap` in `GetResizedImage`, `TryCastByteArray` and `ConvertImageToByteArray`.
     - `IHtmlImageSerializer.ImageFormat` is now `SKEncodedImageFormat`.
     - The unused GDI+-only `ImageHelper.GetEncoder(ImageFormat)` was removed.

     All callers inside the solution were updated.
3. **CS3008 (4 warnings).** In `Modules/Update.aspx(.cs)`, the protected fields `_downloadUrl` and `_release` were renamed to `downloadUrl` and `release`, so the identifiers are CLS-compliant. The markup was updated to match.

## Next steps

- **CS0649 `MixERPWebpage.menus` (1 warning).** This warning already existed before the upgrade. The private field is never assigned, and its only readers, the private methods `VerifyAccess()` and `GetMenu()`, are never called. I left this code alone because whether to remove it or wire it up is a product decision.
- **SkiaSharp behaviour to check at runtime:**
  - Thumbnails from `ImageHandler.ashx` for JPG, PNG, GIF and BMP files.
  - `ConvertImageToByteArray` with `Bmp` or `Gif`. Skia cannot encode these formats and returns `null`; no current caller passes them.
  - Linux images need `libfontconfig1`, which `SkiaSharp.NativeAssets.Linux` depends on. In minimal containers, consider `SkiaSharp.NativeAssets.Linux.NoDependencies` instead.
- **External callers of the `MixERP.Net.Common` image API:** .NET Framework code that uses the netstandard2.0 build of these methods must move from `System.Drawing` types to the SkiaSharp types.
- **Items still open from iteration 4:**
  - Provision PostgreSQL and the `DbServer.config` settings.
  - Move secrets out of the config files.
  - Add an explicit `<machineKey>` if the SignalR hubs can't decrypt the sign-in cookie.
  - Run manual smoke tests: sign-in, routes and ASMX services, attachments, SignalR, transactions, stock-adjustment Dr/Cr validation, and date handling under Npgsql 6+.
  - Decide on the checklist e-mail attachment, which is now HTML instead of an image.
  - Rework the IIS-based Deployment installer.
  - Add a publish step that copies the module assemblies.
  - Add an xUnit test project and CI.
- **Upgrade pipeline:** each iteration starts again from the original commit. If atx keeps failing on quota, start the next cycle from the `main-upgrade-itr-*` branch so the work is not repeated.
