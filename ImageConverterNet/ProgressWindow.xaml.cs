using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ImageConverterNet
{
    public partial class ProgressWindow : Window
    {
        private readonly string _targetType;
        private readonly string[] _filenames;

        public ProgressWindow(string targetType, string[] filenames)
        {
            InitializeComponent();

            _targetType = targetType ?? string.Empty;
            ConvertingFilesText.Text = $"Converting files to {_targetType}...";
            Trace.WriteLine($"Target type is {_targetType}");

            _filenames = filenames ?? Array.Empty<string>();
            Trace.WriteLine($"Filenames to convert: {string.Join(", ", _filenames)}");
        }

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            ConversionProgressBar.Minimum = 0;
            ConversionProgressBar.Maximum = Math.Max(0, _filenames.Length);
            ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Green);

            var progress = new Progress<(string file, int done, System.Windows.Media.Color foregroundColor)>(tuple =>
            {
                ConversionProgressBar.Value = tuple.done;
                ConversionProgressBar.Foreground = new SolidColorBrush(tuple.foregroundColor);
                CurrentFileNameText.Text = Path.GetFileName(tuple.file);
            });

            long successes = 0;
            long failures = 0;

            using var semaphore = new SemaphoreSlim(Environment.ProcessorCount - 1);
            var tasks = _filenames.Select(async filename =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await ConvertImageType(_targetType, filename);
                    Interlocked.Increment(ref successes);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failures);
                    Trace.WriteLine($"Error converting {filename}: {ex.Message}");
                }
                finally
                {
                    long successesSoFar = Interlocked.Read(ref successes);
                    long failuresSoFar = Interlocked.Read(ref failures);
                    var done = (int)(successesSoFar + failuresSoFar);
                    System.Windows.Media.Color currentColor = Colors.Green;
                    if (failuresSoFar == 0)
                    {
                        currentColor = Colors.Green;
                    }
                    else if (successesSoFar + failuresSoFar < _filenames.Length)
                    {
                        currentColor = Colors.Gold;
                    }
                    else
                    {
                        currentColor = successesSoFar > 0 ? Colors.Gold : Colors.Red;
                    }
                    ((IProgress<(string, int, System.Windows.Media.Color)>)progress).Report((filename, done, currentColor));
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);


            //foreach (var filename in _filenames)
            //{
            //    CurrentFileNameText.Text = filename;
            //    await Task.Yield();

            //    try
            //    {
            //        await ConvertImageType(_targetType, filename);
            //        Trace.WriteLine($"Successfully converted {filename}");
            //        successes++;
            //    }
            //    catch (Exception ex)
            //    {
            //        ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Gold);
            //        Trace.WriteLine($"Error converting {filename}: {ex.Message}");
            //        failures++;
            //    }

            //    ConversionProgressBar.Value = Math.Min(ConversionProgressBar.Maximum, ConversionProgressBar.Value + 1);
            //    await Task.Delay(1);
            //    await Task.Yield();
            //}

            if (successes == 0)
            {
                CurrentFileNameText.Text = $"Failed to convert {(_filenames.Any() ? "file" : $"{failures} files")}.";
                //ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Red);
            }
            else if (failures > 0)
            {
                CurrentFileNameText.Text = $"Converted {successes} file{(_filenames.Any() ? "s" : "")} ({failures} failed).";
            }
            else
            {
                CurrentFileNameText.Text = $"Successfully converted {_filenames.Length} file{(_filenames.Any() ? "s" : "")}.";
            }
        }

        private async Task ConvertImageType(string targetType, string filename)
        {
            var image = await Image.LoadAsync(filename).ConfigureAwait(false);

            switch (targetType)
            {
                case "JPG":
                    var jpgPath = Path.ChangeExtension(filename, ".jpg");
                    await image.SaveAsJpegAsync(jpgPath, new JpegEncoder { Quality = 75 }).ConfigureAwait(false);
                    break;
                case "PNG":
                    var pngPath = Path.ChangeExtension(filename, ".png");
                    await image.SaveAsPngAsync(pngPath,
                        new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression }).ConfigureAwait(false);
                    break;
            }
        }
    }
}
