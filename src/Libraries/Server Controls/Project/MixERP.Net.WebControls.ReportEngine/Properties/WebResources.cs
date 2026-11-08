/********************************************************************************
Copyright (C) Binod Nepal, Mix Open Foundation (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/

//WEB RESOURCE DECLARATIONS - migrated from the ten
//[assembly: System.Web.UI.WebResource(name, contentType)] attributes that used to sit
//at the top of Report/Javascript.cs.
//
//System.Web.UI.WebResourceAttribute does not exist on modern .NET and this project does
//NOT recreate it (a hand-written System.Web type would be a compatibility shim, not a
//migration). The attribute was purely declarative anyway: the scripts are served at
//runtime by the host's /_mixerp/script/{assemblyName}/{resourceName} endpoint, which
//reads them through MixERP.Net.Common.Helpers.JSUtility.GetEmbeddedScript ->
//Assembly.GetManifestResourceStream(resourceName). Nothing reads the attribute.
//
//The ten resource-name/content-type pairs are therefore preserved as real BCL assembly
//metadata (System.Reflection.AssemblyMetadataAttribute) so they remain discoverable in
//the compiled assembly exactly as before. The authoritative runtime keys are these same
//strings in three places, which MUST stay byte-identical:
//  1. the <EmbeddedResource ... LogicalName="..."> items in the .csproj,
//  2. the JSUtility.AddJSReference calls in Report.razor (AddJavascript),
//  3. this file.

using System.Reflection;

[assembly: AssemblyMetadata("MixERP.Net.WebControls.ReportEngine.Scripts.JsBarcode.CODE128.js", "application/x-javascript")]
[assembly: AssemblyMetadata("MixERP.Net.WebControls.ReportEngine.Scripts.JsBarcode.CODE39.js", "application/x-javascript")]
[assembly: AssemblyMetadata("MixERP.Net.WebControls.ReportEngine.Scripts.JsBarcode.EAN_UPC.js", "application/x-javascript")]
[assembly: AssemblyMetadata("MixERP.Net.WebControls.ReportEngine.Scripts.JsBarcode.ITF.js", "application/x-javascript")]
[assembly: AssemblyMetadata("MixERP.Net.WebControls.ReportEngine.Scripts.JsBarcode.ITF14.js", "application/x-javascript")]
[assembly: AssemblyMetadata("MixERP.Net.WebControls.ReportEngine.Scripts.JsBarcode.JsBarcode.js", "application/x-javascript")]
[assembly: AssemblyMetadata("MixERP.Net.WebControls.ReportEngine.Scripts.ReportEngine.Barcode.js", "application/x-javascript")]
[assembly: AssemblyMetadata("MixERP.Net.WebControls.ReportEngine.Scripts.JQueryQRcode.jquery.qrcode.js", "application/x-javascript")]
[assembly: AssemblyMetadata("MixERP.Net.WebControls.ReportEngine.Scripts.JQueryQRcode.qrcode.js", "application/x-javascript")]
[assembly: AssemblyMetadata("MixERP.Net.WebControls.ReportEngine.Scripts.ReportEngine.QRCode.js", "application/x-javascript")]
