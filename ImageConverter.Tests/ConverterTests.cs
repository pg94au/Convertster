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
    /// <summary>
    /// Test for the Converter class, to ensure that it both converts image file formats as expected,
    /// and also that it emits the expected events.
    ///
    /// Note, because conversion is performed in parallel, any test scenario which involves the conversion of
    /// multiple files will be indeterministic in terms of the order in which the files are converted.  This
    /// manifests in the file conversion event ordering not being predictable unless the timing is explicitly
    /// controlled.
    /// </summary>
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

            var conversionResults = new List<ConversionResult>();

            var converter = new Converter();
            converter.OnFileConverted += (sender, conversionResult) => conversionResults.Add(conversionResult);

            await converter.ConvertAsync(targetFormat, new[] { testSourcePath }, CancellationToken.None);

            Assert.That(conversionResults.Count, Is.EqualTo(1));
            var conversionResult = conversionResults[0];
            Assert.That(conversionResult.Filename, Is.EqualTo(testSourcePath));
            Assert.That(conversionResult.Result, Is.EqualTo(FileResult.Succeeded));

            AssertThatFileContainsValidImage(expectedTargetPath);
        }

        [Test]
        public async Task Conversion_FailsIfSourceDoesNotExist()
        {
            var testSourcePath = Path.Combine(_testFilesDirectory, $"{Guid.NewGuid().ToString()}.bmp");
            var expectedTargetPath = Path.ChangeExtension(testSourcePath, ".jpg");

            var conversionResults = new List<ConversionResult>();

            var converter = new Converter();
            converter.OnFileConverted += (sender, conversionResult) => conversionResults.Add(conversionResult);

            await converter.ConvertAsync("JPG", new[] { testSourcePath }, CancellationToken.None);

            Assert.That(conversionResults.Count, Is.EqualTo(1));
            var conversionResult = conversionResults[0];
            Assert.That(conversionResult.Filename, Is.EqualTo(testSourcePath));
            Assert.That(conversionResult.Result, Is.EqualTo(FileResult.Failed));

            Assert.That(File.Exists(expectedTargetPath), Is.False);
        }

        [Test]
        public async Task Conversion_FailsIfSourceIsCorrupt()
        {
            var testSourcePath = Path.Combine(_testFilesDirectory, $"{Guid.NewGuid().ToString()}.bmp");
            File.WriteAllText(testSourcePath, "NO GOOD");

            var expectedTargetPath = Path.ChangeExtension(testSourcePath, ".jpg");
            File.Delete(expectedTargetPath);

            var conversionResults = new List<ConversionResult>();

            var converter = new Converter();
            converter.OnFileConverted += (sender, conversionResult) => conversionResults.Add(conversionResult);

            await converter.ConvertAsync("JPG", new[] { testSourcePath }, CancellationToken.None);

            Assert.That(conversionResults.Count, Is.EqualTo(1));
            var conversionResult = conversionResults[0];
            Assert.That(conversionResult.Filename, Is.EqualTo(testSourcePath));
            Assert.That(conversionResult.Result, Is.EqualTo(FileResult.Failed));

            Assert.That(File.Exists(expectedTargetPath), Is.False);
        }

        [Test]
        public async Task Conversion_HandlesMultipleFiles()
        {
            var testSourcePath1 = CreateBmpFile();
            var testSourcePath2 = CreateTiffFile();
            var expectedTargetPath1 = Path.ChangeExtension(testSourcePath1, ".jpg");
            var expectedTargetPath2 = Path.ChangeExtension(testSourcePath2, ".jpg");

            var conversionResults = new List<ConversionResult>();

            var converter = new Converter();
            converter.OnFileConverted += (sender, conversionResult) => conversionResults.Add(conversionResult);

            await converter.ConvertAsync("JPG", new[] { testSourcePath1, testSourcePath2 }, CancellationToken.None);

            Assert.That(conversionResults.Count, Is.EqualTo(2));
            Assert.That(
                conversionResults,
                Has.One.Matches<ConversionResult>(cr => cr.Filename == testSourcePath1 && cr.Result == FileResult.Succeeded)
            );
            Assert.That(
                conversionResults,
                Has.One.Matches<ConversionResult>(cr => cr.Filename == testSourcePath2 && cr.Result == FileResult.Succeeded)
            );

            AssertThatFileContainsValidImage(expectedTargetPath1);
            AssertThatFileContainsValidImage(expectedTargetPath2);
        }

        [Test]
        public async Task Conversion_HandlesPartialSuccess()
        {
            var testSourcePath1 = CreateBmpFile();
            var testSourcePath2 = Path.Combine(_testFilesDirectory, $"{Guid.NewGuid().ToString()}.bmp");
            File.WriteAllText(testSourcePath2, "NO GOOD");

            var expectedTargetPath1 = Path.ChangeExtension(testSourcePath1, ".jpg");
            var expectedTargetPath2 = Path.ChangeExtension(testSourcePath2, ".jpg");
            File.Delete(expectedTargetPath2);

            var conversionResults = new List<ConversionResult>();

            var converter = new Converter();
            converter.OnFileConverted += (sender, conversionResult) => conversionResults.Add(conversionResult);

            await converter.ConvertAsync("JPG", new[] { testSourcePath1, testSourcePath2 }, CancellationToken.None);

            Assert.That(conversionResults.Count, Is.EqualTo(2));
            Assert.That(
                conversionResults,
                Has.One.Matches<ConversionResult>(cr => cr.Filename == testSourcePath1 && cr.Result == FileResult.Succeeded)
                );
            Assert.That(
                conversionResults,
                Has.One.Matches<ConversionResult>(cr => cr.Filename == testSourcePath2 && cr.Result == FileResult.Failed)
            );

            AssertThatFileContainsValidImage(expectedTargetPath1);
            Assert.That(File.Exists(expectedTargetPath2), Is.False);
        }

        public void AssertThatFileContainsValidImage(string filename)
        {
            Assert.That(File.Exists(filename));
            using var targetImage1 = Image.FromFile(filename);
            Assert.That(targetImage1.Width, Is.EqualTo(100));
            Assert.That(targetImage1.Height, Is.EqualTo(100));
        }

        //TODO: Share these methods with other tests.
        private string CreateImageFile(ImageFormat imageFormat, bool deleteExistingTargetFiles = true)
        {
            var extension = imageFormat.Equals(ImageFormat.Bmp) ? "bmp" : "tiff";
            var testFilePath = Path.Combine(_testFilesDirectory, $"{Guid.NewGuid().ToString()}.{extension}");

            // Create a simple 100x100 red bitmap
            using var bitmap = new Bitmap(100, 100);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(System.Drawing.Color.Red);

            bitmap.Save(testFilePath, imageFormat);

            if (deleteExistingTargetFiles)
            {
                File.Delete(Path.ChangeExtension(testFilePath, "jpg"));
                File.Delete(Path.ChangeExtension(testFilePath, "png"));
            }

            return testFilePath;
        }

        private string CreateBmpFile(bool deleteExistingTargetFiles = true) => CreateImageFile(ImageFormat.Bmp, deleteExistingTargetFiles);
        private string CreateTiffFile(bool deleteExistingTargetFiles = true) => CreateImageFile(ImageFormat.Tiff, deleteExistingTargetFiles);
        private string CreateImageFile(string type, bool deleteExistingTargetFiles = true) =>
            type.ToUpper() == "BMP"
                ? CreateBmpFile(deleteExistingTargetFiles)
                : CreateTiffFile(deleteExistingTargetFiles);
    }
}