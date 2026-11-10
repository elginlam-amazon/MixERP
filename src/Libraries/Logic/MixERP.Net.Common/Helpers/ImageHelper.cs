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
using SkiaSharp;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    ///     Image helpers. Ported from System.Drawing (Windows-only on modern .NET) to SkiaSharp.
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

        /// <summary>
        ///     Scales and centre-crops the image to the requested size (0 keeps the original
        ///     dimension) and returns it JPEG-encoded, as the System.Drawing version did.
        /// </summary>
        public static byte[] GetResizedImage(byte[] imageData, int width, int height)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }

            using (SKBitmap image = SKBitmap.Decode(imageData))
            {
                if (image == null)
                {
                    return null;
                }

                int targetWidth = width.Equals(0) ? image.Width : width;
                int targetHeight = height.Equals(0) ? image.Height : height;

                float scalingRatio = CalculateScalingRatio(image.Width, image.Height, targetWidth, targetHeight);

                int scaledWidth = (int)Math.Round(image.Width * scalingRatio);
                int scaledHeight = (int)Math.Round(image.Height * scalingRatio);
                int scaledLeft = (targetWidth - scaledWidth) / 2;
                int scaledTop = (targetHeight - scaledHeight) / 2;

                // For portrait mode, adjust the vertical top of the crop area so that we get more of
                // the top area
                if (scaledWidth < scaledHeight && scaledHeight > targetHeight)
                {
                    scaledTop = (targetHeight - scaledHeight) / 4;
                }

                SKRect cropArea = SKRect.Create(scaledLeft, scaledTop, scaledWidth, scaledHeight);

                SKImageInfo info = new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);

                using (SKSurface surface = SKSurface.Create(info))
                {
                    SKCanvas canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);

                    using (SKImage source = SKImage.FromBitmap(image))
                    using (SKPaint paint = new SKPaint { IsAntialias = true })
                    {
                        canvas.DrawImage(source, cropArea, new SKSamplingOptions(SKCubicResampler.Mitchell), paint);
                    }

                    using (SKImage thumbnail = surface.Snapshot())
                    using (SKData data = thumbnail.Encode(SKEncodedImageFormat.Jpeg, 90))
                    {
                        return data.ToArray();
                    }
                }
            }
        }

        private static float CalculateScalingRatio(int originalWidth, int originalHeight, int targetWidth, int targetHeight)
        {
            float originalAspectRatio = (float)originalWidth / originalHeight;
            float targetAspectRatio = (float)targetWidth / targetHeight;

            if (targetAspectRatio >= originalAspectRatio)
            {
                return (float)targetWidth / originalWidth;
            }

            return (float)targetHeight / originalHeight;
        }
    }
}
