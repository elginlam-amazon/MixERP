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
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Entities.Core;
using MixERP.Net.WebControls.StockTransactionFactory.Helpers;
using Serilog;

namespace MixERP.Net.Core.Modules.BackOffice.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted <c>Services/Attachments.asmx</c>
    ///     <c>[ScriptService]</c> web service, called by
    ///     <c>wwwroot/js/AttachmentManager.ascx.js</c>.
    ///     <para>
    ///         All three former <c>[WebMethod]</c> bodies, and the private
    ///         <c>DeleteImage</c> helper, are carried over VERBATIM - including the
    ///         <c>"/Resource/Static/Attachments/"</c> URL PREFIX that
    ///         <c>Data.Attachments.GetAttachments</c> stamps onto every returned
    ///         <c>FilePath</c>. That prefix is what the caller feeds to the image handler's
    ///         <c>Path</c> query value and to the "Download" anchor, so it is a client
    ///         contract and is not "modernized".
    ///     </para>
    ///     <para>
    ///         <c>DeleteAttachment</c>'s <c>UserId &gt; 0</c> check is real authorization, not
    ///         a formality: it is what stops an unauthenticated principal deleting an
    ///         attachment file off disk, and <c>.RequireAuthorization()</c> only requires
    ///         THAT a user is signed in. It is reproduced exactly, returning <c>false</c>
    ///         rather than throwing, as the original did.
    ///     </para>
    ///     <para>
    ///         <c>Server.MapPath("~/Resource/Static/Attachments/" + filePath)</c> has no
    ///         counterpart; it became <c>PageUtility.MapPath</c>, which resolves an
    ///         application-relative path against the CONTENT ROOT exactly as
    ///         <c>HostingEnvironment.MapPath</c> did (<c>Resource/</c> is a content-root
    ///         directory and must stay unserved by static-file middleware). A CONTAINMENT
    ///         CHECK is added around it for the same reason the image handler needed one: the
    ///         deleted path comes from the database row, but the row's value is the file name
    ///         an UPLOAD supplied, so a traversal segment must not be able to reach outside
    ///         the attachments directory.
    ///     </para>
    /// </summary>
    public static class BackOfficeAttachmentsEndpoints
    {
        private const string GetAttachmentsRoute = "/services/backoffice/attachments/getattachments";
        private const string DeleteAttachmentRoute = "/services/backoffice/attachments/deleteattachment";
        private const string SaveRoute = "/services/backoffice/attachments/save";

        /// <summary>
        ///     The URL prefix <c>Data.Attachments.GetAttachments</c> stamps onto every returned
        ///     <c>FilePath</c>, carried over verbatim from the deleted web method.
        /// </summary>
        private const string AttachmentsUrlPrefix = "/Resource/Static/Attachments/";

        /// <summary>
        ///     The content-root-relative directory the attachment files live in, and the root
        ///     every delete must stay inside.
        /// </summary>
        private const string AttachmentsRoot = "Resource/Static/Attachments";

        public static IEndpointRouteBuilder MapBackOfficeAttachmentsService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(GetAttachmentsRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters = await AsmxCompatibility.ReadParametersAsync(context)
                        .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetAttachments(
                        parameters.GetString("book"), parameters.GetInt64("id")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(DeleteAttachmentRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters = await AsmxCompatibility.ReadParametersAsync(context)
                        .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(DeleteAttachment(parameters.GetInt64("id")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(SaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters = await AsmxCompatibility.ReadParametersAsync(context)
                        .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(Save(
                        parameters.GetString("book"), parameters.GetInt64("id"),
                        parameters.GetString("attachmentsJSON")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            return endpoints;
        }

        //-------------------------------------------------------------------------------
        // The former [WebMethod] bodies, carried over verbatim.
        //
        // AppUsers.GetCurrent() / GetCurrentUserDB() are KEPT here on purpose: a minimal-API
        // endpoint has a live HttpContext, which is the one place they resolve correctly.
        //-------------------------------------------------------------------------------

        private static bool DeleteAttachment(long id)
        {
            try
            {
                if (id <= 0)
                {
                    throw new ArgumentNullException("id");
                }

                if (AppUsers.GetCurrent().View.UserId.ToInt() > 0)
                {
                    return DeleteImage(Data.Attachments.DeleteReturningPath(AppUsers.GetCurrentUserDB(), id));
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Warning("Could not delete attachment. {Exception}", ex);
                throw;
            }
        }

        private static IEnumerable<Attachment> GetAttachments(string book, long id)
        {
            if (string.IsNullOrWhiteSpace(book))
            {
                throw new ArgumentNullException("book");
            }

            if (id <= 0)
            {
                throw new ArgumentNullException("id");
            }

            return Data.Attachments.GetAttachments(AppUsers.GetCurrentUserDB(), AttachmentsUrlPrefix, book, id);
        }

        private static bool Save(string book, long id, string attachmentsJSON)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(book))
                {
                    throw new ArgumentNullException("book");
                }

                if (id <= 0)
                {
                    throw new ArgumentNullException("id");
                }

                if (string.IsNullOrWhiteSpace(attachmentsJSON))
                {
                    throw new ArgumentNullException("attachmentsJSON");
                }

                Collection<Attachment> attachments = CollectionHelper.GetAttachmentCollection(attachmentsJSON);
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();

                return Data.Attachments.Save(AppUsers.GetCurrentUserDB(), userId, book, id, attachments);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save attachment. {Exception}", ex);
                throw;
            }
        }

        /// <summary>
        ///     Was <c>this.Server.MapPath("~/Resource/Static/Attachments/" + filePath)</c>
        ///     followed by <c>File.Delete</c>. The resolution moved to
        ///     <c>PageUtility.MapPath</c> (content root), and a CONTAINMENT CHECK was added so
        ///     a stored file name carrying traversal segments cannot delete a file outside the
        ///     attachments directory. The outcome for a legitimate path, and the
        ///     <c>IOException</c> warning-and-return-false path, are unchanged.
        /// </summary>
        private static bool DeleteImage(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            string root = PageUtility.MapPath(AttachmentsRoot);

            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            root = Path.GetFullPath(root);

            string resolved;

            try
            {
                resolved = Path.GetFullPath(Path.Combine(root, filePath));
            }
            catch (ArgumentException)
            {
                //An invalid path is not a file we own.
                Log.Warning("Refused to delete the malformed attachment path {FilePath}.", filePath);
                return false;
            }

            if (!IsContained(root, resolved))
            {
                Log.Warning("Refused to delete the attachment {FilePath}: it resolves outside {Root}.",
                    filePath, root);
                return false;
            }

            if (File.Exists(resolved))
            {
                try
                {
                    File.Delete(resolved);
                    return true;
                }
                catch (IOException)
                {
                    Log.Warning("Could not delete file: {FilePath}.", resolved);
                }
            }

            return false;
        }

        private static bool IsContained(string root, string candidate)
        {
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            + Path.DirectorySeparatorChar;

            return candidate.StartsWith(prefix, StringComparison.Ordinal);
        }
    }
}
