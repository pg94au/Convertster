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
        private TestSupport _testSupport;
        private static string _imageConverterExePath;

        [SetUp]
        public void SetUp()
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
        public void CanConvertSingleImage(string sourceFormat, string targetFormat)
        {
            var testSourcePath = _testSupport.CreateImageFile(sourceFormat);
            var expectedTargetPath = Path.ChangeExtension(testSourcePath, "." + targetFormat.ToLower());

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

                _testSupport.AssertThatFileContainsValidImage(expectedTargetPath);
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
            var testFilePath1 = _testSupport.CreateBmpFile();
            var expectedTargetPath1 = Path.ChangeExtension(testFilePath1, ".jpg");

            var testFilePath2 = _testSupport.CreateTiffFile();
            var expectedTargetPath2 = Path.ChangeExtension(testFilePath2, ".jpg");

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

                _testSupport.AssertThatFileContainsValidImage(expectedTargetPath1);
                _testSupport.AssertThatFileContainsValidImage(expectedTargetPath2);
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
    }
}
