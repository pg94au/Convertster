using System.Windows;

namespace ImageConverter
{
    public enum OverwriteDialogResult
    {
        Yes,
        YesToAll,
        No,
        Cancel
    }

    public partial class OverwriteDialog : Window
    {
        public OverwriteDialog(string filename)
        {
            InitializeComponent();
            DetailText.Text = filename;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Tag = OverwriteDialogResult.Yes;
            Close();
        }

        private void YesAllButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Tag = OverwriteDialogResult.YesToAll;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Tag = OverwriteDialogResult.No;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Tag = OverwriteDialogResult.Cancel;
            Close();
        }

        public OverwriteDialogResult GetResult()
        {
            if (Tag is OverwriteDialogResult r) return r;
            return OverwriteDialogResult.Cancel;
        }
    }
}
