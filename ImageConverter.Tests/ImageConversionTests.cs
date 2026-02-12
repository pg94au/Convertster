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
        private static string ImageConverterExePath;
        private static string TestFilesDirectory;

        [SetUp]
        public static void SetUp()
        {
            // Find ImageConverter.exe
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string solutionDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\.."));
            ImageConverterExePath = Path.Combine(solutionDir, @"ImageConverter\bin\Release\ImageConverter.exe");

            if (!File.Exists(ImageConverterExePath))
            {
                ImageConverterExePath = Path.Combine(solutionDir, @"ImageConverter\bin\Debug\ImageConverter.exe");
            }

            Assert.That(File.Exists(ImageConverterExePath), Is.True,
                $"ImageConverter.exe not found. Expected at: {ImageConverterExePath}");

            // Create temp directory for test files
            TestFilesDirectory = Path.Combine(Path.GetTempPath(), "ImageConverterTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(TestFilesDirectory);
        }

        [TearDown]
        public static void TearDown()
        {
            if (Directory.Exists(TestFilesDirectory))
            {
                try
                {
                    Directory.Delete(TestFilesDirectory, true);
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
            string testBmpPath = Path.Combine(TestFilesDirectory, "test_image.bmp");
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
                // Act: Launch ImageConverter using FlaUI
                var psi = new ProcessStartInfo(ImageConverterExePath, $"JPG \"{testBmpPath}\"");
                app = Application.Launch(psi);
                automation = new UIA3Automation();
                Assert.That(app, Is.Not.Null, "Failed to launch ImageConverter");

                var process = Process.GetProcessById(app.ProcessId);
                
                var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
                Assert.That(mainWindow, Is.Not.Null, "Could not get main window");

                var progressBar = Retry.WhileNull(
                    () => mainWindow.FindFirstDescendant(
                        mainWindow.ConditionFactory.ByAutomationId("ConversionProgressBar")
                        ),
                    TimeSpan.FromSeconds(5)
                    ).Result?.AsProgressBar();

                //var progressBar = mainWindow.FindFirstDescendant(mainWindow.ConditionFactory.ByAutomationId("ConversionProgressBar"))?.AsProgressBar();

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

        //        [TestMethod]
        //        public void ConvertBmpToJpg_CreatesJpgFile()
        //        {
        //            // Arrange: Create a test BMP file
        //            string testBmpPath = Path.Combine(TestFilesDirectory, "test_image.bmp");
        //            string expectedJpgPath = Path.ChangeExtension(testBmpPath, ".jpg");

        //            // Create a simple 100x100 red bitmap
        //            using (var bitmap = new Bitmap(100, 100))
        //            {
        //                using (var graphics = Graphics.FromImage(bitmap))
        //                {
        //                    graphics.Clear(System.Drawing.Color.Red);
        //                }
        //                bitmap.Save(testBmpPath, ImageFormat.Bmp);
        //            }

        //            Assert.IsTrue(File.Exists(testBmpPath), "Test BMP file was not created");

        //            if (File.Exists(expectedJpgPath))
        //            {
        //                File.Delete(expectedJpgPath);
        //            }

        //            // Act: Launch ImageConverter using FlaUI
        //            var psi = new ProcessStartInfo(ImageConverterExePath, $"JPG \"{testBmpPath}\"");
        //            using (var app = FlaUI.Core.Application.Launch(psi))
        //            {
        //                using (var automation = new UIA3Automation())
        //                {
        //                    Assert.IsNotNull(app, "Failed to launch ImageConverter");

        //                    var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(5));
        //                    Assert.IsNotNull(mainWindow, "Could not get main window");

        //                    // Wait for Close button (conversion complete)
        //                    Button closeButton = null;
        //                    DateTime startTime = DateTime.Now;
        //                    while (DateTime.Now.Subtract(startTime).TotalSeconds < 30)
        //                    {
        //                        try
        //                        {
        //                            mainWindow.Focus();

        //                            // Find Close button by name (supports multiple languages)
        //                            var condition = mainWindow.ConditionFactory.ByControlType(ControlType.Button)
        //                                .And(mainWindow.ConditionFactory.ByName("Close")
        //                                    .Or(mainWindow.ConditionFactory.ByName("Fermer"))
        //                                    .Or(mainWindow.ConditionFactory.ByName("Cerrar"))
        //                                    .Or(mainWindow.ConditionFactory.ByName("Schließen"))
        //                                    .Or(mainWindow.ConditionFactory.ByName("Chiudi")));

        //                            closeButton = mainWindow.FindFirstDescendant(condition)?.AsButton();

        //                            if (closeButton != null) break;
        //                        }
        //                        catch
        //                        {
        //                        }

        //                        Thread.Sleep(500);
        //                    }

        //                    Assert.IsNotNull(closeButton, "Close button not found after 30 seconds");

        //                    // Click Close button
        //                    closeButton.Click();

        //                    // Wait for process to exit
        ////                    bool exited = app.WaitForExit(TimeSpan.FromSeconds(5));
        //                    Thread.Sleep(500);

        //                    // Assert: Verify results
        ////                    Assert.IsTrue(exited, "Application did not exit");
        //                    Assert.IsTrue(File.Exists(expectedJpgPath), $"JPG file not created: {expectedJpgPath}");

        //                    FileInfo jpgInfo = new FileInfo(expectedJpgPath);
        //                    Assert.IsTrue(jpgInfo.Length > 0, "JPG file is empty");

        //                    // Verify image dimensions
        //                    using (var jpgImage = Image.FromFile(expectedJpgPath))
        //                    {
        //                        Assert.AreEqual(100, jpgImage.Width);
        //                        Assert.AreEqual(100, jpgImage.Height);
        //                    }
        //                }
        //            }
        //        }
    }
}
