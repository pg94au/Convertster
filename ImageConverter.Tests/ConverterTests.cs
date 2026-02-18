using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ImageConverter.Tests
{
    [TestFixture]
    public class ConverterTests
    {
        private static string _testFilesDirectory;

        [SetUp]
        public static void SetUp()
        {
            // Create temp directory for test files
            _testFilesDirectory = Path.Combine(Path.GetTempPath(), "ImageConverterTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testFilesDirectory);
        }

        [TearDown]
        public static void TearDown()
        {
            if (Directory.Exists(_testFilesDirectory))
            {
                try
                {
                    Directory.Delete(_testFilesDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }


        [Test]
        [TestCase("BMP", "JPG")]
        [TestCase("BMP", "PNG")]
        [TestCase("TIF", "JPG")]
        [TestCase("TIF", "PNG")]
        public async Task Conversion_CreatesTargetFilesAndGeneratesEvents(string sourceFormat, string targetFormat)
        {
            var testSourcePath = CreateImageFile(sourceFormat);

            var expectedTargetPath = Path.ChangeExtension(testSourcePath, "." + targetFormat.ToLower());
            File.Delete(expectedTargetPath);

            var conversionResults = new List<ConversionResult>();

            var converter = new Converter();
            converter.OnFileConverted += (sender, conversionResult) => conversionResults.Add(conversionResult);

            await converter.ConvertAsync(targetFormat, new[] { testSourcePath }, CancellationToken.None);

            Assert.That(conversionResults.Count, Is.EqualTo(1));
            var conversionResult = conversionResults[0];
            Assert.That(conversionResult.Filename, Is.EqualTo(testSourcePath));
            Assert.That(conversionResult.Result, Is.EqualTo(FileResult.Succeeded));

            using var targetImage = Image.FromFile(expectedTargetPath);
            Assert.That(targetImage.Width, Is.EqualTo(100));
            Assert.That(targetImage.Height, Is.EqualTo(100));
        }

        //TODO: Share these methods with other tests.
        private string CreateImageFile(ImageFormat imageFormat)
        {
            var extension = imageFormat.Equals(ImageFormat.Bmp) ? "bmp" : "tiff";
            var testBmpPath = Path.Combine(_testFilesDirectory, $"{Guid.NewGuid().ToString()}.{extension}");

            // Create a simple 100x100 red bitmap
            using var bitmap = new Bitmap(100, 100);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(System.Drawing.Color.Red);

            bitmap.Save(testBmpPath, imageFormat);

            return testBmpPath;
        }

        private string CreateBmpFile() => CreateImageFile(ImageFormat.Bmp);
        private string CreateTiffFile() => CreateImageFile(ImageFormat.Tiff);
        private string CreateImageFile(string type) => type.ToUpper() == "BMP" ? CreateBmpFile() : CreateTiffFile();
    }
}