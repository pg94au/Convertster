using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace ImageConverter
{
    public enum ProgressStatus
    {
        Success,
        Warning,
        Failure
    }

    public partial class ProgressWindow : Window
    {
        private readonly string _targetType;
        private readonly string[] _filenames;
        private readonly CancellationTokenSource _cts = new();
        private volatile bool _finalized = false;

        public ProgressWindow(string targetType, string[] filenames)
        {
            InitializeComponent();

            _targetType = targetType ?? string.Empty;
            ConvertingFilesText.Text = string.Format(Properties.Resources.ConvertingFiles, _targetType);
            CancelButton.Content = Properties.Resources.Cancel;
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
            //TODO: Maybe this doesn't matter, if the value starts at zero (you can't see it)?
            ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Green);
            Trace.WriteLine("Initialized progress bar.");

            var successes = 0;
            var failures = 0;
            var progressBarOverallResult = ProgressStatus.Success;

            var converter = new Converter();
            converter.OnFileConverted += ConverterOnOnFileConverted;

            void ConverterOnOnFileConverted(object sender, ConversionResult conversionResult)
            {
                // Update the progress bar and text based on what just finished...
                Dispatcher.Invoke(() =>
                {
                    ConversionProgressBar.Value++;

                    CurrentFileNameText.Text = Path.GetFileName(conversionResult.Filename);

                    switch (conversionResult.Result)
                    {
                        case FileResult.Succeeded:
                            successes++;
                            break;
                        case FileResult.Failed:
                            failures++;
                            break;
                    }

                    if (conversionResult.Result == FileResult.Skipped &&
                        progressBarOverallResult == ProgressStatus.Success)
                    {
                        progressBarOverallResult = ProgressStatus.Warning;
                    }
                    else if (conversionResult.Result == FileResult.Failed)
                    {
                        progressBarOverallResult = ProgressStatus.Failure;
                    }

                    switch (progressBarOverallResult)
                    {
                        case ProgressStatus.Success:
                            ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Green);
                            break;
                        case ProgressStatus.Warning:
                            ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Gold);
                            break;
                        case ProgressStatus.Failure:
                            ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                            break;
                    }
                });
            }

            await converter.ConvertAsync(_targetType, _filenames, _cts.Token);

            Dispatcher.Invoke(() =>
            {
                Trace.WriteLine("Dispatcher lambda executing (async)...");
                if (_cts.Token.IsCancellationRequested)
                {
                    Trace.WriteLine("Setting text to show operation cancelled.");
                    CurrentFileNameText.Text = Properties.Resources.ConversionCancelled;
                    ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Gray);
                }
                else if (successes == 0 && failures > 0)
                {
                    Trace.WriteLine($"Setting text to show failure to convert all {failures} file(s).");
                    CurrentFileNameText.Text = string.Format(Properties.Resources.FailedToConvertAll, failures);
                    ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (failures > 0)
                {
                    Trace.WriteLine($"Setting text to show conversion of {successes} file(s) and failure to convert {failures} file(s).");
                    CurrentFileNameText.Text = string.Format(Properties.Resources.ConvertedWithFailures, successes, failures);
                    ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Gold);
                }
                else
                {
                    Trace.WriteLine($"Setting text to show successful conversion of all {_filenames.Length} file(s).");
                    CurrentFileNameText.Text = string.Format(Properties.Resources.SuccessfullyConvertedAll, _filenames.Length);
                    ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Green);
                }

                // Switch the Cancel button to a Close button after operations complete/cancel
                Trace.WriteLine("Switching Cancel button to Close.");
                CancelButton.Content = Properties.Resources.Close;
                CancelButton.IsEnabled = true;
            });
            Trace.WriteLine("Final UI update completed.");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // If button already says Close, close the window.
            if (string.Equals(CancelButton.Content as string, Properties.Resources.Close, StringComparison.OrdinalIgnoreCase))
            {
                Close();
                // Need to explicitly shut down the app because of how we configured it.
                Application.Current?.Shutdown();
                return;
            }

            // Immediate UI feedback so user sees cancellation right away
            CancelButton.IsEnabled = false;
            CurrentFileNameText.Text = string.Format(Properties.Resources.CancellationRequested, (int)ConversionProgressBar.Value);
            ConversionProgressBar.Foreground = new SolidColorBrush(Colors.Gray);

            // Request cancellation. Tasks will observe token and exit.
            _cts.Cancel();

            // Enable the button and change it to a Close button so the user can dismiss the dialog.
            CancelButton.Content = Properties.Resources.Close;
            CancelButton.IsEnabled = true;
        }
    }
}
