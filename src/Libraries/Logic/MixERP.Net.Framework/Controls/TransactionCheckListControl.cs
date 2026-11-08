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

namespace MixERP.Net.Framework.Controls
{
    /// <summary>
    ///     The base of every transaction check-list control (checklist task 72).
    ///     <para>
    ///         THE ONE THING THE ORIGINAL DID was <c>this.ValidDateRequest =
    ///         ValidateRequestMode.Disabled;</c> in its constructor, i.e. it switched OFF ASP.NET
    ///         REQUEST VALIDATION for the check-list surface. That validation (the
    ///         "A potentially dangerous Request.Form value was detected" guard) is a WebForms/System.Web
    ///         feature that does not exist on modern .NET — there is nothing to disable, so there is no
    ///         line to port, and adding a modern "validation" of our own would CHANGE behaviour by
    ///         rejecting input the original deliberately accepted.
    ///     </para>
    ///     <para>
    ///         INTENT PRESERVED AS A DOCUMENTED CONTRACT: the transaction check-list surface accepts
    ///         RAW MARKUP INPUT. Check-list items carry html/angle-bracket content (they are rendered
    ///         from the TransactionChecklist configuration and from user-entered remarks), so:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             No derived control may reject or strip input because it contains <c>&lt;</c>,
    ///             <c>&gt;</c> or <c>&amp;</c>.
    ///         </item>
    ///         <item>
    ///             Blazor ENCODES interpolated values by default, so raw markup round-trips safely to
    ///             the browser as text. A derived control that wants the markup to be interpreted must
    ///             opt in explicitly with <c>MarkupString</c> — and that is the one place an injection
    ///             risk exists, so it must only ever be used on values the application itself produced,
    ///             never on unsanitized user input.
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The class stays abstract with the same NAME: the <c>Modules/*</c> Razor Class
    ///         Libraries derive from it. It was RELOCATED here from
    ///         <c>MixERP.Net.FrontEnd/Base/TransactionCheckListControl.cs</c> (namespace
    ///         <c>MixERP.Net.FrontEnd.Base</c>) so the modules no longer need a project reference to
    ///         the HOST — see <see cref="MixERPUserControl" /> for the full rationale.
    ///     </para>
    /// </summary>
    // Derives from ASP.NET Core ComponentBase, which is not CLS-compliant.
    [System.CLSCompliant(false)]
    public abstract class TransactionCheckListControl : MixERPUserControl
    {
    }
}
