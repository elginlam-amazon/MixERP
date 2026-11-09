using MixERP.Net.Common;

namespace MixERP.Net.WebControls.Flag
{
    public partial class FlagControl : WebControlBase
    {
        private void AddScript()
        {
            string script = "$(document).ready(function() {" +
                            string.Format("var flagPopunder = $('#{0}');var flagButton = $('#{1}');", this.ID, this.AssociatedControlId) +
                            "flagButton.click(function () {" +
                            "popUnder(flagPopunder, flagButton);" +
                            "});});";

            // Stubbed: PageUtility.RegisterJavascript removed for net10.0 compatibility
            _ = script;
        }
    }
}