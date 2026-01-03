using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace ImageConverter;

public partial class ProgressForm : Form
{
    private string _targetType;
    private string[] _filenames;

    public ProgressForm(string targetType, string[] filenames)
    {
        InitializeComponent();

        convertingFilesLabel.Text = $"Converting files to {targetType}...";

        _targetType = targetType;
        _filenames = filenames;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        conversionProgressBar.Minimum = 0;
        conversionProgressBar.Maximum = _filenames.Length;

        // Start the conversion process here or in a separate method
        foreach (var filename in _filenames)
        {
            // Update the label to show the current file being processed
            currentFileNameLabel.Text = filename;

            await Task.Yield();

            await ConvertImageType(_targetType, filename);

            conversionProgressBar.Value += 1;

            await Task.Yield();
        }

        currentFileNameLabel.Text = "Conversion complete!";
    }

    private async Task ConvertImageType(string targetType, string filename)
    {
        var image = await SixLabors.ImageSharp.Image.LoadAsync(filename);

        switch (targetType)
        {
            case "JPG":
                var jpgPath = Path.ChangeExtension(filename, ".jpg");
                // TODO: Have some way to specify quality.
                await image.SaveAsJpegAsync(jpgPath, new JpegEncoder { Quality = 75 });
                break;
            case "PNG":
                var pngPath = Path.ChangeExtension(filename, ".png");
                await image.SaveAsPngAsync(pngPath, new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression });
                break;
        }
    }
}