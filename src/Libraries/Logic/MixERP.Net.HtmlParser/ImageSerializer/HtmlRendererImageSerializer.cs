using System.IO;
using System.Text;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    ///     Saves the HTML as a standalone HTML document which is attached to e-mails.
    ///     The former implementation rasterized the HTML with HtmlRenderer.WinForms (System.Drawing/GDI+),
    ///     which is Windows-only and not available on modern .NET; the HTML document preserves the full layout.
    /// </summary>
    public sealed class HtmlRendererImageSerializer : HtmlImageSerializer
    {
        internal override void CreateImage(string imagePath)
        {
            string path = imagePath + ".html";
            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, this.Html ?? string.Empty, Encoding.UTF8);
            this.OnImageSaved(new ImageSavedEventArgs(path));
            base.CreateImage(imagePath);
        }
    }
}
