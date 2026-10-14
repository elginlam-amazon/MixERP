# Version upgrade - Code transformation summary
    
⚙️ Transformation engine: DOTNET_UPGRADE_ASSISTANT_WITH_Q  
🚀 Confidence score: Low  
📂 Files changed: 98  
💾 Lines of code changed: 10633  
📦 Dependencies changed: 19  
⏱️ Transformation duration: 55 minute(s)  
🛠️ Build status post transformation: FAILURE  
⏳ __Estimated time saved: 0 minute(s)__  

## Project Types

The solution contains different project types that were upgraded to appropriate target frameworks:

📚 Library projects: 33 (target: net10.0;netstandard2.0)  
🚀 Application projects: 3 (target: net10.0)  

## Build summary

The code was successfully upgraded to the target framework version. The workflow performed:
- an initial build in the original version
- code transformation to upgrade the code and package dependencies to the target version


## Dependencies removed

The following dependencies were removed in the build file:

None

## Dependencies added

The following dependencies were added in the build file:

- nuget:Microsoft.CSharp (version 4.7.0)
- nuget:System.Data.DataSetExtensions (version 4.5.0)
- nuget:Serilog (version 4.3.1)
- nuget:System.Configuration.ConfigurationManager (version 10.0.13)
- nuget:Npgsql (version 8.0.9)
- nuget:WebFormsForCore.Web (version $(WebFormsForCoreVersion))
- nuget:Microsoft.AspNetCore.SystemWebAdapters (version 2.3.0)
- nuget:System.Runtime.Caching (version 10.0.13)
- nuget:HtmlRenderer.WinForms (version 1.6.1)
- nuget:System.Drawing.Common (version 10.0.13)
- nuget:Newtonsoft.Json (version 13.0.4)
- nuget:System.CodeDom (version 10.0.13)
- nuget:WebFormsForCore.Web.Extensions (version $(WebFormsForCoreVersion))
- nuget:Microsoft.VisualBasic (version 10.3.0)
- nuget:Microsoft.AspNet.WebApi.Client (version 6.0.0)
- nuget:Markdig (version 1.4.0)
- nuget:Microsoft.Web.Administration (version 11.1.0)
- nuget:WebFormsForCore.Web.Services (version $(WebFormsForCoreVersion))
- nuget:Serilog.Sinks.File (version 7.0.0)

## Dependencies upgraded

The following dependencies were upgraded in the build file:

None

## Code changed

The following source code files were transformed:

- .nuget/NuGet.Config
- src/Directory.Build.props
- src/FrontEnd/MixERP.Net.FrontEnd.Data/MixERP.Net.FrontEnd.Data.csproj
- src/FrontEnd/MixERP.Net.FrontEnd.Data/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/MixERP.Net.FrontEnd.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/BackOffice.Data/MixERP.Net.Core.Modules.BackOffice.Data.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/BackOffice.Data/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/BackOffice/MixERP.Net.Core.Modules.BackOffice.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/BackOffice/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/CRM.data/MixERP.Net.Core.Modules.CRM.data.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/CRM.data/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/CRM/MixERP.Net.Core.Modules.CRM.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/CRM/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Finance.Data/MixERP.Net.Core.Modules.Finance.Data.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Finance.Data/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Finance/MixERP.Net.Core.Modules.Finance.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Finance/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Inventory.Data/MixERP.Net.Core.Modules.Inventory.Data.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Inventory.Data/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Inventory/MixERP.Net.Core.Modules.Inventory.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Inventory/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Manufacturing.Data/MixERP.Net.Core.Modules.Manufacturing.Data.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Manufacturing.Data/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Manufacturing/MixERP.Net.Core.Modules.Manufacturing.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Manufacturing/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/POS.Data/MixERP.Net.Core.Modules.POS.Data.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/POS.Data/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/POS/MixERP.Net.Core.Modules.POS.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/POS/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Purchase.Data/MixERP.Net.Core.Modules.Purchase.Data.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Purchase.Data/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Purchase/MixERP.Net.Core.Modules.Purchase.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Purchase/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Sales.Data/MixERP.Net.Core.Modules.Sales.Data.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Sales.Data/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Sales/MixERP.Net.Core.Modules.Sales.csproj
- src/FrontEnd/MixERP.Net.FrontEnd/Modules/Sales/packages.config
- src/FrontEnd/MixERP.Net.FrontEnd/packages.config
- src/Libraries/Checklists/MixERP.Net.FirstSteps/MixERP.Net.FirstSteps.csproj
- src/Libraries/CurrencyAPI/MixERP.Net.CurrencyConversion.Currencylayer/MixERP.Net.CurrencyConversion.Currencylayer.csproj
- src/Libraries/CurrencyAPI/MixERP.Net.CurrencyConversion.Currencylayer/packages.config
- src/Libraries/CurrencyAPI/MixERP.Net.CurrencyConversion.OpenExchangeRates/MixERP.Net.CurrencyConversion.OpenExchangeRates.csproj
- src/Libraries/CurrencyAPI/MixERP.Net.CurrencyConversion.OpenExchangeRates/packages.config
- src/Libraries/Logic/MixER.Net.StateServer/MixERP.Net.ApplicationState.csproj
- src/Libraries/Logic/MixERP.Net.Common/MixERP.Net.Common.csproj
- src/Libraries/Logic/MixERP.Net.Common/packages.config
- src/Libraries/Logic/MixERP.Net.DbFactory/MixERP.Net.DBFactory.csproj
- src/Libraries/Logic/MixERP.Net.DbFactory/packages.config
- src/Libraries/Logic/MixERP.Net.Entities/MixERP.Net.Entities.csproj
- src/Libraries/Logic/MixERP.Net.Entities/packages.config
- src/Libraries/Logic/MixERP.Net.Framework/MixERP.Net.Framework.csproj
- src/Libraries/Logic/MixERP.Net.Framework/packages.config
- src/Libraries/Logic/MixERP.Net.HtmlParser/MixERP.Net.HtmlParser.csproj
- src/Libraries/Logic/MixERP.Net.Messaging/MixERP.Net.Messaging.Email.csproj
- src/Libraries/Logic/MixERP.Net.Messaging/packages.config
- src/Libraries/Logic/MixERP.Net.ReportManager/MixERP.Net.ReportManager.csproj
- src/Libraries/Logic/MixERP.Net.ReportManager/packages.config
- src/Libraries/Logic/MixERP.Net.TransactionGovernor.Data/MixERP.Net.TransactionGovernor.Data.csproj
- src/Libraries/Logic/MixERP.Net.TransactionGovernor.Data/packages.config
- src/Libraries/Logic/MixERP.Net.TransactionGovernor/MixERP.Net.TransactionGovernor.csproj
- src/Libraries/Logic/MixERP.Net.Updater/Api/Release.cs
- src/Libraries/Logic/MixERP.Net.Updater/MixERP.Net.Updater.csproj
- src/Libraries/Logic/MixERP.Net.Updater/packages.config
- src/Libraries/Logic/MixERP.Net.i18n/MixERP.Net.i18n.csproj
- src/Libraries/Logic/MixERP.Net.i18n/packages.config
- src/Libraries/Logic/PetaPoco/PetaPoco.csproj
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.Common.Data/MixERP.Net.WebControls.Common.Data.csproj
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.Common.Data/packages.config
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.Flag.Data/MixERP.Net.WebControls.Flag.Data.csproj
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.Flag.Data/packages.config
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.ReportEngine.Data/MixERP.Net.WebControls.ReportEngine.Data.csproj
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.ReportEngine.Data/packages.config
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.ScrudFactory.Data/MixERP.Net.WebControls.ScrudFactory.Data.csproj
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.ScrudFactory.Data/packages.config
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.StockTransactionView.Data/MixERP.Net.WebControls.StockTransactionViewFactory.Data.csproj
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.StockTransactionView.Data/packages.config
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.TransactionChecklist.Data/MixERP.Net.WebControls.TransactionChecklist.Data.csproj
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.TransactionChecklist.Data/packages.config
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.TransactionViewFactory.Data/MixERP.Net.WebControls.TransactionViewFactory.Data.csproj
- src/Libraries/Server Controls/Data/MixERP.Net.WebControls.TransactionViewFactory.Data/packages.config
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.AttachmentFactory/MixERP.Net.WebControls.AttachmentFactory.csproj
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.Common/MixERP.Net.WebControls.Common.csproj
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.Flag/MixERP.Net.WebControls.Flag.csproj
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.PartyControl/MixERP.Net.WebControls.PartyControl.csproj
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.ReportEngine/MixERP.Net.WebControls.ReportEngine.csproj
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.ReportEngine/packages.config
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.ScrudFactory/MixERP.Net.WebControls.ScrudFactory.csproj
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.ScrudFactory/packages.config
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.StockAdjustmentFactory/MixERP.Net.WebControls.StockAdjustmentFactory.csproj
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.StockTransactionFactory/MixERP.Net.WebControls.StockTransactionFactory.csproj
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.StockTransactionView/MixERP.Net.WebControls.StockTransactionViewFactory.csproj
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.TransactionChecklist/MixERP.Net.WebControls.TransactionChecklist.csproj
- src/Libraries/Server Controls/Project/MixERP.Net.WebControls.TransactionViewFactory/MixERP.Net.WebControls.TransactionViewFactory.csproj
- src/MixERP.Net.sln
- src/Utilities/MixERP.Net.Utility.Deployment/MixERP.Net.Utility.Deployment.csproj
- src/Utilities/MixERP.Net.Utility.Deployment/MixERP.Net.Utility.Deployment.sln
- src/Utilities/MixERP.Net.Utility.Deployment/packages.config
- src/Utilities/MixERP.Net.Utility.SqlBundler/MixERP.Net.Utility.SqlBundler.csproj

## Next step

A pull request has been raised in your source control system, and is ready for validation.

Please review and accept the code changes.
## Kiro credit usage

💳 Kiro CLI credits used: 2.89  

## Codebase index refresh

- Outcome: Index current
- Reason: Index current at bb756c5092fc63f420b173a18975d9ec6e3f702c.
- Previously indexed commit: bb756c5092fc63f420b173a18975d9ec6e3f702c
- Source commit at transformation start: bb756c5092fc63f420b173a18975d9ec6e3f702c
- Duration: 0.0s

## Requirements register refresh

- Outcome: Timed out
- Reason: Register refresh exceeded 300000ms.
- Duration: 300.4s

> The requirements register was not refreshed for this transformation, so feature parity was checked against the **previous** register content. The transformation proceeded regardless.

## Feature Parity Verification

> **Parity results missing.** The feature-parity check did not produce a report, so parity could not be verified. Transformation sign-off is blocked.
>
> Detail: Parity re-derivation was aborted (300s refresh deadline) before the upgraded analysis was obtained.
