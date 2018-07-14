namespace ShenzhenMod
{
    partial class Form1
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
            this.m_exeFolderField = new System.Windows.Forms.TextBox();
            this.m_browseButton = new System.Windows.Forms.Button();
            this.m_patchButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // m_exeFolderField
            // 
            this.m_exeFolderField.Location = new System.Drawing.Point(13, 57);
            this.m_exeFolderField.Name = "m_exeFolderField";
            this.m_exeFolderField.Size = new System.Drawing.Size(509, 20);
            this.m_exeFolderField.TabIndex = 0;
            // 
            // m_browseButton
            // 
            this.m_browseButton.Location = new System.Drawing.Point(537, 53);
            this.m_browseButton.Name = "m_browseButton";
            this.m_browseButton.Size = new System.Drawing.Size(75, 23);
            this.m_browseButton.TabIndex = 1;
            this.m_browseButton.Text = "Browse...";
            this.m_browseButton.UseVisualStyleBackColor = true;
            this.m_browseButton.Click += new System.EventHandler(this.BrowseButtonClick);
            // 
            // m_patchButton
            // 
            this.m_patchButton.Location = new System.Drawing.Point(271, 186);
            this.m_patchButton.Name = "m_patchButton";
            this.m_patchButton.Size = new System.Drawing.Size(75, 23);
            this.m_patchButton.TabIndex = 1;
            this.m_patchButton.Text = "Patch";
            this.m_patchButton.UseVisualStyleBackColor = true;
            this.m_patchButton.Click += new System.EventHandler(this.PatchButtonClick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(632, 406);
            this.Controls.Add(this.m_patchButton);
            this.Controls.Add(this.m_browseButton);
            this.Controls.Add(this.m_exeFolderField);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox m_exeFolderField;
        private System.Windows.Forms.Button m_browseButton;
        private System.Windows.Forms.Button m_patchButton;
    }
}

