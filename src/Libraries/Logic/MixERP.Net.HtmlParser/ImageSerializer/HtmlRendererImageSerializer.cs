using PdfSharp;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    /// Renders the HTML snapshot with HtmlRenderer.
    /// HtmlRenderer.WinForms (GDI+/Windows Forms, STA thread) is not available to a .NET 10 library,
    /// so the snapshot is rendered with the cross-platform HtmlRenderer.PdfSharp adapter and saved as a PDF document.
    /// </summary>
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        private const string Extension = ".pdf";

        static HtmlRendererImageSerializer()
        {
            // Resolve fonts from the Windows font directory when running on Windows (the IIS deployment target).
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }

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
