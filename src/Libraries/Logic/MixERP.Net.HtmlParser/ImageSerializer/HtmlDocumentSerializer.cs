using System.IO;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    ///     Saves the HTML as a standalone .html document.
    ///     The previous HtmlRenderer.WinForms / WebBrowser based image serializers depend on Windows Forms and GDI+,
    ///     which are not available on cross-platform .NET, so the rendered transaction is now delivered as an HTML file.
    /// </summary>
    public sealed class HtmlDocumentSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            string path = imagePath + ".html";
            File.WriteAllText(path, this.Html ?? string.Empty);
            this.OnImageSaved(new ImageSavedEventArgs(path));

            base.CreateImage(imagePath);
        }
    }
}
