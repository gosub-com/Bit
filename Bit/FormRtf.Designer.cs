namespace Gosub.Bit
{
	partial class FormRtf
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
			this.rtf = new System.Windows.Forms.RichTextBox();
			this.labelLoading = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// rtf
			// 
			this.rtf.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.rtf.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.rtf.Location = new System.Drawing.Point(16, 16);
			this.rtf.Name = "rtf";
			this.rtf.Size = new System.Drawing.Size(884, 440);
			this.rtf.TabIndex = 0;
			this.rtf.Text = "";
			// 
			// labelLoading
			// 
			this.labelLoading.AutoSize = true;
			this.labelLoading.Font = new System.Drawing.Font("Microsoft Sans Serif", 72F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelLoading.Location = new System.Drawing.Point(216, 176);
			this.labelLoading.Name = "labelLoading";
			this.labelLoading.Size = new System.Drawing.Size(465, 108);
			this.labelLoading.TabIndex = 1;
			this.labelLoading.Text = "Loading...";
			// 
			// FormRtf
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(910, 466);
			this.Controls.Add(this.labelLoading);
			this.Controls.Add(this.rtf);
			this.Name = "FormRtf";
			this.Text = "FormRtf";
			this.Load += new System.EventHandler(this.FormRtf_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RichTextBox rtf;
		private System.Windows.Forms.Label labelLoading;
	}
}