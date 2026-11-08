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

using MixERP.Net.i18n.Resources;
using System;
using System.Linq;
using System.Threading;

namespace MixERP.Net.WebControls.ScrudFactory.Controls.ListControls
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Shared option-list builder for the two list renderers (drop-down and radio-button list).
    ///     <para>
    ///         The original took a <c>System.Web.UI.WebControls.ListControl</c> and pushed
    ///         <c>ListItem</c>s into its <c>Items</c> collection. Both types are removed on net10.0,
    ///         so it now fills <see cref="ScrudField.Items" /> with
    ///         <see cref="ScrudListItem" />s — the same text/value pairs, in the same order, with
    ///         the same selection semantics.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static class Helper
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddListItems(ListControl, keys, values, selectedValues)</c>, preserving:
        ///     <list type="bullet">
        ///         <item>the key/value count mismatch throwing with <c>Errors.KeyValueMismatch</c>;</item>
        ///         <item><c>new ListItem(key[i].Trim(), value[i].Trim())</c> — the KEY is the option's
        ///         TEXT and the VALUE is what is posted (this is how <c>Titles.Yes</c>/<c>Titles.No</c>
        ///         map onto <c>true</c>/<c>false</c> for a boolean column);</item>
        ///         <item>the single-selection branch, which matched only the LAST comma-separated
        ///         entry of <paramref name="selectedValues" /> and stopped at the first hit.</item>
        ///     </list>
        ///     <para>
        ///         The <c>control is CheckBoxList</c> branch (multi-select, matching ANY of the
        ///         comma-separated values) is expressed by
        ///         <paramref name="allowMultipleSelection" />: no scrud renderer ever built a
        ///         <c>CheckBoxList</c>, so the branch was dead in this project, but it is carried
        ///         rather than dropped because it is real logic the helper performed.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static void AddListItems(ScrudField field, string keys, string values, string selectedValues, bool allowMultipleSelection)
        {
            if (field == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(keys))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(values))
            {
                return;
            }

            char separator = ",".ToString(Thread.CurrentThread.CurrentCulture).ToCharArray()[0];

            string[] key = keys.Split(separator);
            string[] value = values.Split(',');

            if (key.Count() != value.Count())
            {
                throw new InvalidOperationException(Errors.KeyValueMismatch);
            }

            for (int i = 0; i < key.Length; i++)
            {
                ScrudListItem item = new ScrudListItem(key[i].Trim(), value[i].Trim());
                field.Items.Add(item);
            }

            foreach (ScrudListItem item in field.Items)
            {
                if (allowMultipleSelection)
                {
                    if (!string.IsNullOrWhiteSpace(selectedValues))
                    {
                        foreach (string selectedValue in selectedValues.Split(','))
                        {
                            if (item.Value.Trim().Equals(selectedValue.Trim()))
                            {
                                item.Selected = true;
                            }
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(selectedValues))
                    {
                        if (item.Value.Trim().Equals(selectedValues.Split(',').Last().Trim()))
                        {
                            item.Selected = true;
                            break;
                        }
                    }
                }
            }

            //A ListControl surfaced its chosen option through SelectedValue; ScrudField carries it
            //in Value, which is what GetFormCollection reads back.
            ScrudListItem selectedItem = field.Items.FirstOrDefault(item => item.Selected);

            if (selectedItem != null)
            {
                field.Value = selectedItem.Value;
            }
        }

        /// <summary>
        ///     Convenience overload matching the original three-argument call shape (single
        ///     selection, as every scrud list renderer used).
        /// </summary>
        internal static void AddListItems(ScrudField field, string keys, string values, string selectedValues)
        {
            AddListItems(field, keys, values, selectedValues, false);
        }
    }
}
