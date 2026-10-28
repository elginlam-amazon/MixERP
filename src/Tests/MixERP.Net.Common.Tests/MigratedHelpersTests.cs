using System;
using System.Collections.Generic;
using System.IO;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;
using SkiaSharp;
using Xunit;

namespace MixERP.Net.Common.Tests
{
    /// <summary>
    /// Covers the helpers whose implementation changed during the .NET 10 migration.
    /// </summary>
    public class MigratedHelpersTests
    {
        [Fact]
        public void DeserializeDynamic_ArrayOfArrays_MatchesJavaScriptSerializerShape()
        {
            dynamic result = JSONHelper.DeserializeDynamic("[[\"ITEM-1\", \"Item\", 2, \"Piece\", 10.5, null]]");

            object[] rows = Assert.IsType<object[]>(result);
            object[] row = Assert.IsType<object[]>(rows[0]);

            Assert.Equal("ITEM-1", row[0]);
            Assert.Equal(2, row[2]);
            Assert.Equal(10.5m, row[4]);
            Assert.Null(row[5]);
            Assert.Equal(2, Conversion.TryCastInteger(row[2]));
            Assert.Equal(10.5m, Conversion.TryCastDecimal(row[4]));
        }

        [Fact]
        public void DeserializeDynamic_ArrayOfObjects_ReturnsDictionaries()
        {
            dynamic result = JSONHelper.DeserializeDynamic("[{\"Comment\":\"c\",\"FilePath\":\"a.png\",\"OriginalFileName\":\"b.png\"}]");

            foreach (dynamic item in result)
            {
                Dictionary<string, object> attachment = Assert.IsType<Dictionary<string, object>>(item);
                string filePath = item["FilePath"];

                Assert.Equal("c", attachment["Comment"]);
                Assert.Equal("a.png", filePath);
            }
        }

        [Fact]
        public void DeserializeDynamic_Empty_ReturnsNull()
        {
            Assert.Null(JSONHelper.DeserializeDynamic(string.Empty));
        }

        [Fact]
        public void HashSha512Hex_IsUnchanged()
        {
            Assert.Equal(
                "138f75cab47d01d0a813badb38f612dc46604ae669544eb9003f71928d3cb3a7e5a0d7ac63f6697bdccc56f2e2c784b616241227df5122436f847a2634ef6e2f",
                Conversion.HashSha512Hex("secret", "salt"));
        }

        [Fact]
        public void CopyDirectory_CopiesRecursivelyAndOverwrites()
        {
            string root = Path.Combine(Path.GetTempPath(), "mixerp-tests-" + Guid.NewGuid().ToString("N"));
            string source = Path.Combine(root, "source");
            string destination = Path.Combine(root, "destination");

            try
            {
                Directory.CreateDirectory(Path.Combine(source, "child"));
                File.WriteAllText(Path.Combine(source, "a.txt"), "new");
                File.WriteAllText(Path.Combine(source, "child", "b.txt"), "b");
                Directory.CreateDirectory(destination);
                File.WriteAllText(Path.Combine(destination, "a.txt"), "old");

                DirectoryHelper.CopyDirectory(source, destination);

                Assert.Equal("new", File.ReadAllText(Path.Combine(destination, "a.txt")));
                Assert.Equal("b", File.ReadAllText(Path.Combine(destination, "child", "b.txt")));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Fact]
        public void GetResizedImage_ReturnsJpegWithRequestedSize()
        {
            byte[] png = CreatePng(200, 100);

            Assert.True(ImageHelper.IsValidImage(png));

            byte[] resized = ImageHelper.GetResizedImage(png, 50, 50);

            using (SKCodec codec = SKCodec.Create(new SKMemoryStream(resized)))
            {
                Assert.Equal(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
                Assert.Equal(50, codec.Info.Width);
                Assert.Equal(50, codec.Info.Height);
            }
        }

        [Fact]
        public void GetResizedImage_ZeroSize_KeepsOriginalSize()
        {
            byte[] resized = ImageHelper.GetResizedImage(CreatePng(64, 32), 0, 0);

            using (SKBitmap bitmap = SKBitmap.Decode(resized))
            {
                Assert.Equal(64, bitmap.Width);
                Assert.Equal(32, bitmap.Height);
            }
        }

        [Fact]
        public void IsValidImage_RejectsNonImageData()
        {
            Assert.False(ImageHelper.IsValidImage(new byte[] { 1, 2, 3, 4 }));
            Assert.False(ImageHelper.IsValidImage(null));
        }

        private static byte[] CreatePng(int width, int height)
        {
            using (SKBitmap bitmap = new SKBitmap(width, height))
            {
                bitmap.Erase(SKColors.CornflowerBlue);

                using (SKImage image = SKImage.FromBitmap(bitmap))
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return data.ToArray();
                }
            }
        }
    }
}
