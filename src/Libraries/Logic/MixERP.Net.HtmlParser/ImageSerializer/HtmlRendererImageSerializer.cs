using System.IO;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    ///     Saves the HTML document so it can be sent as an attachment.
    /// </summary>
    /// <remarks>
    ///     The .NET Framework version rasterized the HTML to an image with HtmlRenderer.WinForms (GDI+, STA thread).
    ///     HtmlRenderer.WinForms only ships for Windows desktop target frameworks and cannot be used by the
    ///     cross-platform web application, so the rendered document is saved as an .html file instead.
    /// </remarks>
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            string path = imagePath + ".html";

            File.WriteAllText(path, this.Html);
            this.OnImageSaved(new ImageSavedEventArgs(path));

            base.CreateImage(imagePath);
        }
    }
}
