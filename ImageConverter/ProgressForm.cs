namespace ImageConverter
{
    public partial class ProgressForm : Form
    {
        private string[] _filenames;

        public ProgressForm(string[] filenames)
        {
            InitializeComponent();

            _filenames = filenames;
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Start the conversion process here or in a separate method
            foreach (var filename in _filenames)
            {
                // Update the label to show the current file being processed
                currentFileNameLabel.Text = filename;

                await Task.Yield();

                // Simulate file conversion with a delay (replace with actual conversion logic)
                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            currentFileNameLabel.Text = "Conversion complete!";
        }
    }
}
