using NetVips;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageConverterNet
{
    public partial class ProgressForm : Form
    {
        private readonly string _targetType;
        private readonly string[] _filenames;

        public ProgressForm(string targetType, string[] filenames)
        {
            InitializeComponent();

            convertingFilesLabel.Text = $"Converting files to {targetType}...";

            _targetType = targetType;
            Trace.WriteLine($"Target type is {_targetType}");
            _filenames = filenames;
            Trace.WriteLine($"Filenames to convert: {string.Join(", ", _filenames)}");
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            conversionProgressBar.Minimum = 0;
            conversionProgressBar.Maximum = _filenames.Length;
            Trace.WriteLine($"Progress bar maximum = {conversionProgressBar.Maximum}");

            // Start the conversion process here or in a separate method
            var anySuccess = false;
            foreach (var filename in _filenames)
            {
                // Update the label to show the current file being processed
                currentFileNameLabel.Text = filename;

                await Task.Yield();

                try
                {
                    ConvertImageType(_targetType, filename);
                    Trace.WriteLine($"Successfully converted {filename}");
                    anySuccess = true;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error converting {filename}: {ex.Message}");
                    conversionProgressBar.SetState(ProgressBarExtensions.ProgressBarState.Paused);
                }

                conversionProgressBar.Value += 1;
                //conversionProgressBar.Refresh();
                await Task.Delay(1);

                await Task.Yield();
            }

            if (!anySuccess)
            {
                conversionProgressBar.SetState(ProgressBarExtensions.ProgressBarState.Error);
            }

            currentFileNameLabel.Text = "Conversion complete!";
            Trace.WriteLine($"Progress bar value = {conversionProgressBar.Value}");
        }

        private void ConvertImageType(string targetType, string filename)
        {
            var image = Image.NewFromFile(filename, access: Enums.Access.Sequential);

            switch (targetType)
            {
                case "JPG":
                    var jpgPath = Path.ChangeExtension(filename, ".jpg");
                    // TODO: Have some way to specify quality.
                    image.Jpegsave(jpgPath, q: 75);
                    break;
                case "PNG":
                    var pngPath = Path.ChangeExtension(filename, ".png");
                    image.Pngsave(pngPath);
                    break;
            }
        }
    }
}
