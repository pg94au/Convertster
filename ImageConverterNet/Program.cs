using System;
using System.Linq;
using System.Windows;

namespace ImageConverterNet
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // Create and run the WPF application with a ProgressWindow.
            // We avoid relying on generated App.xaml entry point by providing our own Main.
            var window = new ProgressWindow(args[0], args.Skip(1).ToArray());
            var app = new Application();
            app.Run(window);
        }
    }
}
