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

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using MixERP.Net.Common.Helpers;
using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.ScrudFactory.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MixERP.Net.WebControls.ScrudFactory.Controls
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Builds the file field of the schema-driven scrud form (the <c>bytea</c> columns) and
    ///     persists the chosen file into the catalog's temp-media directory.
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static class ScrudFileUpload
    {
        /// <summary>
        ///     The upload size ceiling. <c>IBrowserFile.OpenReadStream</c> refuses to read past a
        ///     limit (512 KB by default) precisely so a hostile client cannot exhaust server memory;
        ///     the legacy <c>FileUpload.SaveAs</c> was instead bounded by the
        ///     <c>httpRuntime/maxRequestLength</c> ASP.NET default of 4 MB. That default is carried
        ///     over here so the same files keep uploading.
        /// </summary>
        private const long MaximumFileSize = 4 * 1024 * 1024;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddFileUpload(HtmlTable, …)</c>: the <c>{column}_fileupload</c> id
        ///     (load-bearing — <c>GetFormCollection</c> reads the uploaded path back by it), the
        ///     image-extension rule, and the required rule plus
        ///     <c>Titles.RequiredFieldIndicator</c>-suffixed label for a non-nullable column.
        ///     <para>
        ///         The original carried a "Todo" asking for either a synchronous script-manager
        ///         postback or an AJAX upload handler, because a WebForms <c>FileUpload</c> could not
        ///         post inside an <c>UpdatePanel</c>. That limitation is gone: Blazor's
        ///         <c>InputFile</c> streams the file to the server without a full postback, which is
        ///         the second option the comment asked for, so the Todo is resolved rather than
        ///         carried forward.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void AddFileUpload(ScrudFieldTable fieldTable, string resourceClassName, string columnName,
            bool isNullable, string errorCssClass)
        {
            if (fieldTable == null)
            {
                return;
            }

            string label = ScrudLocalizationHelper.GetResourceString(resourceClassName, columnName);
            ScrudField field = GetFileUpload(columnName + "_fileupload");

            if (field == null)
            {
                return;
            }

            field.ColumnName = columnName;
            field.IsNullable = isNullable;
            field.ValidatorCssClass = errorCssClass;

            ScrudValidationRule validator = GetImageValidator(field, errorCssClass);

            if (!isNullable)
            {
                field.Label = label + Titles.RequiredFieldIndicator;
                field.AddRule(ScrudFactoryHelper.GetRequiredFieldValidator(field, errorCssClass));
                field.AddRule(validator);
                fieldTable.Add(field);
                return;
            }

            field.Label = label;
            field.AddRule(validator);
            fieldTable.Add(field);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>UploadFile(string catalog, FileUpload fileUpload)</c>.
        ///     <para>
        ///         <c>HttpContext.Current.Server.MapPath(tempMediaPath)</c> is gone on net10.0
        ///         (there is no ambient <c>HttpContext</c> and no <c>Server</c> object), so the
        ///         virtual <c>TempMediaPath</c> scrud parameter is now resolved against the injected
        ///         <see cref="IWebHostEnvironment" />'s web root — the modern equivalent of the web
        ///         application's physical root. The directory is still created when absent.
        ///     </para>
        ///     <para>
        ///         SECURITY. The original composed the saved file name from
        ///         <c>Guid.NewGuid()</c> + <c>Path.GetExtension(fileUpload.FileName)</c>, where
        ///         <c>FileName</c> is attacker-controlled: a crafted name could carry directory
        ///         separators or <c>..</c> segments through <c>GetExtension</c> and make
        ///         <c>Path.Combine</c>/<c>SaveAs</c> write outside the temp-media directory. The
        ///         resolved path is therefore verified to stay inside that directory before anything
        ///         is written, and the extension is taken from the file name's leaf only. This is a
        ///         path-traversal guard the original lacked; it is a deliberate hardening, recorded
        ///         in migration-summary.md.
        ///     </para>
        ///     <para>
        ///         The RETURN CONTRACT is unchanged, quirk included: when no file was chosen the
        ///         method still returns the bare GUID (not an empty string), because
        ///         <c>ScrudForm.Save</c> stores that value in the <c>bytea</c> column's data slot and
        ///         a caller that compared against <c>string.Empty</c> would see different behaviour.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static async Task<string> UploadFileAsync(string catalog, IWebHostEnvironment environment, IBrowserFile fileUpload)
        {
            string uploadDirectory = GetUploadDirectory(catalog, environment);

            if (uploadDirectory == null)
            {
                return string.Empty;
            }

            string id = Guid.NewGuid().ToString();

            //FileUpload.HasFile
            if (fileUpload != null)
            {
                string target = GetContainedPath(uploadDirectory, id, fileUpload.Name);

                if (target == null)
                {
                    //The composed path escaped the temp media directory - refuse the write.
                    return string.Empty;
                }

                //FileUpload.SaveAs(id)
                using (Stream source = fileUpload.OpenReadStream(MaximumFileSize))
                {
                    using (FileStream destination = new FileStream(target, FileMode.Create, FileAccess.Write))
                    {
                        await source.CopyToAsync(destination).ConfigureAwait(false);
                    }
                }

                id = target;
            }

            return id;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The same upload, for a caller holding an <see cref="IFormFile" /> (a classic
        ///     multipart form post or a minimal-API endpoint) rather than a Blazor
        ///     <see cref="IBrowserFile" />. Both overloads share one directory resolver and one
        ///     containment check, so neither can drift from the other.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static async Task<string> UploadFileAsync(string catalog, IWebHostEnvironment environment, IFormFile fileUpload)
        {
            string uploadDirectory = GetUploadDirectory(catalog, environment);

            if (uploadDirectory == null)
            {
                return string.Empty;
            }

            string id = Guid.NewGuid().ToString();

            if (fileUpload != null && fileUpload.Length > 0)
            {
                string target = GetContainedPath(uploadDirectory, id, fileUpload.FileName);

                if (target == null)
                {
                    return string.Empty;
                }

                using (Stream source = fileUpload.OpenReadStream())
                {
                    using (FileStream destination = new FileStream(target, FileMode.Create, FileAccess.Write))
                    {
                        await source.CopyToAsync(destination).ConfigureAwait(false);
                    }
                }

                id = target;
            }

            return id;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Replacement for <c>HttpContext.Current.Server.MapPath(tempMediaPath)</c>: resolves the
        ///     catalog's virtual <c>TempMediaPath</c> against the host's web root and ensures the
        ///     directory exists, as the original did.
        /// </summary>
        /// <returns>The physical directory, or null when it cannot be resolved.</returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static string GetUploadDirectory(string catalog, IWebHostEnvironment environment)
        {
            if (environment == null)
            {
                return null;
            }

            string tempMediaPath = DbConfig.GetScrudParameter(catalog, "TempMediaPath");

            if (string.IsNullOrWhiteSpace(tempMediaPath))
            {
                return null;
            }

            //Server.MapPath treated "~/..." and "/..." as application-root relative.
            string relative = tempMediaPath.Replace('\\', '/').TrimStart('~').TrimStart('/');

            string root = environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(root))
            {
                root = environment.ContentRootPath;
            }

            string uploadDirectory = Path.GetFullPath(Path.Combine(root, relative));

            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            return uploadDirectory;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Composes the target path from the generated GUID and the client-supplied file name's
        ///     extension, then verifies it really sits inside <paramref name="uploadDirectory" />.
        ///     The path-traversal guard the original lacked.
        /// </summary>
        /// <returns>The full physical path, or null when it would escape the directory.</returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static string GetContainedPath(string uploadDirectory, string id, string clientFileName)
        {
            //Take the LEAF of the client-supplied name first: a name such as
            //"..\..\web.config" or "a/../../b.jpg" must not contribute a path segment.
            string leaf = string.IsNullOrWhiteSpace(clientFileName)
                ? string.Empty
                : Path.GetFileName(clientFileName.Replace('\\', '/'));

            string extension = Path.GetExtension(leaf);

            //Reject an extension that is anything other than a simple suffix.
            if (!string.IsNullOrEmpty(extension) && extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return null;
            }

            string fileName = id + extension;

            if (!fileName.Equals(Path.GetFileName(fileName), StringComparison.Ordinal))
            {
                return null;
            }

            string target = Path.GetFullPath(Path.Combine(uploadDirectory, fileName));

            //Containment check: the resolved path must be under the temp media directory.
            string prefix = uploadDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!target.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }

            return target;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetFileUpload(string id)</c>. The <c>FileUpload</c> control is a removed
        ///     type; the field it describes is rendered by <c>ScrudFieldRow.razor</c> as the same
        ///     <c>&lt;input type="file" id="{column}_fileupload"&gt;</c> (through
        ///     <c>InputFile</c>, which also hands the component the chosen
        ///     <see cref="IBrowserFile" />).
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static ScrudField GetFileUpload(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            ScrudField field = new ScrudField();
            field.ElementId = id;
            field.Kind = ScrudFieldKind.FileUpload;

            return field;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetImageValidator</c>. The
        ///     <c>System.Web.UI.WebControls.RegularExpressionValidator</c> it returned
        ///     (<c>ErrorMessage = "&lt;br/&gt;" + Titles.InvalidImage</c>,
        ///     <c>Display = Dynamic</c>) is a removed type, and its rule ran only as emitted client
        ///     script plus a <c>Page.IsValid</c> check. Both are gone, so the extension whitelist is
        ///     re-expressed as a <see cref="ScrudValidationRule" /> and enforced SERVER-side by
        ///     <see cref="ScrudField.Validate" /> against the posted file's name — a client that
        ///     bypasses the browser can no longer slip a non-image through.
        ///     <para>
        ///         The <c>ValidationExpression</c> is carried byte-identically, so exactly the same
        ///         extensions (gif, jpg, jpeg, bmp, in any letter case) are accepted.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static ScrudValidationRule GetImageValidator(ScrudField fieldToValidate, string cssClass)
        {
            if (fieldToValidate == null)
            {
                return null;
            }

            ScrudValidationRule validator = new ScrudValidationRule();
            validator.Id = fieldToValidate.ElementId + "RegexValidator";
            validator.ErrorMessage = @"<br/>" + Titles.InvalidImage;
            validator.CssClass = cssClass;
            validator.DataType = ScrudValidationDataType.RegularExpression;
            validator.ValidationExpression = @"(.*\.([gG][iI][fF]|[jJ][pP][gG]|[jJ][pP][eE][gG]|[bB][mM][pP])$)";

            return validator;
        }
    }
}
