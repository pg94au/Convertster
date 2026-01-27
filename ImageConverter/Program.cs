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

            // Create the WPF Application early so we can show WPF dialogs
            // before running the main window.
            var app = new Application();
            app.DispatcherUnhandledException += AppOnDispatcherUnhandledException;

            // Create and run the WPF application with a ProgressWindow.
            // We avoid relying on generated App.xaml entry point by providing our own Main.
            var targetType = args[0].ToLower();
            var filenames = args.Skip(1).ToArray();

            Trace.WriteLine($"Considering conversion of {string.Join(", ", filenames)}");
            filenames = FilterAnySkippedFiles(targetType, filenames);
            Trace.WriteLine($"Converting {string.Join(", ", filenames)}");

            var window = new ProgressWindow(targetType, filenames);
            app.Run(window);

            Trace.Flush();
        }

        private static string[] FilterAnySkippedFiles(string targetType, string[] filenames)
        {
            var includedFiles = new List<string>();
            bool applyYesToAll = false;
            for (int i = 0; i < filenames.Length; i++)
            {
                var filename = filenames[i];
                var targetFilename = Path.ChangeExtension(filename, targetType.ToLower());

                if (!File.Exists(targetFilename))
                {
                    includedFiles.Add(filename);
                    continue;
                }

                if (applyYesToAll)
                {
                    includedFiles.Add(filename);
                    continue;
                }

                // Use the existing OverwriteDialog (XAML) to prompt the user.
                var overwriteDialog = new OverwriteDialog(targetFilename);
                var ownerWindow = Application.Current?.Windows.OfType<Window>().FirstOrDefault();
                if (ownerWindow != null && !ReferenceEquals(ownerWindow, overwriteDialog))
                {
                    overwriteDialog.Owner = ownerWindow;
                }

                overwriteDialog.ShowDialog();
                var finalChoice = overwriteDialog.GetResult();

                switch (finalChoice)
                {
                    case OverwriteDialogResult.Yes:
                        includedFiles.Add(filename);
                        break;
                    case OverwriteDialogResult.YesToAll:
                        includedFiles.Add(filename);
                        applyYesToAll = true;
                        break;
                    case OverwriteDialogResult.No:
                        // skip
                        break;
                    case OverwriteDialogResult.Cancel:
                    default:
                        Environment.Exit(0);
                        break;
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
