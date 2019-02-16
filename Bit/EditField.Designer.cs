namespace Gosub.Bit
{
	partial class EditField
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			Gosub.Bit.Lexer lexer1 = new Gosub.Bit.Lexer();
			this.labelFieldName = new System.Windows.Forms.Label();
			this.comboBase = new System.Windows.Forms.ComboBox();
			this.editor1 = new Gosub.Bit.Editor();
			this.SuspendLayout();
			// 
			// labelFieldName
			// 
			this.labelFieldName.AutoSize = true;
			this.labelFieldName.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelFieldName.Location = new System.Drawing.Point(272, 0);
			this.labelFieldName.Name = "labelFieldName";
			this.labelFieldName.Size = new System.Drawing.Size(119, 17);
			this.labelFieldName.TabIndex = 2;
			this.labelFieldName.Text = "labelFieldName";
			// 
			// comboBase
			// 
			this.comboBase.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBase.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.comboBase.FormattingEnabled = true;
			this.comboBase.ItemHeight = 13;
			this.comboBase.Items.AddRange(new object[] {
            "Bin",
            "Dec",
            "Hex"});
			this.comboBase.Location = new System.Drawing.Point(0, 0);
			this.comboBase.Name = "comboBase";
			this.comboBase.Size = new System.Drawing.Size(64, 21);
			this.comboBase.TabIndex = 1;
			this.comboBase.TabStop = false;
			this.comboBase.SelectedIndexChanged += new System.EventHandler(this.comboBase_SelectedIndexChanged);
			// 
			// editor1
			// 
			this.editor1.BackColor = System.Drawing.SystemColors.Window;
			this.editor1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.editor1.Cursor = System.Windows.Forms.Cursors.IBeam;
			this.editor1.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.editor1.Lexer = lexer1;
			this.editor1.Location = new System.Drawing.Point(64, 0);
			this.editor1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.editor1.Name = "editor1";
			this.editor1.OverwriteMode = true;
			this.editor1.ReadOnly = false;
			this.editor1.Size = new System.Drawing.Size(208, 21);
			this.editor1.TokenColorOverrides = null;
			this.editor1.TabIndex = 0;
			this.editor1.TabSize = 4;
			this.editor1.TextChanged2 += new System.EventHandler(this.editor1_TextChanged2);
			this.editor1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.editor1_KeyPress);
			this.editor1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.editor1_KeyDown);
			// 
			// EditField
			// 
			this.Controls.Add(this.editor1);
			this.Controls.Add(this.comboBase);
			this.Controls.Add(this.labelFieldName);
			this.Name = "EditField";
			this.Size = new System.Drawing.Size(402, 22);
			this.Load += new System.EventHandler(this.EditField_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelFieldName;
		private System.Windows.Forms.ComboBox comboBase;
		private Editor editor1;
	}
}
