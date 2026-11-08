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
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;
using Serilog;
using SixLabors.ImageSharp;

namespace MixERP.Net.Core.Modules.BackOffice.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted
    ///     <c>Handlers/ImageHandler.ashx</c> + <c>.ashx.cs</c> (<c>IHttpHandler</c>, which has
    ///     no modern counterpart). It is the resizing image proxy the attachment manager's
    ///     cards and modal use.
    ///     <para>
    ///         THE URL IS PRESERVED EXACTLY, because
    ///         <c>wwwroot/js/AttachmentManager.ascx.js</c> builds it in two places by hand:
    ///         <c>"/Modules/BackOffice/Handlers/ImageHandler.ashx?Path=" + file.FilePath +
    ///         "&amp;H=300&amp;W=250"</c> (<c>LoadImageGroup</c>) and
    ///         <c>"...ImageHandler.ashx?&amp;H=500&amp;W=900&amp;Path=" + filePath</c>
    ///         (<c>initializeModal</c>). The three query names <c>Path</c>, <c>W</c>, <c>H</c>
    ///         and all four cache headers are preserved with them, so that file needed no
    ///         change.
    ///     </para>
    ///     <para>
    ///         ⚠ SECURITY FIX - PATH CONTAINMENT. The infrastructure pass flagged a real
    ///         vulnerability: the <c>Path</c> query value is ATTACKER-CONTROLLED and the legacy
    ///         handler fed it straight into <c>HttpContext.Current.Server.MapPath(path)</c>
    ///         (three call sites), so <c>?Path=/web.config</c> or a traversal value let any
    ///         caller read an arbitrary file under the application root, re-encoded as an
    ///         image. The resolution now goes through <see cref="ResolveContainedPath" />,
    ///         which canonicalizes with <c>Path.GetFullPath</c> and REJECTS anything that does
    ///         not land inside the attachments root the attachment service hands out
    ///         (<c>Resource/Static/Attachments</c>) - the only directory this handler has ever
    ///         legitimately served, since every <c>FilePath</c> it is called with comes from
    ///         <c>Data.Attachments.GetAttachments</c>'s <c>/Resource/Static/Attachments/</c>
    ///         prefix. A rejected path answers <c>404</c>, which is the same "nothing is
    ///         written" outcome the legacy handler produced for a non-existent file. This also
    ///         keeps the host's rule that <c>Resource/**</c> is never served by static-file
    ///         middleware intact: the bytes only ever leave through this authorized endpoint.
    ///     </para>
    ///     <para>
    ///         <c>System.Drawing.Bitmap</c> is gone (unsupported on Linux):
    ///         <c>Image.Identify</c> returning null replaces the <c>IsValidBitmap</c>
    ///         try/catch, and <c>Image.Load</c> replaces <c>new Bitmap(path)</c>.
    ///         <c>ImageHelper.GetResizedImage</c> / <c>GetContentType</c> in
    ///         <c>MixERP.Net.Common</c> are ALREADY ImageSharp-based, so the resize pipeline
    ///         and the content-type map are unchanged.
    ///     </para>
    ///     <para>
    ///         The <c>document.png</c> fallback keeps its behaviour, with the trap the
    ///         infrastructure pass recorded avoided: the asset is resolved through
    ///         <c>IWebHostEnvironment.WebRootPath</c>, NOT by handing the leading-slash literal
    ///         <c>"/static/images/document.png"</c> to a <c>MapPath</c>-style helper (on Linux
    ///         that is an ABSOLUTE path and would resolve to the filesystem root).
    ///     </para>
    ///     <para>
    ///         <c>.RequireAuthorization()</c> matches the deleted <c>Modules/web.config</c>'s
    ///         <c>&lt;deny users="?" /&gt;</c>, which covered this handler's path - attachments
    ///         are not public.
    ///     </para>
    /// </summary>
    public static class BackOfficeImageHandlerEndpoint
    {
        /// <summary>
        ///     The url <c>wwwroot/js/AttachmentManager.ascx.js</c> builds. Do not change it
        ///     without changing that file in the same commit.
        /// </summary>
        public const string ImageHandlerRoute = "/Modules/BackOffice/Handlers/ImageHandler.ashx";

        /// <summary>
        ///     The ONLY directory this handler serves: the attachments root, content-root
        ///     relative. Every <c>Path</c> value the caller sends is a
        ///     <c>Data.Attachments.GetAttachments</c> <c>FilePath</c>, which is this directory
        ///     plus a file name.
        /// </summary>
        private const string AttachmentsRoot = "Resource/Static/Attachments";

        /// <summary>The placeholder served when the file is not a readable bitmap.</summary>
        private const string PlaceholderWebPath = "static/images/document.png";

        public static IEndpointRouteBuilder MapBackOfficeImageHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet(ImageHandlerRoute, async (HttpContext context) =>
            {
                IWebHostEnvironment environment = context.RequestServices
                    .GetRequiredService<IWebHostEnvironment>();

                await ProcessImageAsync(context, environment).ConfigureAwait(false);
            })
                .RequireAuthorization();

            return endpoints;
        }

        /// <summary>
        ///     Port of the handler's <c>ProcessImage(HttpContext)</c>, step for step.
        /// </summary>
        private static async Task ProcessImageAsync(HttpContext context, IWebHostEnvironment environment)
        {
            string path = string.Empty;
            int width = 0;
            int height = 0;

            if (!string.IsNullOrWhiteSpace(context.Request.Query["Path"]))
            {
                path = Conversion.TryCastString(context.Request.Query["Path"].ToString());
            }

            if (!string.IsNullOrWhiteSpace(context.Request.Query["W"]))
            {
                width = Convert.ToInt32(context.Request.Query["W"].ToString(), CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(context.Request.Query["H"]))
            {
                height = Convert.ToInt32(context.Request.Query["H"].ToString(), CultureInfo.InvariantCulture);
            }

            //Was context.Response.Clear(): nothing has been written yet on a minimal-API
            //endpoint, so there is nothing to clear.
            string resolved = ResolveContainedPath(path);

            if (resolved == null || !File.Exists(resolved))
            {
                //The legacy handler simply wrote NOTHING when the file did not exist, which the
                //browser saw as an empty 200. A 404 is the honest equivalent and is what a
                //rejected (out-of-root) path also gets - an <img> renders its broken-image
                //placeholder either way.
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            //file.Extension of the REQUESTED file decides the content type, even when the
            //placeholder ends up being served - that is the original's behaviour and it is kept.
            FileInfo file = new FileInfo(resolved);

            string imagePath = resolved;

            if (!IsValidBitmap(imagePath))
            {
                imagePath = Path.Combine(environment.WebRootPath ?? string.Empty,
                    PlaceholderWebPath.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(imagePath))
                {
                    Log.Warning("The image placeholder {Placeholder} was not found.", imagePath);
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }

            byte[] buffer;

            using (Image originalImage = await Image.LoadAsync(imagePath).ConfigureAwait(false))
            {
                buffer = ImageHelper.GetResizedImage(originalImage, width, height);
            }

            if (buffer == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            //The four cache headers of the original, in the same terms:
            //  Cache.SetCacheability(Public)                 -> "public"
            //  Cache.SetExpires(DateTime.Now.AddMonths(1))   -> Expires
            //  Cache.SetMaxAge(new TimeSpan(30,0,0,0,0))     -> max-age=2592000
            //  AddHeader("Last-Modified", ToShortDateString())
            context.Response.ContentType = ImageHelper.GetContentType(file.Extension);
            context.Response.Headers["Cache-Control"] = "public, max-age=2592000";
            context.Response.Headers["Expires"] =
                DateTime.Now.AddMonths(1).ToString("R", CultureInfo.InvariantCulture);
            context.Response.Headers["Last-Modified"] = DateTime.Now.ToShortDateString();

            await context.Response.Body.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        }

        /// <summary>
        ///     ⚠ THE CONTAINMENT CHECK. Resolves the attacker-controlled <c>Path</c> query value
        ///     to a physical path and returns it ONLY when it canonicalizes to a location
        ///     strictly inside the attachments root; otherwise <c>null</c> (refused).
        ///     <para>
        ///         Three things make this safe where <c>Server.MapPath(path)</c> was not:
        ///         the value is forced to be root-relative (a rooted or UNC value is refused
        ///         outright rather than honoured), it is canonicalized with
        ///         <c>Path.GetFullPath</c> so <c>..</c> segments and mixed separators collapse
        ///         BEFORE the comparison, and the comparison is a prefix test against the root
        ///         plus a directory separator so a sibling directory whose name merely starts
        ///         with the root's name ("Attachments.bak") cannot pass.
        ///     </para>
        /// </summary>
        private static string ResolveContainedPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string root = PageUtility.MapPath(AttachmentsRoot);

            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }

            root = Path.GetFullPath(root);

            //The caller sends "/Resource/Static/Attachments/<file>" (the prefix
            //Data.Attachments.GetAttachments stamps on). Strip the leading separator and the
            //app-relative "~" so the value is always combined with the root, never treated as
            //an absolute path.
            string relative = path.Trim().Replace('\\', '/');

            if (relative.StartsWith("~", StringComparison.Ordinal))
            {
                relative = relative.Substring(1);
            }

            relative = relative.TrimStart('/');

            //Honour the documented prefix when it is present, so both forms resolve.
            const string Prefix = "Resource/Static/Attachments/";

            if (relative.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                relative = relative.Substring(Prefix.Length);
            }

            if (string.IsNullOrWhiteSpace(relative))
            {
                return null;
            }

            string candidate;

            try
            {
                candidate = Path.GetFullPath(Path.Combine(root,
                    relative.Replace('/', Path.DirectorySeparatorChar)));
            }
            catch (ArgumentException)
            {
                Log.Warning("Refused the malformed image path {Path}.", path);
                return null;
            }
            catch (NotSupportedException)
            {
                Log.Warning("Refused the malformed image path {Path}.", path);
                return null;
            }

            string containmentPrefix =
                root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!candidate.StartsWith(containmentPrefix, StringComparison.Ordinal))
            {
                Log.Warning("Refused the image path {Path}: it resolves outside {Root}.", path, root);
                return null;
            }

            return candidate;
        }

        /// <summary>
        ///     Replaces the <c>new Bitmap(path)</c> try/catch. <c>Image.Identify</c> reads only
        ///     the header and returns null (or throws <c>UnknownImageFormatException</c>) when
        ///     the file is not a decodable image, which is exactly the condition the original
        ///     caught.
        /// </summary>
        private static bool IsValidBitmap(string path)
        {
            try
            {
                return Image.Identify(path) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
