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
        private enum OverwriteDialogResult
        {
            Yes,
            YesToAll,
            No,
            Cancel
        }

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

                // Build a simple WPF dialog window programmatically to avoid
                // issues with external XAML files.
                var dlg = new Window
                {
                    Title = "Confirm overwrite",
                    Width = 520,
                    Height = 180,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ResizeMode = ResizeMode.NoResize
                };

                // Assign an owner window if one exists and is not the dialog
                var ownerWindow = Application.Current?.Windows.OfType<Window>().FirstOrDefault();
                if (ownerWindow != null && !ReferenceEquals(ownerWindow, dlg))
                {
                    dlg.Owner = ownerWindow;
                }

                var grid = new System.Windows.Controls.Grid { Margin = new Thickness(12) };
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

                var instr = new System.Windows.Controls.TextBlock { Text = "File already exists", FontWeight = FontWeights.Bold, FontSize = 14 };
                System.Windows.Controls.Grid.SetRow(instr, 0);
                grid.Children.Add(instr);

                var detail = new System.Windows.Controls.TextBlock { Text = targetFilename, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,8,0,8) };
                System.Windows.Controls.Grid.SetRow(detail, 1);
                grid.Children.Add(detail);

                var panel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };

                var yesBtn = new System.Windows.Controls.Button { Content = "Yes", Width = 96, Margin = new Thickness(4,0,4,0) };
                var yesAllBtn = new System.Windows.Controls.Button { Content = "Yes to All", Width = 120, Margin = new Thickness(4,0,4,0) };
                var noBtn = new System.Windows.Controls.Button { Content = "No", Width = 96, Margin = new Thickness(4,0,4,0) };
                var cancelBtn = new System.Windows.Controls.Button { Content = "Cancel", Width = 96, Margin = new Thickness(4,0,4,0) };

                panel.Children.Add(yesBtn);
                panel.Children.Add(yesAllBtn);
                panel.Children.Add(noBtn);
                panel.Children.Add(cancelBtn);
                System.Windows.Controls.Grid.SetRow(panel, 2);
                grid.Children.Add(panel);

                OverwriteDialogResult? choice = null;

                yesBtn.Click += (_, __) => { choice = OverwriteDialogResult.Yes; dlg.DialogResult = true; dlg.Close(); };
                yesAllBtn.Click += (_, __) => { choice = OverwriteDialogResult.YesToAll; dlg.DialogResult = true; dlg.Close(); };
                noBtn.Click += (_, __) => { choice = OverwriteDialogResult.No; dlg.DialogResult = false; dlg.Close(); };
                cancelBtn.Click += (_, __) => { choice = OverwriteDialogResult.Cancel; dlg.DialogResult = false; dlg.Close(); };

                dlg.Content = grid;

                // ShowDialog will block until the dialog is closed.
                dlg.ShowDialog();

                var finalChoice = choice ?? OverwriteDialogResult.Cancel;

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
