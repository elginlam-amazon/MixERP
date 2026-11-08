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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;
using ConfigurationHelper = MixERP.Net.WebControls.AttachmentFactory.Helpers.ConfigurationHelper;

namespace MixERP.Net.WebControls.AttachmentFactory
{
    /// <summary>
    ///     The two endpoints the attachment control talks to. Replaces the former
    ///     <c>FileUploadHanlder : IHttpHandler</c> and
    ///     <c>Handlers.UploadService : WebService, IHttpHandlerFactory</c>, both of which were
    ///     registered from the host's legacy Web.config <c>&lt;handlers&gt;</c> section as
    ///     <c>FileUploadHanlder</c> (<c>FileUploadHanlder.ashx</c>, verb <c>*</c>) and
    ///     <c>FileUploadService</c> (<c>FileUploadHanlder.asmx</c>, verb <c>*</c>).
    ///     <para>
    ///         The legacy paths are preserved VERBATIM, misspelling included: the
    ///         <c>UploadHandlerUrl</c> and <c>UndoUploadServiceUrl</c> rows of the attachment
    ///         configuration table point at them and AttachmentFactory.js posts to whatever
    ///         those rows return.
    ///     </para>
    ///     <para>
    ///         <c>UploadService</c>'s <c>IHttpHandlerFactory</c> implementation is gone rather
    ///         than ported: it used private reflection into
    ///         <c>WebServiceHandlerFactory.CoreGetHandler</c> purely to self-host the ASMX
    ///         service. Endpoint routing is what does that job here, and reflection into a type
    ///         that no longer exists could not work in any case. Its
    ///         <c>[PermissionSet(SecurityAction.InheritanceDemand)]</c> and the control's
    ///         <c>[AspNetHostingPermission]</c> were Code Access Security host-trust demands,
    ///         which modern .NET does not have and which carried no application-level
    ///         authorization behaviour.
    ///     </para>
    /// </summary>
    public static class AttachmentFactoryEndpoints
    {
        /// <summary>
        ///     Maps the upload and undo-upload endpoints at the exact paths and verbs the legacy
        ///     handlers were registered for. Call this from the host's <c>Program.cs</c>:
        ///     <c>app.MapAttachmentFactoryEndpoints();</c>
        /// </summary>
        public static IEndpointRouteBuilder MapAttachmentFactoryEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            //THE HANDLERS WRITE THE RESPONSE THEMSELVES AND RETURN Task, NOT Task<IResult>.
            //A `Task<IResult>`-returning method passed here as a METHOD GROUP binds the
            //MapPost(string, RequestDelegate) overload - Task<IResult> converts to Task by a
            //covariant method-group conversion - and RequestDelegate DISCARDS the returned
            //IResult. The routes would answer 200 with an EMPTY BODY: the uploader would store
            //an empty FilePath and $(data).find("boolean").text() would never confirm an undo,
            //all while building green (the compiler only warns, ASP0016). Writing straight to
            //HttpContext.Response removes the overload ambiguity entirely instead of relying
            //on the lambda-vs-method-group binding rules.

            //Port of FileUploadHanlder.ProcessRequest. The legacy handler was registered for
            //verb "*"; the uploader (ajax-file-upload.js) only ever POSTs multipart form data,
            //and GET/HEAD on an upload endpoint had no meaningful behaviour, so POST is the
            //mapped verb.
            endpoints.MapPost("/FileUploadHanlder.ashx", UploadAsync)
                //The uploader posts via XMLHttpRequest with no antiforgery token, exactly as it
                //did against the legacy .ashx (which had no token check either).
                .DisableAntiforgery();

            //Port of UploadService.UndoUpload. The legacy ASMX was reached by a jQuery
            //$.post of the "uploadedFilesJson" form field.
            //
            //THE METHOD SUFFIX IS PART OF THE URL. The client does not use the Web.config
            //handler path - it posts to whatever the "UndoUploadServiceUrl" row of the
            //attachment configuration table returns, and every seed script
            //(db/patch.sql, blank-db.sql, sample-db.sql, tables-and-constraints.sql and the
            //incremental scripts) sets that row to "~/FileUploadHanlder.asmx/UndoUpload".
            //Under ASMX the handler matched on the ".asmx" extension and the trailing
            //"/UndoUpload" selected the [WebMethod]. Mapping only the bare ".asmx" path would
            //404 every undo request silently - no reset, no notification, and the orphaned
            //uploads never deleted - so the method-suffixed path is mapped first. The bare
            //path is also mapped, for any installation whose config row was hand-edited.
            endpoints.MapPost("/FileUploadHanlder.asmx/UndoUpload", UndoUploadAsync)
                .DisableAntiforgery();

            endpoints.MapPost("/FileUploadHanlder.asmx", UndoUploadAsync)
                .DisableAntiforgery();

            return endpoints;
        }

        /// <summary>
        ///     Port of <c>FileUploadHanlder.ProcessRequest(HttpContext)</c>.
        ///     <para>
        ///         The response shape is load-bearing: <c>text/plain</c> whose body is the
        ///         comma-joined list of SAVED file names. ajax-file-upload.js tries
        ///         <c>JSON.parse</c> on it, falls back to the raw text, and then uses that value
        ///         as the <c>FilePath</c> of the <c>Upload</c> object it posts back to the undo
        ///         endpoint. It is written directly to the response (see the note in
        ///         <see cref="MapAttachmentFactoryEndpoints" />) so it cannot be discarded.
        ///     </para>
        /// </summary>
        private static async Task UploadAsync(HttpContext context)
        {
            //Per-request, not hoisted and not cached: the catalog is the tenant/user scoping
            //for both the attachments directory and the allowed-extension list.
            string catalog = AppUsers.GetCurrentUserDB();
            string attachmentsDirectory = ConfigurationHelper.GetAttachmentsDirectory(catalog);
            string physicalDirectory = GetPhysicalAttachmentsDirectory(attachmentsDirectory);

            Collection<string> uploadedFiles = new Collection<string>();

            //HttpPostedFile/HttpFileCollection -> IFormFileCollection. Reading Form on a
            //request that is not form-encoded throws, which the legacy Request.Files did not,
            //hence the content-type guard.
            if (context.Request.HasFormContentType)
            {
                IFormFileCollection files = (await context.Request.ReadFormAsync(context.RequestAborted)).Files;

                List<string> allowedExtensions = GetAllowedExtensions(catalog);

                for (int i = 0; i < files.Count; i++)
                {
                    IFormFile file = files[i];
                    string extension = Path.GetExtension(file.FileName);

                    //The allow-list is enforced SERVER side, not only by validateFileName() in
                    //AttachmentFactory.js. A file that fails is skipped silently, as before.
                    if (!allowedExtensions.Contains(extension.Replace(".", "")))
                    {
                        continue;
                    }

                    //Path.GetFileName is the path-traversal guard on the client-supplied name
                    //and is kept. Random.Shared replaces a per-call "new Random()", which on
                    //.NET Framework seeded from a low-resolution clock and handed consecutive
                    //uploads the same number - a latent duplicate-name bug. The intent (a
                    //random prefix up to 9999999) is preserved.
                    string fileName = Random.Shared.Next(9999999).ToString(CultureInfo.InvariantCulture)
                                      + "_" + Path.GetFileName(file.FileName);

                    string savePath = ResolveInsideDirectory(physicalDirectory, fileName);

                    if (savePath == null)
                    {
                        continue;
                    }

                    //Server.MapPath pointed into a directory IIS had already created. Nothing
                    //creates it now, so a missing directory would fail every upload.
                    Directory.CreateDirectory(physicalDirectory);

                    using (FileStream stream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                    {
                        await file.CopyToAsync(stream, context.RequestAborted);
                    }

                    uploadedFiles.Add(fileName);
                }
            }

            //Identical to the legacy Response.ContentType + Response.Write pair.
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(string.Join(",", uploadedFiles), Encoding.UTF8,
                context.RequestAborted);
        }

        /// <summary>
        ///     Port of <c>UploadService.UndoUpload(string uploadedFilesJson)</c>.
        ///     <para>
        ///         The legacy method was <c>[ScriptMethod(ResponseFormat = ResponseFormat.Xml)]</c>
        ///         over a <c>bool</c>, which ASMX serialized as
        ///         <c>&lt;boolean&gt;true&lt;/boolean&gt;</c>, and the client reads it with
        ///         <c>$(data).find("boolean").text()</c>. The XML envelope is therefore
        ///         reproduced by hand - returning JSON would break undo silently while the build
        ///         stays green. It is written directly to the response (see the note in
        ///         <see cref="MapAttachmentFactoryEndpoints" />) so it cannot be discarded.
        ///     </para>
        /// </summary>
        private static async Task UndoUploadAsync(HttpContext context)
        {
            string uploadedFilesJson = null;

            if (context.Request.HasFormContentType)
            {
                IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted);
                uploadedFilesJson = form["uploadedFilesJson"];
            }

            Collection<Entities.Core.Attachment> uploads = GetAttachmentCollection(uploadedFilesJson);

            string attachmentsDirectory =
                ConfigurationHelper.GetAttachmentsDirectory(AppUsers.GetCurrentUserDB());
            string physicalDirectory = GetPhysicalAttachmentsDirectory(attachmentsDirectory);

            foreach (Entities.Core.Attachment upload in uploads)
            {
                //HARDENING. The legacy code concatenated the client-supplied FilePath straight
                //into Server.MapPath before File.Delete, so a caller could post
                //"../../web.config" and have it deleted. The name is now reduced to its file
                //component and the resolved path is confirmed to sit inside the attachments
                //directory before anything is removed.
                string path = ResolveInsideDirectory(physicalDirectory, upload.FilePath);

                if (path == null)
                {
                    continue;
                }

                //A file that is already gone is not an error, as before.
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            //The ASMX [ScriptMethod(ResponseFormat.Xml)] envelope over a bool, reproduced
            //verbatim because the client reads it with $(data).find("boolean").text().
            context.Response.ContentType = "text/xml; charset=utf-8";
            await context.Response.WriteAsync("<boolean>true</boolean>", Encoding.UTF8,
                context.RequestAborted);
        }

        /// <summary>
        ///     Port of <c>UploadService.GetAttachmentCollection(string)</c>.
        ///     <para>
        ///         <c>JavaScriptSerializer</c> + <c>dynamic</c> is replaced by
        ///         <see cref="System.Text.Json" />. The three property names are the ones the
        ///         client-side <c>Upload</c> object in AttachmentFactory.js emits - Comment,
        ///         FilePath and OriginalFileName - and the method keeps its
        ///         <c>Collection&lt;Entities.Core.Attachment&gt;</c> return shape and its
        ///         tolerance of null/garbage input.
        ///     </para>
        /// </summary>
        public static Collection<Entities.Core.Attachment> GetAttachmentCollection(string json)
        {
            Collection<Entities.Core.Attachment> details = new Collection<Entities.Core.Attachment>();

            if (string.IsNullOrWhiteSpace(json))
            {
                return details;
            }

            List<UploadedFile> result;

            try
            {
                result = JsonSerializer.Deserialize<List<UploadedFile>>(json, SerializerOptions);
            }
            catch (JsonException)
            {
                //JavaScriptSerializer.Deserialize<dynamic> on malformed input threw too, and the
                //legacy caller treated "nothing to undo" as the outcome. Deleting nothing is the
                //safe equivalent.
                return details;
            }

            if (result != null)
            {
                foreach (UploadedFile item in result)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    Entities.Core.Attachment detail = new Entities.Core.Attachment();
                    detail.Comment = item.Comment;
                    detail.FilePath = item.FilePath;
                    detail.OriginalFileName = item.OriginalFileName;

                    details.Add(detail);
                }
            }

            return details;
        }

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            //JavaScriptSerializer matched member names case-insensitively.
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        /// <summary>
        ///     The wire shape of the client-side <c>Upload</c> object defined in
        ///     AttachmentFactory.js.
        /// </summary>
        private sealed class UploadedFile
        {
            public string Comment { get; set; }
            public string FilePath { get; set; }
            public string OriginalFileName { get; set; }
        }

        /// <summary>
        ///     Port of <c>FileUploadHanlder.GetAllowedExtensions()</c>.
        /// </summary>
        private static List<string> GetAllowedExtensions(string catalog)
        {
            string extensions = ConfigurationHelper.GetAllowedExtensions(catalog);

            if (string.IsNullOrWhiteSpace(extensions))
            {
                return new List<string>();
            }

            return extensions.Split(',').ToList();
        }

        /// <summary>
        ///     Replaces <c>context.Server.MapPath(...)</c>.
        ///     <see cref="MixERPConfiguration.MapPath" /> is the already-migrated equivalent of
        ///     <c>HostingEnvironment.MapPath</c>: it resolves an application-relative path
        ///     against the host's content root and normalizes a rooted one.
        ///     <para>
        ///         THE VALUE MUST BE FORCED TO VIRTUAL-PATH FORM FIRST. The configured
        ///         directory is seeded as <c>/Resource/Static/Attachments/</c> (a leading slash,
        ///         no <c>~</c>) and <c>PageUtility.ResolveUrl</c> passes it through untouched
        ///         because it only rewrites <c>~</c>-prefixed values. <c>MapPath</c> treats a
        ///         leading slash as application-root-relative only on Windows; on Linux every
        ///         such path is <c>Path.IsPathRooted</c>, so the raw value would resolve to
        ///         <c>/Resource/Static/Attachments</c> at the FILESYSTEM root instead of under
        ///         the content root. Legacy <c>Server.MapPath</c> always treated it as a virtual
        ///         path, resolving to <c>&lt;app&gt;/Resource/Static/Attachments</c> - which is
        ///         a real directory in the repository. Because the upload endpoint now creates
        ///         the directory if it is absent, the unnormalized form would not even fail
        ///         loudly: a container running as root would happily write the uploads somewhere
        ///         nothing can serve them from, and undo would delete from that same wrong
        ///         place. Re-prefixing with <c>~/</c> restores the original semantics on both
        ///         platforms.
        ///     </para>
        ///     <para>
        ///         NOTE: <see cref="FileSystemHelper.IsDirectoryWritable" /> (in
        ///         MixERP.Net.Common) calls <c>MapPath</c> on this same unnormalized value, so
        ///         the component's write-permission warning is currently evaluated against the
        ///         un-normalized path. The divergence is recorded in migration-summary.md
        ///         section 5 rather than fixed here, because that helper is shared by 43
        ///         dependent projects and is not this project's to change mid-flight.
        ///     </para>
        /// </summary>
        private static string GetPhysicalAttachmentsDirectory(string attachmentsDirectory)
        {
            if (string.IsNullOrWhiteSpace(attachmentsDirectory))
            {
                return null;
            }

            //"/Resource/Static/Attachments/" and "~/Resource/Static/Attachments/" both become
            //"~/Resource/Static/Attachments/", which MapPath resolves against the content root
            //on Windows and Linux alike.
            string virtualPath = "~/" + attachmentsDirectory.Trim().TrimStart('~').TrimStart('/', '\\');

            string physical = MixERPConfiguration.MapPath(virtualPath);

            return string.IsNullOrWhiteSpace(physical) ? null : Path.GetFullPath(physical);
        }

        /// <summary>
        ///     Reduces <paramref name="fileName" /> to its file component and combines it with
        ///     <paramref name="physicalDirectory" />, returning the full path only when it
        ///     genuinely sits inside that directory. Returns <c>null</c> for anything that
        ///     escapes it (or when the directory could not be resolved), which both callers treat
        ///     as "skip this entry".
        /// </summary>
        private static string ResolveInsideDirectory(string physicalDirectory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(physicalDirectory) || string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            string safeName = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(safeName))
            {
                return null;
            }

            string candidate = Path.GetFullPath(Path.Combine(physicalDirectory, safeName));

            string root = physicalDirectory.EndsWith(Path.DirectorySeparatorChar)
                ? physicalDirectory
                : physicalDirectory + Path.DirectorySeparatorChar;

            if (!candidate.StartsWith(root, StringComparison.Ordinal))
            {
                return null;
            }

            return candidate;
        }
    }
}
