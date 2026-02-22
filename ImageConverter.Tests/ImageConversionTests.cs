using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ImageConverter.Tests
{
    /// <summary>
    /// Tests for the ImageConverter that automate the executable using FlaUI.
    /// </summary>
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

        [Test]
        public void CanCancelWhenPromptedToOverwriteTargetFile()
        {
            var testFilePath1 = _testSupport.CreateBmpFile();
            var expectedTargetPath1 = Path.ChangeExtension(testFilePath1, "jpg");
            File.WriteAllText(expectedTargetPath1, "SOME DATA");
            var testFilePath2 = _testSupport.CreateBmpFile();
            var expectedTargetPath2 = Path.ChangeExtension(testFilePath2, ".jpg");
            File.WriteAllText(expectedTargetPath2, "SOME DATA");

            Application app = null;
            UIA3Automation automation = null;
            try
            {
                var psi = new ProcessStartInfo(_imageConverterExePath, $"JPG \"{testFilePath1}\" \"{testFilePath2}\"");
                app = Application.Launch(psi);
                Assert.That(app, Is.Not.Null, "Failed to launch ImageConverter");
                automation = new UIA3Automation();

                var process = Process.GetProcessById(app.ProcessId);

                // Wait for prompt for first file to select Yes.
                var promptWindow1 = WaitForMainWindow(
                    app,
                    automation,
                    window => window.Title.StartsWith("Confirm"),
                    TimeSpan.FromSeconds(5)
                );
                Assert.That(promptWindow1, Is.Not.Null, "Could not get first prompt window");

                ClickButton(promptWindow1, "YesButton");

                // Wait for prompt for second file to select Cancel.
                var promptWindow2 = WaitForMainWindow(
                    app,
                    automation,
                    window => window.Title.StartsWith("Confirm"),
                    TimeSpan.FromSeconds(5)
                );
                Assert.That(promptWindow2, Is.Not.Null, "Could not get second prompt window");

                ClickButton(promptWindow2, "CancelButton");

                //TODO: Why are we not able to find this window by its AutomationId even when it is in the XAML?
                var mainWindow = WaitForMainWindow(
                    app,
                    automation,
                    window => window.Title.StartsWith("Convertster"),
                    TimeSpan.FromSeconds(5)
                );
                Assert.That(mainWindow, Is.Null, "There should be no conversion window");

                process.WaitForExit(1000);

                Assert.That(File.ReadAllText(expectedTargetPath1), Is.EqualTo("SOME DATA"));
                Assert.That(File.ReadAllText(expectedTargetPath2), Is.EqualTo("SOME DATA"));
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
        public void CanSkipOverwritingAllTargetFiles()
        {
            var testFilePath = _testSupport.CreateBmpFile();
            var expectedTargetPath = Path.ChangeExtension(testFilePath, ".jpg");
            File.WriteAllText(expectedTargetPath, "SOME DATA");

            Application app = null;
            UIA3Automation automation = null;
            try
            {
                var psi = new ProcessStartInfo(_imageConverterExePath, $"JPG \"{testFilePath}\"");
                app = Application.Launch(psi);
                Assert.That(app, Is.Not.Null, "Failed to launch ImageConverter");
                automation = new UIA3Automation();

                var process = Process.GetProcessById(app.ProcessId);

                var promptWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(5));
                Assert.That(promptWindow, Is.Not.Null, "Could not get prompt window");

                ClickButton(promptWindow, "NoButton");

                //TODO: Why are we not able to find this window by its AutomationId even when it is in the XAML?
                var mainWindow = WaitForMainWindow(
                    app,
                    automation,
                    window => window.Title.StartsWith("Convertster"),
                    TimeSpan.FromSeconds(5)
                );
                Assert.That(mainWindow, Is.Not.Null, "Could not get main window");

                WaitForProgressBarMaximum(mainWindow);

                ClickCloseButton(mainWindow);

                process.WaitForExit(1000);

                Assert.That(File.ReadAllText(expectedTargetPath), Is.EqualTo("SOME DATA"));
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
        public void CanChooseToOverwriteExistingTargetFile()
        {
            var testFilePath = _testSupport.CreateBmpFile();
            var expectedTargetPath = Path.ChangeExtension(testFilePath, ".jpg");
            File.WriteAllText(expectedTargetPath, "SOME DATA");

            Application app = null;
            UIA3Automation automation = null;
            try
            {
                var psi = new ProcessStartInfo(_imageConverterExePath, $"JPG \"{testFilePath}\"");
                app = Application.Launch(psi);
                Assert.That(app, Is.Not.Null, "Failed to launch ImageConverter");
                automation = new UIA3Automation();

                var process = Process.GetProcessById(app.ProcessId);

                var promptWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(5));
                Assert.That(promptWindow, Is.Not.Null, "Could not get prompt window");

                ClickButton(promptWindow, "YesButton");

                //TODO: Why are we not able to find this window by its AutomationId even when it is in the XAML?
                var mainWindow = WaitForMainWindow(
                    app,
                    automation,
                    window => window.Title.StartsWith("Convertster"),
                    TimeSpan.FromSeconds(5)
                );
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

        private void ClickButton(Window window, string buttonAutomationId, Action<Button> buttonAssertion = null)
        {
            var condition = window.ConditionFactory.ByAutomationId(buttonAutomationId);
            var button = window.FindFirstDescendant(condition)?.AsButton();

            Assert.That(button, Is.Not.Null, $"Could not locate button with AutomationId {buttonAutomationId}");

            buttonAssertion?.Invoke(button);

            button?.Click();
        }

        private void ClickCloseButton(Window window)
        {
            ClickButton(window, "CancelCloseButton", button =>
            {
                Assert.That(
                    button.Name,
                    Is.EqualTo("Close"),
                    "Button should say 'Close' after conversion completes"
                );
            });
        }

        private Window WaitForMainWindow(Application app, UIA3Automation automation, Func<Window, bool> filter, TimeSpan timeout)
        {
            return Retry.WhileNull(() =>
            {
                var windows = app.GetAllTopLevelWindows(automation);
                var matchingWindow = windows.FirstOrDefault(filter);
                return matchingWindow;
            }, timeout).Result;
        }
    }
}
