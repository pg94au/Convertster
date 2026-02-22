using NUnit.Framework;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageConverter.Tests;

/// <summary>
/// Some methods to support the tests, such as creating test image files and asserting that files contain valid images.
/// </summary>
public class TestSupport : IDisposable
{
    public string TestFilesDirectory { get; private set; }

    public TestSupport()
    {
        TestFilesDirectory = Path.Combine(Path.GetTempPath(), "ImageConverterTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TestFilesDirectory);
    }

    public void Dispose()
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

    public void AssertThatFileContainsValidImage(string filename)
    {
        Assert.That(File.Exists(filename));
        using var targetImage1 = Image.FromFile(filename);
        Assert.That(targetImage1.Width, Is.EqualTo(100));
        Assert.That(targetImage1.Height, Is.EqualTo(100));
    }

    public string CreateImageFile(ImageFormat imageFormat, bool deleteExistingTargetFiles = true)
    {
        var extension = imageFormat.Equals(ImageFormat.Bmp) ? "bmp" : "tiff";
        var testFilePath = Path.Combine(TestFilesDirectory, $"{Guid.NewGuid().ToString()}.{extension}");

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

    public string CreateBmpFile(bool deleteExistingTargetFiles = true) => CreateImageFile(ImageFormat.Bmp, deleteExistingTargetFiles);
    public string CreateTiffFile(bool deleteExistingTargetFiles = true) => CreateImageFile(ImageFormat.Tiff, deleteExistingTargetFiles);
    public string CreateImageFile(string type, bool deleteExistingTargetFiles = true) =>
        type.ToUpper() == "BMP"
            ? CreateBmpFile(deleteExistingTargetFiles)
            : CreateTiffFile(deleteExistingTargetFiles);

    public string CreateBadFile(ImageFormat imageFormat, bool deleteExistingTargetFiles = true)
    {
        var extension = imageFormat.Equals(ImageFormat.Bmp) ? "bmp" : "tiff";
        var testFilePath = Path.Combine(TestFilesDirectory, $"{Guid.NewGuid().ToString()}.{extension}");

        File.WriteAllText(testFilePath, "NO GOOD");

        if (deleteExistingTargetFiles)
        {
            File.Delete(Path.ChangeExtension(testFilePath, "jpg"));
            File.Delete(Path.ChangeExtension(testFilePath, "png"));
        }

        return testFilePath;
    }
}