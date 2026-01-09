namespace ImageConverterNet
{
    partial class ProgressForm  
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.convertingFilesLabel = new System.Windows.Forms.Label();
            this.conversionProgressBar = new System.Windows.Forms.ProgressBar();
            this.currentFileNameLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // convertingFilesLabel
            // 
            this.convertingFilesLabel.AutoSize = true;
            this.convertingFilesLabel.Location = new System.Drawing.Point(13, 13);
            this.convertingFilesLabel.Name = "convertingFilesLabel";
            this.convertingFilesLabel.Size = new System.Drawing.Size(131, 18);
            this.convertingFilesLabel.TabIndex = 0;
            this.convertingFilesLabel.Text = "Converting file(s)...";
            // 
            // conversionProgressBar
            // 
            this.conversionProgressBar.Location = new System.Drawing.Point(12, 50);
            this.conversionProgressBar.Name = "conversionProgressBar";
            this.conversionProgressBar.Size = new System.Drawing.Size(674, 23);
            this.conversionProgressBar.TabIndex = 1;
            // 
            // currentFileNameLabel
            // 
            this.currentFileNameLabel.AutoSize = true;
            this.currentFileNameLabel.Location = new System.Drawing.Point(9, 85);
            this.currentFileNameLabel.Name = "currentFileNameLabel";
            this.currentFileNameLabel.Size = new System.Drawing.Size(145, 18);
            this.currentFileNameLabel.TabIndex = 2;
            this.currentFileNameLabel.Text = "<<some file name>>";
            // 
            // ProgressForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(702, 203);
            this.Controls.Add(this.currentFileNameLabel);
            this.Controls.Add(this.conversionProgressBar);
            this.Controls.Add(this.convertingFilesLabel);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(720, 250);
            this.MinimumSize = new System.Drawing.Size(720, 250);
            this.Name = "ProgressForm";
            this.Text = "Blinkenlights Image Converter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label convertingFilesLabel;
        private System.Windows.Forms.ProgressBar conversionProgressBar;
        private System.Windows.Forms.Label currentFileNameLabel;
    }
}

