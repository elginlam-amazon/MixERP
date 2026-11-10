using System;
using System.IO;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    ///     Saves the HTML to a uniquely named file under <see cref="TempDirectory" /> and raises
    ///     <see cref="ImageSaved" /> with its path.
    /// </summary>
    /// <remarks>
    ///     The .NET Framework version rasterised the HTML to a bitmap through Internet Explorer's
    ///     WebBrowser control or HtmlRenderer.WinForms. Both need Windows Forms/GDI+, which do
    ///     not exist on Linux, so the document is now kept as a self-contained HTML file. The
    ///     consumers (e-mail attachments) receive the file path exactly as before.
    /// </remarks>
    public class HtmlImageSerializer : IHtmlImageSerializer
    {
        public string TempDirectory { get; set; }
        public string Html { get; set; }
        public event EventHandler<ImageSavedEventArgs> ImageSaved;

        public void Serialize()
        {
            if (this.TempDirectory == null)
            {
                throw new MixERPException(Messages.TempDirectoryNullError);
            }

            string imagePath = WebContext.MapPath(this.TempDirectory + Guid.NewGuid());

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new MixERPException(Messages.CouldNotDetermineVirtualPathError);
            }

            this.CreateImage(imagePath);
        }

        internal virtual void CreateImage(string imagePath)
        {
            string directory = Path.GetDirectoryName(imagePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string path = imagePath + ".html";
            File.WriteAllText(path, this.Html ?? string.Empty);
            this.OnImageSaved(new ImageSavedEventArgs(path));
        }

        public void OnImageSaved(ImageSavedEventArgs e)
        {
            if (this.ImageSaved != null)
            {
                this.ImageSaved(this, e);
            }
        }
    }
}
