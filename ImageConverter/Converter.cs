using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace ImageConverter;

public enum FileResult
{
    Succeeded,
    Failed,
    Skipped
}

public class ConversionResult
{
    public string Filename { get; set; }
    public FileResult Result { get; set; }
}

public class ConversionComplete
{
}

public class Converter
{
    public event EventHandler<ConversionResult> OnFileConverted;

    public async Task ConvertAsync(
        string targetType,
        string[] inputFiles,
        CancellationToken cancellationToken
        )
    {
        long successes = 0;
        long failures = 0;
        long skips = 0;

        using var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount - 1));

        var tasks = inputFiles.Select(async filename =>
        {
            Trace.WriteLine($"Processing {filename}...");
            try
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                Trace.WriteLine($"Acquired semaphore for {filename}.");
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref skips);
                Trace.WriteLine($"Cancelled while awaiting semaphore for {filename}.");
                return new ConversionResult { Filename = filename, Result = FileResult.Skipped };
            }

            try
            {
                Trace.WriteLine($"Converting {filename} to {targetType}...");
                await ConvertImageType(targetType, filename, cancellationToken).ConfigureAwait(false);
                Interlocked.Increment(ref successes);
                Trace.WriteLine($"Successfully converted {filename}.");
                return new ConversionResult { Filename = filename, Result = FileResult.Succeeded };
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref skips);
                Trace.WriteLine($"Conversion cancelled for {filename}");
                return new ConversionResult { Filename = filename, Result = FileResult.Skipped };
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failures);
                Trace.WriteLine($"Error converting {filename}: {ex.Message}");
                return new ConversionResult { Filename = filename, Result = FileResult.Failed };
            }
            finally
            {
                semaphore.Release();
                Trace.WriteLine($"Released semaphore for {filename}.");
            }
        }).ToArray();

        var remainingTasks = new List<Task<ConversionResult>>(tasks);
        while (remainingTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(remainingTasks).ConfigureAwait(false);
            var conversionResult = await completedTask;
            OnFileConverted?.Invoke(this, conversionResult);
            remainingTasks.Remove(completedTask);
        }
    }

    private async Task ConvertImageType(string targetType, string filename, CancellationToken token)
    {
        var image = await Image.LoadAsync(filename, token).ConfigureAwait(false);

        switch (targetType.ToLower())
        {
            case "jpg":
                var jpgPath = Path.ChangeExtension(filename, ".jpg");
                Trace.WriteLine($"Saving {jpgPath}");
                await image.SaveAsJpegAsync(
                    jpgPath,
                    new JpegEncoder { Quality = 75 },
                    token).ConfigureAwait(false);
                break;
            case "png":
                var pngPath = Path.ChangeExtension(filename, ".png");
                Trace.WriteLine($"Saving {pngPath}");
                await image.SaveAsPngAsync(
                    pngPath,
                    new PngEncoder { CompressionLevel = PngCompressionLevel.DefaultCompression },
                    token).ConfigureAwait(false);
                break;
            default:
                Trace.WriteLine($"Request to convert to unsupported target format {targetType}.");
                throw new NotImplementedException($"Target file type {targetType} not supported.");
        }
    }
}
