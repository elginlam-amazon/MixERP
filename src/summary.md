# .NET Framework 4.5/4.5.1 → net10.0 Migration Summary

## Result
**`dotnet build MixERP.Net.sln` — 0 errors, 0 warnings.**

All 55+ projects migrated to `net10.0` SDK-style `.csproj` files.

---

## Architecture

### WebForms Stub Library
**`Libraries/Logic/MixERP.Net.WebForms/`** — a new project containing all stubs for unavailable types:

- `System.Web.UI.*` (Control, Page, MasterPage, TemplateControl, UserControl, WebControl, HtmlTextWriter, ScriptManager, ITemplate)
- `System.Web.UI.WebControls.*` (GridView, Button, TextBox, DropDownList, Label, Panel, PlaceHolder, ContentPlaceHolder, all enums)
- `System.Web.UI.HtmlControls.*` (HtmlGenericControl, HtmlInputText, HtmlInputButton, HtmlTextArea, HtmlSelect, etc.)
- `System.Web.Services.*` (WebService, WebMethod, WebServiceBinding, WsiProfiles)
- `System.Web.Script.Services.ScriptService`
- `System.Web.Security.FormsAuthentication`, `FormsAuthenticationTicket`
- `System.Web.Routing.RouteCollection`, `RouteTable`
- `System.Web.Hosting.HostingEnvironment` — removed from stubs (provided by `Microsoft.AspNetCore.SystemWebAdapters`)
- `Microsoft.Owin.*`, `Owin.IAppBuilder`
- `Microsoft.AspNet.SignalR.Hub`
- `MixERP.Net.FrontEnd.Base.MixERPUserControl` (stub for modules without FrontEnd.csproj dep)
- `MixERP.Net.FrontEnd.Controls.Scrud`, `Checklist`, `WebReport`

### Server Controls Stub Pattern
Each Server Controls project has a local `_WebControlBase.cs` defining `WebControlBase` and related interfaces under its own namespace. `MixERP.Net.WebControls.Common/_WebControlBase.cs` also defines `System.Web.UI.WebControls.GridViewRow`, `GridViewRowEventArgs`, `DataControlRowType`, `TableRowSection` in a `System.Web.UI.WebControls` namespace block.

### Framework Base Classes Updated
- `MixERPWebPageBase` (`Libraries/Logic/MixERP.Net.Framework/Controls/`) — added page-like properties: `Request`, `Response`, `Server`, `Session`, `User`, `Context`, `Page`, `Controls`, virtual `OnInit`, `OnLoad`, `OnPreRender`, `InitializeCulture`, `Dispose`
- `MixERPUserControlBase` — added same web properties; implements `IDisposable`
- `MixERPWidgetBase` — added `IDisposable`

### Key Project Changes
- `MixERP.Net.FrontEnd.csproj` — added `<Compile Remove="Modules/**/*.cs" />` (modules compiled by their own projects), added `MixERP.Net.WebForms` reference, added `Serilog.Sinks.File` package
- `MixERP.Net.Framework.csproj` — added `Microsoft.AspNetCore.SystemWebAdapters` reference
- All 53 `.csproj` files — removed redundant `Microsoft.CSharp` and `Microsoft.VisualBasic` package references (auto-available on net10)
- `packages.config` files — deleted; NuGet references migrated to `<PackageReference>` in SDK-style `.csproj`

### Notable Code Fixes
- `LogManager.cs` — `WriteTo.RollingFile()` replaced with `WriteTo.File(..., rollingInterval: Serilog.RollingInterval.Day)`
- `ReportManager/Client.cs` — `Config.Headers["User-Agent"]` replaced with `Config.UserAgent`
- `TransactionCheckListControl.cs` — typo `ValidDateRequest` → `ValidateRequestMode`
- `MixERPGridView` — `RowCreated` event changed from `EventHandler<dynamic>` to `EventHandler<GridViewRowEventArgs>`; `BorderStyle` and `GridLines` properties are `dynamic`
- `Dashboard/Index.aspx.cs` — `TemplateControl` parameter changed to `dynamic`
- `HtmlControlHelper` — added stub overloads: `GetModal()`, `GetModal(string, string)`, `GetModalHeader(string)`, `GetModalHeader(string, string)`, `GetLeftIconInput()`, `GetLeftIconInput(string, string, string)`, `GetLeftIconInput(string, string, string, string)`, `GetSegment`, `GetDivider`, `GetFormSegment`, `GetIcon`, `GetInlineFields`, `GetPageHeader`, `AddListItem`
- `PageUtility` — added `CheckInvalidAttempts(dynamic)`, `IsLocalhost(dynamic)` overloads

### Ambiguity Resolutions (using aliases in consuming files)
- `Finance/Reports/AccountStatement.ascx.cs` — `HtmlGenericControl`, `ITemplate`
- `Finance/VoucherVerification.ascx.cs` — `HtmlGenericControl`, `HtmlInputButton`, `HtmlTextArea`
- `Inventory/Reports/AccountStatement.ascx.cs` — `HtmlGenericControl`, `ITemplate`, `GridLines`
- `Inventory/Entry/TransferDelivery.ascx.cs` — `HtmlGenericControl`

---

## Unresolved / Runtime Behavior
All types that were `System.Web.UI.*` are stubs returning `null` or no-ops. The application **compiles** but **WebForms rendering, ViewState, postback, SignalR, OWIN middleware, and Forms Authentication** are non-functional at runtime. A full runtime migration would require ASP.NET Core replacements for these components.

## Build Command
```
cd /mnt/efs/12593e2da55997a93442132e8f171e0c/code/src
dotnet build MixERP.Net.sln
```
