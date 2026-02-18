using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageConverter.Tests
{
    [TestFixture]
    [Category("UI")]
    public class ImageConversionTests
    {
        private static string _imageConverterExePath;
        private static string _testFilesDirectory;

        [SetUp]
        public static void SetUp()
        {
            // Find ImageConverter.exe
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string solutionDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\.."));
            _imageConverterExePath = Path.Combine(solutionDir, @"ImageConverter\bin\Release\ImageConverter.exe");

            if (!File.Exists(_imageConverterExePath))
            {
                _imageConverterExePath = Path.Combine(solutionDir, @"ImageConverter\bin\Debug\ImageConverter.exe");
            }

            Assert.That(File.Exists(_imageConverterExePath), Is.True,
                $"ImageConverter.exe not found. Expected at: {_imageConverterExePath}");

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
        public void CanConvertSingleImage(string sourceFormat, string targetFormat)
        {
            var testSourcePath = CreateImageFile(sourceFormat);

            var expectedTargetPath = Path.ChangeExtension(testSourcePath, "." + targetFormat.ToLower());
            File.Delete(expectedTargetPath);

            Application app = null;
            UIA3Automation automation = null;
            try
            {
                var psi = new ProcessStartInfo(_imageConverterExePath, $"{targetFormat} \"{testSourcePath}\"");
                app = Application.Launch(psi);
                Assert.That(app, Is.Not.Null, "Failed to launch ImageConverter");
                automation = new UIA3Automation();

                var process = Process.GetProcessById(app.ProcessId);
                
                var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(5));
                Assert.That(mainWindow, Is.Not.Null, "Could not get main window");

                WaitForProgressBarMaximum(mainWindow);

                ClickCloseButton(mainWindow);

                process.WaitForExit(1000);

                using var targetImage = Image.FromFile(expectedTargetPath);
                Assert.That(targetImage.Width, Is.EqualTo(100));
                Assert.That(targetImage.Height, Is.EqualTo(100));
            }
            finally
            {
                if (app is { HasExited: false })
                {
                    app.Close();
                }
                app?.Dispose();
                automation?.Dispose();
            }
        }

        [Test]
        public void CanConvertMultipleImages()
        {
            var testFilePath1 = CreateBmpFile();
            var expectedTargetPath1 = Path.ChangeExtension(testFilePath1, ".jpg");
            File.Delete(expectedTargetPath1);

            var testFilePath2 = CreateTiffFile();
            var expectedTargetPath2 = Path.ChangeExtension(testFilePath2, ".jpg");
            File.Delete(expectedTargetPath2);

            Application app = null;
            UIA3Automation automation = null;
            try
            {
                var psi = new ProcessStartInfo(_imageConverterExePath, $"JPG \"{testFilePath1}\" \"{testFilePath2}\"");
                app = Application.Launch(psi);
                Assert.That(app, Is.Not.Null, "Failed to launch ImageConverter");
                automation = new UIA3Automation();

                var process = Process.GetProcessById(app.ProcessId);

                var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(5));
                Assert.That(mainWindow, Is.Not.Null, "Could not get main window");

                WaitForProgressBarMaximum(mainWindow);

                ClickCloseButton(mainWindow);

                process.WaitForExit(1000);

                using var targetImage1 = Image.FromFile(expectedTargetPath1);
                Assert.That(targetImage1.Width, Is.EqualTo(100));
                Assert.That(targetImage1.Height, Is.EqualTo(100));

                using var targetImage2 = Image.FromFile(expectedTargetPath2);
                Assert.That(targetImage2.Width, Is.EqualTo(100));
                Assert.That(targetImage2.Height, Is.EqualTo(100));
            }
            finally
            {
                if (app is { HasExited: false })
                {
                    app.Close();
                }
                app?.Dispose();
                automation?.Dispose();
            }
        }

        private void WaitForProgressBarMaximum(Window mainWindow)
        {
            var progressBar = Retry.WhileNull(
                () => mainWindow.FindFirstDescendant(
                    mainWindow.ConditionFactory.ByAutomationId("ConversionProgressBar")
                ),
                TimeSpan.FromSeconds(5)
            ).Result?.AsProgressBar();
            Assert.That(progressBar, Is.Not.Null, "Progress bar not found");

            Retry.WhileTrue(() => progressBar.Value < progressBar.Maximum, TimeSpan.FromSeconds(5));
            Assert.That(
                progressBar.Value,
                Is.EqualTo(progressBar.Maximum),
                "Progress bar did not complete"
            );
        }

        private void ClickCloseButton(Window mainWindow)
        {
            var condition = mainWindow.ConditionFactory.ByAutomationId("CancelCloseButton");
            var cancelCloseButton = mainWindow.FindFirstDescendant(condition)?.AsButton();

            Assert.That(cancelCloseButton, Is.Not.Null, "Could not locate Cancel/Close button");

            Assert.That(
                cancelCloseButton.Name,
                Is.EqualTo("Close"),
                "Button should say 'Close' after conversion completes"
            );

            cancelCloseButton?.Click();
        }

        private string CreateImageFile(ImageFormat imageFormat)
        {
            var extension = imageFormat.Equals(ImageFormat.Bmp) ? "bmp" : "tiff";
            var testFilePath = Path.Combine(_testFilesDirectory, $"{Guid.NewGuid().ToString()}.{extension}");

            // Create a simple 100x100 red bitmap
            using var bitmap = new Bitmap(100, 100);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(System.Drawing.Color.Red);

            bitmap.Save(testFilePath, imageFormat);

            return testFilePath;
        }

        private string CreateBmpFile() => CreateImageFile(ImageFormat.Bmp);
        private string CreateTiffFile() => CreateImageFile(ImageFormat.Tiff);
        private string CreateImageFile(string type) => type.ToUpper() == "BMP" ? CreateBmpFile() : CreateTiffFile();
    }
}
