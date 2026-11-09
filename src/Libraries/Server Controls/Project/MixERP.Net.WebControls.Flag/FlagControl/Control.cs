using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.Flag.Data;

namespace MixERP.Net.WebControls.Flag
{
    public sealed partial class FlagControl : WebControlBase
    {
        private dynamic container;
        private dynamic updateButton;

        protected override void CreateChildControls()
        {
            // Stubbed: WebForms control tree creation removed for net10.0 compatibility
        }

        protected override void RecreateChildControls()
        {
            this.EnsureChildControls();
        }

        protected override void Render(object w) { }

        private void BindFlagTypeDropDownList()
        {
            if (this.flagDropDownlist == null) return;
            this.flagDropDownlist.DataSource = FlagType.GetFlagTypes(this.Catalog);
            this.flagDropDownlist.DataTextField = "FlagTypeName";
            this.flagDropDownlist.DataValueField = "FlagTypeId";
            this.flagDropDownlist.DataBind();
        }

        private void Initialize()
        {
            // Stubbed: WebForms HTML control building removed for net10.0 compatibility
        }
    }
}
