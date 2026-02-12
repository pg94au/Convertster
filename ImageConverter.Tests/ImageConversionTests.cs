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
using System.Threading;

namespace ImageConverter.Tests
{
    [TestFixture]
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
        public void CanConvertSingleBmpToJpg()
        {
            // Arrange: Create a test BMP file
            string testBmpPath = Path.Combine(_testFilesDirectory, "test_image.bmp");
            string expectedJpgPath = Path.ChangeExtension(testBmpPath, ".jpg");

            // Create a simple 100x100 red bitmap
            using (var bitmap = new Bitmap(100, 100))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.Red);
                }

                bitmap.Save(testBmpPath, ImageFormat.Bmp);
            }

            Assert.That(File.Exists(testBmpPath), Is.True, "Test BMP file was not created");

            if (File.Exists(expectedJpgPath))
            {
                File.Delete(expectedJpgPath);
            }

            Application app = null;
            UIA3Automation automation = null;
            try
            {
                var psi = new ProcessStartInfo(_imageConverterExePath, $"JPG \"{testBmpPath}\"");
                app = Application.Launch(psi);
                automation = new UIA3Automation();
                Assert.That(app, Is.Not.Null, "Failed to launch ImageConverter");

                var process = Process.GetProcessById(app.ProcessId);
                
                var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(5));
                Assert.That(mainWindow, Is.Not.Null, "Could not get main window");

                var progressBar = Retry.WhileNull(
                    () => mainWindow.FindFirstDescendant(
                        mainWindow.ConditionFactory.ByAutomationId("ConversionProgressBar")
                        ),
                    TimeSpan.FromSeconds(5)
                    ).Result?.AsProgressBar();

                Assert.That(progressBar, Is.Not.Null, "Progress bar not found");

                var startTime = DateTime.Now;
                while (progressBar.Value < progressBar.Maximum && DateTime.Now.Subtract(startTime).TotalSeconds < 3)
                {
                    Thread.Sleep(100);
                }

                Assert.That(progressBar.Value, Is.EqualTo(progressBar.Maximum), "Progress bar did not complete");

                var condition = mainWindow.ConditionFactory.ByAutomationId("CancelCloseButton");
                var cancelCloseButton = mainWindow.FindFirstDescendant(condition)?.AsButton();

                Assert.That(cancelCloseButton, Is.Not.Null, "Could not locate Cancel/Close button");

                var buttonText = cancelCloseButton.Name;

                Assert.That(buttonText, Is.EqualTo("Close"), "Button should say 'Close' after conversion completes");

                cancelCloseButton?.Click();

                process.WaitForExit(1000);

                using var jpgImage = Image.FromFile(expectedJpgPath);
                Assert.That(jpgImage.Width, Is.EqualTo(100));
                Assert.That(jpgImage.Height, Is.EqualTo(100));
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
    }
}
