using System;
using System.Windows;
using Microsoft.Win32;
using ConfigureResources = Configure.Properties.Resources;

namespace Configure
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string RegistryKeyPath = @"Software\Convertster";
        private const string JpgQualityValueName = "JpgQuality";
        private const string PngCompressionValueName = "PngCompression";
        private const int DefaultJpgQuality = 75;
        private const int DefaultPngCompression = 6;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettingsFromRegistry();
            SaveButton.Click += SaveButton_Click;
        }

        private void LoadSettingsFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
                {
                    if (key != null)
                    {
                        // Read JPG Quality
                        var jpgQualityObj = key.GetValue(JpgQualityValueName);
                        if (jpgQualityObj != null && int.TryParse(jpgQualityObj.ToString(), out int jpgQuality))
                        {
                            // Clamp to valid range
                            JpegQualitySlider.Value = Math.Max(5, Math.Min(100, jpgQuality));
                        }
                        else
                        {
                            JpegQualitySlider.Value = DefaultJpgQuality;
                        }

                        // Read PNG Compression
                        var pngCompressionObj = key.GetValue(PngCompressionValueName);
                        if (pngCompressionObj != null && int.TryParse(pngCompressionObj.ToString(), out int pngCompression))
                        {
                            // Clamp to valid range
                            PngCompressionSlider.Value = Math.Max(0, Math.Min(9, pngCompression));
                        }
                        else
                        {
                            PngCompressionSlider.Value = DefaultPngCompression;
                        }
                    }
                    else
                    {
                        // Registry key doesn't exist, use defaults
                        JpegQualitySlider.Value = DefaultJpgQuality;
                        PngCompressionSlider.Value = DefaultPngCompression;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ConfigureResources.ErrorReadSettings, ex.Message), ConfigureResources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                JpegQualitySlider.Value = DefaultJpgQuality;
                PngCompressionSlider.Value = DefaultPngCompression;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath, true))
                {
                    if (key != null)
                    {
                        // Write JPG Quality
                        int jpgQuality = (int)Math.Round(JpegQualitySlider.Value);
                        key.SetValue(JpgQualityValueName, jpgQuality, RegistryValueKind.DWord);

                        // Write PNG Compression
                        int pngCompression = (int)Math.Round(PngCompressionSlider.Value);
                        key.SetValue(PngCompressionValueName, pngCompression, RegistryValueKind.DWord);
                    }
                    else
                    {
                        MessageBox.Show(ConfigureResources.FailedOpenRegistry, ConfigureResources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Exit the application after saving
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ConfigureResources.ErrorWriteSettings, ex.Message), ConfigureResources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
