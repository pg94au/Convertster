using System.Diagnostics;

namespace ImageConverter
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var debugOutputFile = Environment.GetEnvironmentVariable("BLINKENLIGHTS_IMAGE_CONVERTER_DEBUG_OUTPUT");
            if (debugOutputFile != null)
            {
                Trace.Listeners.Add(new TextWriterTraceListener(debugOutputFile));
                Trace.AutoFlush = true;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new ProgressForm(args[0], args.Skip(1).ToArray()));
        }
    }
}