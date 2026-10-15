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
using SkiaSharp;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    ///     Image helpers. System.Drawing (GDI+) is Windows-only on .NET, so images are processed with SkiaSharp.
    /// </summary>
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

        public static string GetFileExtension(SKEncodedImageFormat format)
        {
            switch (format)
            {
                case SKEncodedImageFormat.Bmp: return ".BMP";
                case SKEncodedImageFormat.Gif: return ".GIF";
                case SKEncodedImageFormat.Png: return ".PNG";
                default: return ".JPG";
            }
        }

        public static SKEncodedImageFormat GetImageFormat(String path)
        {
            switch (Path.GetExtension(path))
            {
                case ".bmp": return SKEncodedImageFormat.Bmp;
                case ".gif": return SKEncodedImageFormat.Gif;
                case ".jpg": return SKEncodedImageFormat.Jpeg;
                case ".png": return SKEncodedImageFormat.Png;
            }
            return SKEncodedImageFormat.Jpeg;
        }

        public static byte[] GetResizedImage(SKBitmap image, int width, int height)
        {
            SKSizeI thumbnailSize = new SKSizeI(width, height);

            if (image == null)
            {
                return null;
            }

            if (thumbnailSize.Width.Equals(0))
            {
                thumbnailSize.Width = image.Width;
            }

            if (thumbnailSize.Height.Equals(0))
            {
                thumbnailSize.Height = image.Height;
            }

            SKSizeI originalSize = new SKSizeI(image.Width, image.Height);
            float scalingRatio = CalculateScalingRatio(originalSize, thumbnailSize);

            int scaledWidth = (int)Math.Round(image.Width * scalingRatio);
            int scaledHeight = (int)Math.Round(image.Height * scalingRatio);
            int scaledLeft = (thumbnailSize.Width - scaledWidth) / 2;
            int scaledTop = (thumbnailSize.Height - scaledHeight) / 2;

            // For portrait mode, adjust the vertical top of the crop area so that we get more of
            // the top area
            if (scaledWidth < scaledHeight && scaledHeight > thumbnailSize.Height)
            {
                scaledTop = (thumbnailSize.Height - scaledHeight) / 4;
            }

            SKRect cropArea = SKRect.Create(scaledLeft, scaledTop, scaledWidth, scaledHeight);

            using (SKBitmap thumbnail = new SKBitmap(thumbnailSize.Width, thumbnailSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul))
            {
                using (SKCanvas thumbnailCanvas = new SKCanvas(thumbnail))
                using (SKImage source = SKImage.FromBitmap(image))
                using (SKPaint paint = new SKPaint { IsAntialias = true })
                {
                    thumbnailCanvas.Clear(SKColors.Transparent);
                    // High quality bicubic resampling (was InterpolationMode.HighQualityBicubic).
                    thumbnailCanvas.DrawImage(source, cropArea, new SKSamplingOptions(SKCubicResampler.Mitchell), paint);
                }

                return Conversion.TryCastByteArray(thumbnail);
            }
        }

        private static float CalculateScalingRatio(SKSizeI originalSize, SKSizeI targetSize)
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