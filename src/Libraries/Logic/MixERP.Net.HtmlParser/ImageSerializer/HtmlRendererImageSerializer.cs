using PdfSharp;
using PdfSharp.Pdf;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    ///     Serializes HTML into a document file using HtmlRenderer.
    ///     The WinForms (GDI) image renderer is Windows-only on .NET, so the HTML is rendered
    ///     to a PDF document with the cross-platform HtmlRenderer.PdfSharp renderer instead.
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

            OnImageSaved(new ImageSavedEventArgs(path));
            base.CreateImage(imagePath);
        }
    }
}
