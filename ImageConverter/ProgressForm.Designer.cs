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
            SuspendLayout();
            // 
            // convertingFilesLabel
            // 
            convertingFilesLabel.AutoSize = true;
            convertingFilesLabel.Location = new Point(29, 34);
            convertingFilesLabel.Name = "convertingFilesLabel";
            convertingFilesLabel.Size = new Size(131, 20);
            convertingFilesLabel.TabIndex = 0;
            convertingFilesLabel.Text = "Converting file(s)...";
            // 
            // currentFileNameLabel
            // 
            currentFileNameLabel.AutoSize = true;
            currentFileNameLabel.Location = new Point(29, 90);
            currentFileNameLabel.Name = "currentFileNameLabel";
            currentFileNameLabel.Size = new Size(131, 20);
            currentFileNameLabel.TabIndex = 1;
            currentFileNameLabel.Text = "<some file name>";
            // 
            // ProgressForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(622, 273);
            Controls.Add(currentFileNameLabel);
            Controls.Add(convertingFilesLabel);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MaximumSize = new Size(640, 320);
            MinimumSize = new Size(640, 320);
            Name = "ProgressForm";
            Text = "Image Converter";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label convertingFilesLabel;
        private Label currentFileNameLabel;
    }
}
