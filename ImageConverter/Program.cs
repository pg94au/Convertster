using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ImageConverter
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var traceFilename = Environment.GetEnvironmentVariable("CONVERTSTER_DEBUG_OUTPUT");
            if (!string.IsNullOrWhiteSpace(traceFilename))
            {
                var fileListener = new TextWriterTraceListener(traceFilename, "FileListener");

                Trace.Listeners.Add(fileListener);
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // Create and run the WPF application with a ProgressWindow.
            // We avoid relying on generated App.xaml entry point by providing our own Main.
            var targetType = args[0].ToLower();
            var filenames = args.Skip(1).ToArray();

            filenames = FilterAnySkippedFiles(targetType, filenames);
            Trace.WriteLine($"Converting {string.Join(", ", filenames)}");

            var window = new ProgressWindow(targetType, filenames);
            var app = new Application();
            app.DispatcherUnhandledException += AppOnDispatcherUnhandledException;
            app.Run(window);

            Trace.Flush();
        }

        private static string[] FilterAnySkippedFiles(string targetType, string[] filenames)
        {
            var includedFiles = new List<string>();
            foreach (var filename in filenames)
            {
                var targetFilename = Path.ChangeExtension(filename, targetType.ToLower());
                // Prompt the user about existing target files.
                if (File.Exists(targetFilename))
                {
                    var message = $"Overwrite existing file {targetFilename}?";
                    var result = MessageBox.Show(message, "Confirm overwrite", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            includedFiles.Add(filename);
                            break;
                        case MessageBoxResult.No:
                            // Skip this file.
                            break;
                        case MessageBoxResult.Cancel:
                        default:
                            // Cancel the entire operation.
                            Environment.Exit(0);
                            break;
                    }
                }
                else
                {
                    includedFiles.Add(filename);
                }
            }

            return includedFiles.ToArray();
        }

        private static void AppOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Trace.WriteLine($"DispatcherUnhandledException: {e.Exception}");
            Trace.Flush();
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Trace.WriteLine($"UnobservedTaskException: {e.Exception}");
            Trace.Flush();
            e.SetObserved();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.WriteLine($"UnhandledException: {e.ExceptionObject}");
            Trace.Flush();
        }
    }
}
