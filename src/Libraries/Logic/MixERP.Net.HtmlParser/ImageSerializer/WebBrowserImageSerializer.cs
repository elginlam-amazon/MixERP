using MixERP.Net.Common.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    ///     The <see cref="ParserType.IEWebBrowser" /> serializer. It used to host the Windows-only
    ///     Internet Explorer <c>System.Windows.Forms.WebBrowser</c> control on an STA thread,
    ///     navigate it to a temporary .html file, spin on <c>ReadyState</c> with
    ///     <c>Application.DoEvents()</c> and then <c>DrawToBitmap</c> the full document. Headless
    ///     Chromium now does the navigation and rasterization, and the distinctive post-processing
    ///     of this serializer - the temporary file, the 100 pixel side margins and top allowance,
    ///     the white canvas and the 3200 DPI metadata - is preserved below.
    /// </summary>
    public sealed class WebBrowserImageSerializer : HtmlImageSerializer
    {
        private const int Margin = 100;
        private const double Resolution = 3200;

        internal override void CreateImage(string imagePath)
        {
            string tempPath = imagePath + ".html";

            File.WriteAllText(tempPath, this.Html);

            byte[] rendered;

            try
            {
                //The legacy canvas width came from browser.Width (the un-sized WebBrowser control's
                //default, since its Width/Height assignments were commented out). The width is
                //configurable through HtmlRenderingEngine.ViewportWidth so the legacy output
                //dimensions can be pinned if a layout regresses.
                rendered = HtmlRenderingEngine.RenderFile(tempPath, HtmlRenderingEngine.ViewportWidth, 1);
            }
            finally
            {
                //The original deleted the navigated temp file once the document had loaded.
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }

            string path = imagePath + ImageHelper.GetFileExtension(this.ImageFormat);

            using (Image content = Image.Load(rendered))
            {
                int width = content.Width;
                int height = content.Height + Margin;

                using (Image<Rgba32> canvas = new Image<Rgba32>(width + 2 * Margin, height))
                {
                    canvas.Mutate(context =>
                    {
                        context.BackgroundColor(Color.White);
                        context.DrawImage(content, new Point(Margin, 0), 1f);
                    });

                    canvas.Metadata.HorizontalResolution = Resolution;
                    canvas.Metadata.VerticalResolution = Resolution;

                    IImageEncoder encoder = ImageHelper.GetEncoder(this.ImageFormat) ?? new PngEncoder();
                    canvas.Save(path, encoder);
                }
            }

            this.OnImageSaved(new ImageSavedEventArgs(path));

            base.CreateImage(imagePath);
        }
    }
}
