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

namespace MixERP.Net.Common.Models
{
    /// <summary>
    ///     Text/value pair returned by the JSON services for drop-down lists. Replaces
    ///     System.Web.UI.WebControls.ListItem, keeping the members the scripts read
    ///     (Text, Value, Selected, Enabled).
    /// </summary>
    public sealed class ListItem
    {
        public ListItem()
        {
            this.Enabled = true;
        }

        public ListItem(string text, string value)
            : this()
        {
            this.Text = text;
            this.Value = value;
        }

        public string Text { get; set; }

        public string Value { get; set; }

        public bool Selected { get; set; }

        public bool Enabled { get; set; }
    }
}
