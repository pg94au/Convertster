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

namespace ImageConverter
{
    public partial class ProgressWindow : Window
    {
        private readonly string _targetType;
        private readonly string[] _filenames;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private volatile bool _finalized = false;

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
            ConversionProgressBar.Value = 0;
            ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Green);
            Trace.WriteLine("Initialized progress bar.");

            var progress = new Progress<(string file, int done, System.Windows.Media.Color foregroundColor)>(tuple =>
            {
                ConversionProgressBar.Value = tuple.done;

                // Ignore updating additional progress once we've finalized the UI state.
                if (_finalized)
                {
                    return;
                }

                ConversionProgressBar.Foreground = new SolidColorBrush(tuple.foregroundColor);
                CurrentFileNameText.Text = Path.GetFileName(tuple.file);
            });

            long successes = 0;
            long failures = 0;
            long skips = 0;

            using var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount - 1));
            var token = _cts.Token;

            var tasks = _filenames.Select(async filename =>
            {
                Trace.WriteLine($"Processing {filename}...");
                try
                {
                    await semaphore.WaitAsync(token).ConfigureAwait(false);
                    Trace.WriteLine($"Acquired semaphore for {filename}.");
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref skips);
                    Trace.WriteLine($"Cancelled while awaiting semaphore for {filename}.");
                    return;
                }

                try
                {
                    Trace.WriteLine($"Converting {filename} to {_targetType}...");
                    await ConvertImageType(_targetType, filename, token).ConfigureAwait(false);
                    Interlocked.Increment(ref successes);
                    Trace.WriteLine($"Successfully converted {filename}.");
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref skips);
                    Trace.WriteLine($"Conversion cancelled for {filename}");
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

                    if (!token.IsCancellationRequested)
                    {
                        Trace.WriteLine($"Updating progress to {filename}, {done} complete, color {currentColor}.");
                        ((IProgress<(string, int, System.Windows.Media.Color)>)progress).Report((filename, done, currentColor));
                    }

                    semaphore.Release();
                    Trace.WriteLine($"Released semaphore for {filename}.");
                }
            }).ToArray();

            try
            {
                Trace.WriteLine("Awaiting completion of all tasks.");
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // If any task threw on cancellation, swallow here - final UI update runs below.
                Trace.WriteLine("At least some tasks were cancelled.");
            }
            catch (Exception ex)
            {
                // Log unexpected exceptions; final UI update will reflect state.
                Trace.WriteLine($"Caught exception waiting for tasks: {ex.Message}");
            }
            _finalized = true;

            // Update final UI state on the dispatcher asynchronously. Mark the
            // UI as finalized before posting so any further progress reports are
            // ignored.
            Trace.WriteLine("Updating final UI state.");
            await Dispatcher.InvokeAsync(() =>
            {
                Trace.WriteLine("Dispatcher lambda executing (async)...");
                if (token.IsCancellationRequested)
                {
                    Trace.WriteLine("Setting text to show operation cancelled.");
                    CurrentFileNameText.Text = "Conversion cancelled.";
                    ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Gray);
                }
                else if (successes == 0 && failures > 0)
                {
                    Trace.WriteLine($"Setting text to show failure to convert all {failures} file(s).");
                    CurrentFileNameText.Text = $"Failed to convert {failures} file(s).";
                    ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (failures > 0)
                {
                    Trace.WriteLine($"Setting text to show conversion of {successes} file(s) and failure to convert {failures} file(s).");
                    CurrentFileNameText.Text = $"Converted {successes} file(s) ({failures} failed).";
                    ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Gold);
                }
                else
                {
                    Trace.WriteLine($"Setting text to show sucessful conversion of all {_filenames.Length} file(s).");
                    CurrentFileNameText.Text = $"Successfully converted {_filenames.Length} file(s).";
                    ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Green);
                }

                // Switch the Cancel button to a Close button after operations complete/cancel
                Trace.WriteLine("Switching Cancel button to Close.");
                CancelButton.Content = "Close";
                CancelButton.IsEnabled = true;
            }).Task.ConfigureAwait(false);
            Trace.WriteLine("Final UI update completed.");
        }

        private async Task ConvertImageType(string targetType, string filename, CancellationToken token)
        {
            var image = await Image.LoadAsync(filename, token).ConfigureAwait(false);

            switch (targetType)
            {
                case "JPG":
                    var jpgPath = Path.ChangeExtension(filename, ".jpg");
                    await image.SaveAsJpegAsync(
                        jpgPath,
                        new JpegEncoder { Quality = 75 },
                        token).ConfigureAwait(false);
                    break;
                case "PNG":
                    var pngPath = Path.ChangeExtension(filename, ".png");
                    await image.SaveAsPngAsync(
                        pngPath,
                        new PngEncoder { CompressionLevel = PngCompressionLevel.DefaultCompression },
                        token).ConfigureAwait(false);
                    break;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // If button already says Close, close the window.
            if (string.Equals(CancelButton.Content as string, "Close", StringComparison.OrdinalIgnoreCase))
            {
                Close();
                return;
            }

            // Immediate UI feedback so user sees cancellation right away
            CancelButton.IsEnabled = false;
            CurrentFileNameText.Text = $"Cancellation requested... ({(int)ConversionProgressBar.Value} processed)";
            ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Gray);

            // Request cancellation. Tasks will observe token and exit.
            _cts.Cancel();

            // Enable the button and change it to a Close button so the user can dismiss the dialog.
            CancelButton.Content = "Close";
            CancelButton.IsEnabled = true;
        }
    }
}
