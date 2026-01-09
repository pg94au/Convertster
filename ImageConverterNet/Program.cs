using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace ImageConverterNet
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ProgressForm(args[0], args.Skip(1).ToArray()));
        }
    }
}
