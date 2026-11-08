/********************************************************************************
Copyright (C) Binod Nepal, Mix Open Foundation (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/

using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace MixERP.Net.Common.Helpers
{
    public static class ImageHelper
    {
        public static string GetContentType(string extension)
        {
            switch (extension)
            {
                case ".bmp": return "Image/bmp";
                case ".gif": return "Image/gif";
                case ".jpg": return "Image/jpeg";
                case ".jpeg": return "Image/jpeg";
                case ".png": return "Image/png";
                default: return "text/plain";
            }
        }

        /// <summary>
        /// Returns the encoder registered for the supplied image format. Replaces the
        /// System.Drawing ImageCodecInfo lookup, which is Windows-only and removed here.
        /// </summary>
        public static IImageEncoder GetEncoder(IImageFormat format)
        {
            if (format == null)
            {
                return null;
            }

            try
            {
                return Configuration.Default.ImageFormatsManager.GetEncoder(format);
            }
            catch (NotSupportedException)
            {
                //No encoder registered for this format, mirroring the original null return.
                return null;
            }
        }

        /// <summary>
        /// Returns the primary file extension of the supplied format, upper-cased and dot
        /// prefixed (for example ".JPG"), exactly as the System.Drawing version did.
        /// </summary>
        public static string GetFileExtension(IImageFormat format)
        {
            if (format == null)
            {
                return null;
            }

            string extension = format.FileExtensions.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(extension))
            {
                return null;
            }

            if (!extension.StartsWith(".", StringComparison.Ordinal))
            {
                extension = "." + extension;
            }

            return extension.ToUpperInvariant();
        }

        public static IImageFormat GetImageFormat(String path)
        {
            switch (Path.GetExtension(path))
            {
                case ".bmp": return BmpFormat.Instance;
                case ".gif": return GifFormat.Instance;
                case ".jpg": return JpegFormat.Instance;
                case ".png": return PngFormat.Instance;
            }
            return JpegFormat.Instance;
        }

        public static byte[] GetResizedImage(Image image, int width, int height)
        {
            if (image == null)
            {
                return null;
            }

            Size thumbnailSize = new Size(width, height);

            if (thumbnailSize.Width.Equals(0))
            {
                thumbnailSize.Width = image.Size.Width;
            }

            if (thumbnailSize.Height.Equals(0))
            {
                thumbnailSize.Height = image.Size.Height;
            }

            //The original pipeline scaled the source with CalculateScalingRatio (which always
            //covers the thumbnail box) and drew it onto a thumbnail-sized canvas, cropping the
            //overflow - and for portrait overflow it biased the crop towards the top of the
            //image. ResizeMode.Crop with an anchor reproduces that behaviour, and the
            //HighQualityBicubic/HighQuality hints map onto the bicubic resampler.
            float scalingRatio = CalculateScalingRatio(image.Size, thumbnailSize);

            int scaledWidth = (int)Math.Round(image.Size.Width * scalingRatio);
            int scaledHeight = (int)Math.Round(image.Size.Height * scalingRatio);

            AnchorPositionMode anchor = AnchorPositionMode.Center;

            if (scaledWidth < scaledHeight && scaledHeight > thumbnailSize.Height)
            {
                anchor = AnchorPositionMode.Top;
            }

            ResizeOptions options = new ResizeOptions
            {
                Size = thumbnailSize,
                Mode = ResizeMode.Crop,
                Position = anchor,
                Sampler = KnownResamplers.Bicubic
            };

            //Clone so the caller's image (and its frames/metadata) is left untouched, exactly
            //as the original did by rendering onto a separate Bitmap.
            using (Image thumbnail = image.Clone(x => x.Resize(options)))
            {
                return Conversion.ConvertImageToByteArray(thumbnail, GetSourceFormat(image));
            }
        }

        /// <summary>
        /// Resolves the format the image was decoded from so that re-encoding preserves the
        /// source format. Encoding everything as JPEG would destroy PNG transparency and
        /// GIF animation.
        /// </summary>
        internal static IImageFormat GetSourceFormat(Image image)
        {
            if (image == null)
            {
                return JpegFormat.Instance;
            }

            return image.Metadata.DecodedImageFormat ?? JpegFormat.Instance;
        }

        private static float CalculateScalingRatio(Size originalSize, Size targetSize)
        {
            float originalAspectRatio = (float)originalSize.Width / originalSize.Height;
            float targetAspectRatio = (float)targetSize.Width / targetSize.Height;

            float scalingRatio;

            if (targetAspectRatio >= originalAspectRatio)
            {
                scalingRatio = (float)targetSize.Width / originalSize.Width;
            }
            else
            {
                scalingRatio = (float)targetSize.Height / originalSize.Height;
            }

            return scalingRatio;
        }
    }
}
