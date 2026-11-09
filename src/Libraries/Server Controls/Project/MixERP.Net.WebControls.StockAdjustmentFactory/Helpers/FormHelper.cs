
namespace MixERP.Net.WebControls.StockAdjustmentFactory.Helpers
{
    internal class FormHelper
    {
        internal static dynamic GetFields()
        {
            using (dynamic row = new HtmlGenericControl())
            {
                row.TagName = "div";
                row.Attributes.Add("class", "inline fields");

                return row;
            }
        }

        internal static dynamic GetField()
        {
            using (dynamic formGroup = new HtmlGenericControl())
            {
                formGroup.TagName = "div";
                formGroup.Attributes.Add("class", "small field");

                return formGroup;
            }
        }
    }
}
