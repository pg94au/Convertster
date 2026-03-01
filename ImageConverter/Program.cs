using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ImageConverter;

internal static class Program
{
    private const string RegistryKeyPath = @"Software\Convertster";
    private const string JpgQualityValueName = "JpgQuality";
    private const string PngCompressionValueName = "PngCompression";
    private const int DefaultJpgQuality = 75;
    private const int DefaultPngCompression = 6;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

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
        var app = new Application
        {
            // Need this so that the app doesn't auto-shutdown when the first dialog closes.
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        app.DispatcherUnhandledException += AppOnDispatcherUnhandledException;

        // Create and run the WPF application with a ProgressWindow.
        // We avoid relying on generated App.xaml entry point by providing our own Main.
        var targetType = args[0].ToLower();
        var filenames = args.Skip(1).ToArray();

        Trace.WriteLine($"Considering conversion of {string.Join(", ", filenames)}");
        filenames = FilterAnySkippedFiles(targetType, filenames);
        Trace.WriteLine($"Converting {string.Join(", ", filenames)}");

        var (jpgQuality, pngCompression) = LoadSettingsFromRegistry();
        var window = new ProgressWindow(targetType, filenames, jpgQuality, pngCompression);
        var explorerHwnd = GetExplorerForegroundWindow();
        if (explorerHwnd != IntPtr.Zero)
        {
            // Set native owner to the explorer window that likely launched us
            new System.Windows.Interop.WindowInteropHelper(window).Owner = explorerHwnd;
        }
        app.Run(window);

        Trace.Flush();
    }

    private static (int JpgQuality, int PngCompression) LoadSettingsFromRegistry()
    {
        var jpgQuality = DefaultJpgQuality;
        var pngCompression = DefaultPngCompression;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            if (key != null)
            {
                var jpgQualityObj = key.GetValue(JpgQualityValueName);
                if (jpgQualityObj != null && int.TryParse(jpgQualityObj.ToString(), out var loadedJpgQuality))
                {
                    jpgQuality = Math.Max(5, Math.Min(100, loadedJpgQuality));
                }

                var pngCompressionObj = key.GetValue(PngCompressionValueName);
                if (pngCompressionObj != null && int.TryParse(pngCompressionObj.ToString(), out var loadedPngCompression))
                {
                    pngCompression = Math.Max(0, Math.Min(9, loadedPngCompression));
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error reading settings from registry: {ex.Message}");
        }

        return (jpgQuality, pngCompression);
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

            // Replace existing owner-selection code with this before overwriteDialog.ShowDialog()
            var explorerHwnd = GetExplorerForegroundWindow();
            if (explorerHwnd != IntPtr.Zero)
            {
                // Set native owner to the explorer window that likely launched us
                new System.Windows.Interop.WindowInteropHelper(overwriteDialog).Owner = explorerHwnd;
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

    private static IntPtr GetExplorerForegroundWindow()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return IntPtr.Zero;

        GetWindowThreadProcessId(hwnd, out uint pid);
        try
        {
            var proc = Process.GetProcessById((int)pid);
            if (string.Equals(proc.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase))
            {
                return hwnd;
            }
        }
        catch
        {
            // ignore
        }

        return IntPtr.Zero;
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