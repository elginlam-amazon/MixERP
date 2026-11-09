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


namespace MixERP.Net.WebControls.ScrudFactory
{
    public partial class ScrudForm : WebControlBase
    {
        private dynamic descriptionLabel;
        private dynamic titleLabel;

        private static void AddRuler(dynamic p)
        {
            if (p == null)
            {
                return;
            }

            using (dynamic ruler = new HtmlGenericControl("div")) //"hr"
            {
                ruler.Attributes.Add("class", "ui divider");
                p.Controls.Add(ruler);
            }
        }

        private void AddDescription(dynamic p)
        {
            this.descriptionLabel = new Label();
            this.descriptionLabel.Attributes.Add("style", "display:block;");
            p.Controls.Add(this.descriptionLabel);
        }

        private void AddTitle(dynamic p)
        {
            if (p == null)
            {
                return;
            }

            using (dynamic titleDiv = new HtmlGenericControl())
            {
                titleDiv.TagName = "div";
                this.titleLabel = new Label();
                this.titleLabel.ID = "TitleLabel";
                titleDiv.Attributes.Add("class", this.GetTitleLabelCssClass());

                titleDiv.Controls.Add(this.titleLabel);
                p.Controls.Add(titleDiv);
            }
        }

        private void LoadDescription()
        {
            if (!string.IsNullOrWhiteSpace(this.Description))
            {
                this.descriptionLabel.CssClass = this.GetDescriptionCssClass();
                this.descriptionLabel.Text = this.Description;
            }
        }

        private void LoadTitle()
        {
            this.titleLabel.Text = this.Text;
            this.Page.Title = this.Text;
        }
    }
}