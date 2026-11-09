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

namespace MixERP.Net.WebControls.ScrudFactory.Controls
{
    internal sealed class CommandPanel: IDisposable
    {
        private dynamic commandPanel;
        public event EventHandler DeleteButtonClick;

        public event EventHandler EditButtonClick;

        public string AddButtonIconCssClass { get; set; }
        public string AllButtonIconCssClass { get; set; }
        public string ButtonCssClass { get; set; }
        public string CompactButtonIconCssClass { get; set; }
        public string CssClass { get; set; }
        public Button DeleteButton { get; private set; }
        public string DeleteButtonIconCssClass { get; set; }
        public Button EditButton { get; private set; }
        public string EditButtonIconCssClass { get; set; }
        public string PrintButtonIconCssClass { get; set; }
        public string SelectButtonIconCssClass { get; set; }

        public dynamic GetCommandPanel(string controlSuffix)
        {
            this.commandPanel = new HtmlGenericControl("div");
            this.commandPanel.Attributes.Add("role", "toolbar");
            this.commandPanel.Attributes.Add("class", this.CssClass);
            this.AddSelectButton(this.commandPanel);
            this.AddShowCompactButton(this.commandPanel);
            this.AddShowAllButton(this.commandPanel);
            this.AddAddButton(this.commandPanel);
            this.AddEditButtonHidden(this.commandPanel, controlSuffix);
            this.AddEditButtonVisible(this.commandPanel, controlSuffix);
            this.AddDeleteButtonHidden(this.commandPanel, controlSuffix);
            this.AddDeleteButtonVisible(this.commandPanel, controlSuffix);
            this.AddPrintButton(this.commandPanel);
            return this.commandPanel;
        }

        private void AddAddButton(dynamic p)
        {
            using (HtmlGenericControl addButton = this.GetInputButton("CTRL + SHIFT + A", "return(scrudAddNew());", Titles.AddNew, this.ButtonCssClass, this.AddButtonIconCssClass))
            {
                p.Controls.Add(addButton);
            }
        }

        private void AddDeleteButtonHidden(dynamic p, string controlSuffix)
        {
            this.DeleteButton = new Button();
            this.DeleteButton.ID = "DeleteButton" + controlSuffix;
            this.DeleteButton.CssClass = "hidden";
            this.DeleteButton.CausesValidation = false;
            this.DeleteButton.Click += this.OnDeleteButtonClick;
            p.Controls.Add(this.DeleteButton);
        }

        private void AddDeleteButtonVisible(dynamic p, string controlSuffix)
        {
            using (HtmlGenericControl deleteButton = this.GetInputButton("CTRL + SHIFT + D", "$('#DeleteButton" + controlSuffix + "').click();return false;", Titles.DeleteSelected, this.ButtonCssClass, this.DeleteButtonIconCssClass))
            {
                p.Controls.Add(deleteButton);
            }
        }

        private void AddEditButtonHidden(dynamic p, string controlSuffix)
        {
            this.EditButton = new Button();
            this.EditButton.ID = "EditButton" + controlSuffix;
            this.EditButton.CssClass = "hidden";
            this.EditButton.Click += this.OnEditButtonClick;
            p.Controls.Add(this.EditButton);
        }

        private void AddEditButtonVisible(dynamic p, string controlSuffix)
        {
            using (HtmlGenericControl editButton = this.GetInputButton("CTRL + SHIFT + E", "$('#EditButton" + controlSuffix + "').click();return false;", Titles.EditSelected, this.ButtonCssClass, this.EditButtonIconCssClass))
            {
                p.Controls.Add(editButton);
            }
        }

        private void AddPrintButton(dynamic p)
        {
            using (HtmlGenericControl printButton = this.GetInputButton("CTRL + SHIFT + P", "scrudPrintGridView();", Titles.Print, this.ButtonCssClass, this.PrintButtonIconCssClass))
            {
                p.Controls.Add(printButton);
            }
        }

        private void AddSelectButton(dynamic p)
        {
            if (IsModal())
            {
                using (HtmlGenericControl addSelectButton = this.GetInputButton("RETURN", "scrudSelectAndClose();", Titles.Select, this.ButtonCssClass, this.SelectButtonIconCssClass))
                {
                    p.Controls.Add(addSelectButton);
                }
            }
        }

        private void AddShowAllButton(dynamic p)
        {
            using (HtmlGenericControl showAllButton = this.GetInputButton("CTRL + SHIFT + S", "scrudShowAll();", Titles.ShowAll, this.ButtonCssClass, this.AllButtonIconCssClass))
            {
                p.Controls.Add(showAllButton);
            }
        }

        private void AddShowCompactButton(dynamic p)
        {
            using (HtmlGenericControl showCompactButton = this.GetInputButton("CTRL + SHIFT + C", "scrudShowCompact();", Titles.ShowCompact, this.ButtonCssClass, this.CompactButtonIconCssClass))
            {
                p.Controls.Add(showCompactButton);
            }
        }

        private dynamic GetButton(string toolTip, string onClientClick, string text)
        {
            using (dynamic button = new Button())
            {
                button.CssClass = this.ButtonCssClass;
                button.ToolTip = toolTip;
                button.OnClientClick = onClientClick;
                button.Text = text;
                return button;
            }
        }

        private HtmlGenericControl GetInputButton(string title, string onclick, string value, string cssClass, string iconCssClass)
        {
            using (HtmlButton inputButton = new HtmlButton())
            {
                inputButton.Attributes.Add("class", cssClass);
                inputButton.Attributes.Add("type", "button");
                inputButton.Attributes.Add("title", title);
                inputButton.Attributes.Add("onclick", onclick);

                if (!string.IsNullOrWhiteSpace(iconCssClass))
                {
                    inputButton.InnerHtml = @"<i class='" + iconCssClass + "'></i> " + value;
                }
                else
                {
                    inputButton.InnerHtml = value;
                }

                return inputButton;
            }
        }

        private static bool IsModal()
        {
            return false;
        }

        private void OnDeleteButtonClick(dynamic sender, EventArgs e)
        {
            if (this.DeleteButtonClick != null)
            {
                this.DeleteButtonClick(this, e);
            }
        }

        private void OnEditButtonClick(dynamic sender, EventArgs e)
        {
            if (this.EditButtonClick != null)
            {
                this.EditButtonClick(this, e);
            }
        }

        #region IDisposable

        private bool disposed;

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Dispose(true);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (this.commandPanel != null)
            {
                this.commandPanel.Dispose();
                this.commandPanel = null;
            }

            if (this.EditButton != null)
            {
                this.EditButton.Click -= this.OnEditButtonClick;
                this.EditButtonClick = null;
                this.EditButton.Dispose();
                this.EditButton = null;
            }

            if (this.DeleteButton != null)
            {
                this.DeleteButton.Click -= this.OnDeleteButtonClick;
                this.DeleteButtonClick = null;
                this.DeleteButton.Dispose();
                this.DeleteButton = null;
            }


            this.disposed = true;
        }

        #endregion
    }
}