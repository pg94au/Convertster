using System;
using System.Collections.Generic;
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
        private TestSupport _testSupport;

        [SetUp]
        public void SetUp()
        {
            _testSupport = new TestSupport();
        }

        [TearDown]
        public void TearDown()
        {
            _testSupport?.Dispose();
        }


        [Test]
        [TestCase("BMP", "JPG")]
        [TestCase("BMP", "PNG")]
        [TestCase("TIF", "JPG")]
        [TestCase("TIF", "PNG")]
        public async Task Conversion_CreatesTargetFilesAndGeneratesEvents(string sourceFormat, string targetFormat)
        {
            var testSourcePath = _testSupport.CreateImageFile(sourceFormat);
            var expectedTargetPath = Path.ChangeExtension(testSourcePath, "." + targetFormat.ToLower());

            var conversionResults = new List<ConversionResult>();

            var converter = new Converter();
            converter.OnFileConverted += (sender, conversionResult) => conversionResults.Add(conversionResult);

            await converter.ConvertAsync(targetFormat, new[] { testSourcePath }, CancellationToken.None);

            Assert.That(conversionResults.Count, Is.EqualTo(1));
            var conversionResult = conversionResults[0];
            Assert.That(conversionResult.Filename, Is.EqualTo(testSourcePath));
            Assert.That(conversionResult.Result, Is.EqualTo(FileResult.Succeeded));

            _testSupport.AssertThatFileContainsValidImage(expectedTargetPath);
        }

        [Test]
        public async Task Conversion_FailsIfSourceDoesNotExist()
        {
            var testSourcePath = Path.Combine(_testSupport.TestFilesDirectory, $"{Guid.NewGuid().ToString()}.bmp");
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
            var testSourcePath = _testSupport.CreateBadFile(ImageFormat.Bmp);

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
            var testSourcePath1 = _testSupport.CreateBmpFile();
            var testSourcePath2 = _testSupport.CreateTiffFile();
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

            _testSupport.AssertThatFileContainsValidImage(expectedTargetPath1);
            _testSupport.AssertThatFileContainsValidImage(expectedTargetPath2);
        }

        [Test]
        public async Task Conversion_HandlesPartialSuccess()
        {
            var testSourcePath1 = _testSupport.CreateBmpFile();
            var testSourcePath2 = _testSupport.CreateBadFile(ImageFormat.Bmp);

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

            _testSupport.AssertThatFileContainsValidImage(expectedTargetPath1);
            Assert.That(File.Exists(expectedTargetPath2), Is.False);
        }

        [Test]
        public async Task Conversion_SkipsWhenCancelled()
        {
            var testSourcePath = _testSupport.CreateBmpFile();
            var expectedTargetPath = Path.ChangeExtension(testSourcePath, ".jpg");

            var conversionResults = new List<ConversionResult>();

            var converter = new CancelableConverter();

            converter.OnFileConverted += (sender, conversionResult) => conversionResults.Add(conversionResult);

            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

            await converter.ConvertAsync("JPG", new[] { testSourcePath }, cts.Token);

            Assert.That(conversionResults.Count, Is.EqualTo(1));
            var conversionResult = conversionResults[0];
            Assert.That(conversionResult.Filename, Is.EqualTo(testSourcePath));
            Assert.That(conversionResult.Result, Is.EqualTo(FileResult.Skipped));

            Assert.That(File.Exists(expectedTargetPath), Is.False);
        }
    }

    /// <summary>
    /// A substitute for the Converter that waits long enough for the operation to be cancelled.
    /// (It doesn't do anything otherwise.)
    /// </summary>
    public class CancelableConverter : Converter
    {
        protected override async Task ConvertImageType(string targetType, string filename, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

            Assert.Fail("The conversion operation should have been canceled.");
        }
    }
}
