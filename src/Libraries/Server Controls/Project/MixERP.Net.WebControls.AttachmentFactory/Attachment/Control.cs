
namespace MixERP.Net.WebControls.AttachmentFactory
{
        public sealed partial class Attachment : WebControlBase
    {
        public Attachment(string catalog)
        {
            this.Catalog = catalog;
        }

        protected void CreateChildControls()
        {
            this.placeHolder = new PlaceHolder();
            this.CreateUploadPanel(this.placeHolder);
            this.CreateButtons(this.placeHolder);
            this.CreateWarningLabel(this.placeHolder);
            this.CreateHiddenFields(this.placeHolder);
            this.RegisterJavascript();
            this.AddJavascript();
            this.CheckPermission();
            
            this.Controls.Add(this.placeHolder);
        }

        protected void RecreateChildControls()
        {
            this.EnsureChildControls();
        }

        protected void Render(dynamic w)
        {
            this.EnsureChildControls();
            this.placeHolder.RenderControl(w);
        }
    }
}