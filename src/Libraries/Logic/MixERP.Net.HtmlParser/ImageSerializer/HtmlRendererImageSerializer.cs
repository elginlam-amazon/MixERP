using PdfSharp;
using PdfSharp.Pdf;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    /// Serializes HTML with HtmlRenderer.
    /// The former HtmlRenderer.WinForms bitmap renderer only exists for Windows desktop targets, so the
    /// cross-platform HtmlRenderer.PdfSharp renderer is used instead and the document is written as a PDF file.
    /// </summary>
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        private const string Extension = ".pdf";

        internal override void CreateImage(string imagePath)
        {
            string path = imagePath + Extension;

            using (PdfDocument document = PdfGenerator.GeneratePdf(this.Html, PageSize.A4))
            {
                document.Save(path);
            }

            this.OnImageSaved(new ImageSavedEventArgs(path));

            base.CreateImage(imagePath);
        }
    }
}
