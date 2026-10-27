using PdfSharp;
using PdfSharp.Pdf;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    ///     Renders the HTML snapshot with HtmlRenderer. The GDI/WinForms renderer (HtmlRenderer.WinForms,
    ///     HTML to bitmap on an STA thread) is not available on .NET 10, so the snapshot is rendered as a PDF document
    ///     with the cross-platform HtmlRenderer.PdfSharp renderer.
    /// </summary>
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            string path = imagePath + ".pdf";

            using (PdfDocument document = PdfGenerator.GeneratePdf(this.Html, PageSize.A4))
            {
                document.Save(path);
            }

            this.OnImageSaved(new ImageSavedEventArgs(path));

            base.CreateImage(imagePath);
        }
    }
}
