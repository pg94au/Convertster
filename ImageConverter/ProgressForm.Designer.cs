namespace ImageConverter
{
    partial class ProgressForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            convertingFilesLabel = new Label();
            currentFileNameLabel = new Label();
            conversionProgressBar = new ProgressBar();
            SuspendLayout();
            // 
            // convertingFilesLabel
            // 
            convertingFilesLabel.AutoSize = true;
            convertingFilesLabel.Location = new Point(25, 26);
            convertingFilesLabel.Name = "convertingFilesLabel";
            convertingFilesLabel.Size = new Size(107, 15);
            convertingFilesLabel.TabIndex = 0;
            convertingFilesLabel.Text = "Converting file(s)...";
            // 
            // currentFileNameLabel
            // 
            currentFileNameLabel.AutoSize = true;
            currentFileNameLabel.Location = new Point(25, 105);
            currentFileNameLabel.Name = "currentFileNameLabel";
            currentFileNameLabel.Size = new Size(104, 15);
            currentFileNameLabel.TabIndex = 1;
            currentFileNameLabel.Text = "<some file name>";
            // 
            // conversionProgressBar
            // 
            conversionProgressBar.Location = new Point(25, 64);
            conversionProgressBar.Name = "conversionProgressBar";
            conversionProgressBar.Size = new Size(498, 23);
            conversionProgressBar.TabIndex = 2;
            // 
            // ProgressForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(546, 211);
            Controls.Add(conversionProgressBar);
            Controls.Add(currentFileNameLabel);
            Controls.Add(convertingFilesLabel);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            MaximumSize = new Size(562, 250);
            MinimumSize = new Size(562, 250);
            Name = "ProgressForm";
            Text = "Image Converter";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label convertingFilesLabel;
        private Label currentFileNameLabel;
        private ProgressBar conversionProgressBar;
    }
}
