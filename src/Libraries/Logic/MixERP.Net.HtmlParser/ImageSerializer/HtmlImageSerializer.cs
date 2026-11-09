using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using System;
using System.Drawing.Imaging;
using System.IO;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    public class HtmlImageSerializer : IHtmlImageSerializer
    {
        public string TempDirectory { get; set; }
        public string Html { get; set; }
        public ImageFormat ImageFormat { get; set; }
        public event EventHandler<ImageSavedEventArgs> ImageSaved;

        public void Serialize()
        {
            if (this.TempDirectory == null)
            {
                throw new MixERPException(Messages.TempDirectoryNullError);
            }

            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                this.TempDirectory.TrimStart('/').Replace('/', Path.DirectorySeparatorChar),
                Guid.NewGuid().ToString());

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new MixERPException(Messages.CouldNotDetermineVirtualPathError);
            }

            this.CreateImage(imagePath);
        }

        internal virtual void CreateImage(string imagePath)
        {
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
