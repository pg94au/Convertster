using System;
using System.Diagnostics;
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
            var window = new ProgressWindow(args[0], args.Skip(1).ToArray());
            var app = new Application();
            app.DispatcherUnhandledException += AppOnDispatcherUnhandledException;
            app.Run(window);

            Trace.Flush();
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
