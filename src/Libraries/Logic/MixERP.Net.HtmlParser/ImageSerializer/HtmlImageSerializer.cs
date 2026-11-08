using MixERP.Net.Common.Helpers;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using SixLabors.ImageSharp.Formats;
using System;
using System.IO;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    public class HtmlImageSerializer : IHtmlImageSerializer
    {
        public string TempDirectory { get; set; }
        public string Html { get; set; }
        [System.CLSCompliant(false)]
        public IImageFormat ImageFormat { get; set; }
        public event EventHandler<ImageSavedEventArgs> ImageSaved;

        public void Serialize()
        {
            if (this.TempDirectory == null)
            {
                throw new MixERPException(Messages.TempDirectoryNullError);
            }

            //HostingEnvironment.MapPath is a System.Web API with no net10.0 equivalent. The
            //MixERPConfiguration seam (initialized once by the host from IWebHostEnvironment)
            //resolves the same "~/Resource/Temp/Images/" style virtual path against the content
            //root, and normalizes separators for case-sensitive Linux filesystems.
            string imagePath = MixERPConfiguration.MapPath(this.TempDirectory + Guid.NewGuid());

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new MixERPException(Messages.CouldNotDetermineVirtualPathError);
            }

            //IIS created the temp directory for the Framework app; a self-hosted ASP.NET Core
            //process has to ensure it exists before anything is written into it.
            string directory = Path.GetDirectoryName(imagePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            this.CreateImage(imagePath);
        }

        internal virtual void CreateImage(string imagePath)
        {
        }

        public void OnImageSaved(ImageSavedEventArgs e)
        {
            EventHandler<ImageSavedEventArgs> handler = this.ImageSaved;

            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
