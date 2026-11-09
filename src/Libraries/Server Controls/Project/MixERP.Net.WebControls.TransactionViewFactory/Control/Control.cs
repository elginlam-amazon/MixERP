
namespace MixERP.Net.WebControls.TransactionViewFactory
{
            public sealed partial class TransactionView : WebControlBase
    {
        private dynamic panel;

        protected void CreateChildControls()
        {
            this.panel = new HtmlGenericControl();

            this.AddHeading(this.panel);
            this.AddButtons(this.panel);
            this.AddTopPanel(this.panel);
            this.AddGridPanel(this.panel);
            this.AddHiddenFields(this.panel);
            this.AddFlagControl(this.panel);

            this.SetDefaultValues();
            this.BindGrid();
            this.AddJavascript();

            this.Controls.Add(this.panel);
        }

        protected void RecreateChildControls()
        {
            this.EnsureChildControls();
        }

        protected void Render(dynamic w)
        {
            this.panel.RenderControl(w);
        }
    }
}