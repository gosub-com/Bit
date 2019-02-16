namespace Gosub.Bit
{
	partial class FormSetupSimulation
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
			this.listBoxes = new System.Windows.Forms.ListBox();
			this.labelGates = new System.Windows.Forms.Label();
			this.labelName = new System.Windows.Forms.Label();
			this.buttonSimulate = new System.Windows.Forms.Button();
			this.buttonViewCompiledCode = new System.Windows.Forms.Button();
			this.groupCompile = new System.Windows.Forms.GroupBox();
			this.checkDisableOptimizer = new System.Windows.Forms.CheckBox();
			this.groupCompile.SuspendLayout();
			this.SuspendLayout();
			// 
			// listBoxes
			// 
			this.listBoxes.FormattingEnabled = true;
			this.listBoxes.Location = new System.Drawing.Point(8, 32);
			this.listBoxes.Name = "listBoxes";
			this.listBoxes.Size = new System.Drawing.Size(304, 303);
			this.listBoxes.Sorted = true;
			this.listBoxes.TabIndex = 0;
			this.listBoxes.SelectedIndexChanged += new System.EventHandler(this.listBoxes_SelectedIndexChanged);
			// 
			// labelGates
			// 
			this.labelGates.AutoSize = true;
			this.labelGates.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelGates.Location = new System.Drawing.Point(176, 0);
			this.labelGates.Name = "labelGates";
			this.labelGates.Size = new System.Drawing.Size(104, 26);
			this.labelGates.TabIndex = 1;
			this.labelGates.Text = "Gates\r\n(pre-optimization)";
			this.labelGates.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// labelName
			// 
			this.labelName.AutoSize = true;
			this.labelName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelName.Location = new System.Drawing.Point(8, 16);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(39, 13);
			this.labelName.TabIndex = 4;
			this.labelName.Text = "Name";
			// 
			// buttonSimulate
			// 
			this.buttonSimulate.Location = new System.Drawing.Point(216, 16);
			this.buttonSimulate.Name = "buttonSimulate";
			this.buttonSimulate.Size = new System.Drawing.Size(80, 24);
			this.buttonSimulate.TabIndex = 8;
			this.buttonSimulate.Text = "Simulate";
			this.buttonSimulate.UseVisualStyleBackColor = true;
			this.buttonSimulate.Click += new System.EventHandler(this.buttonSimulate_Click);
			// 
			// buttonViewCompiledCode
			// 
			this.buttonViewCompiledCode.Location = new System.Drawing.Point(8, 16);
			this.buttonViewCompiledCode.Name = "buttonViewCompiledCode";
			this.buttonViewCompiledCode.Size = new System.Drawing.Size(80, 24);
			this.buttonViewCompiledCode.TabIndex = 4;
			this.buttonViewCompiledCode.Text = "View Code";
			this.buttonViewCompiledCode.UseVisualStyleBackColor = true;
			this.buttonViewCompiledCode.Click += new System.EventHandler(this.buttonViewCompiledCode_Click);
			// 
			// groupCompile
			// 
			this.groupCompile.Controls.Add(this.checkDisableOptimizer);
			this.groupCompile.Controls.Add(this.buttonSimulate);
			this.groupCompile.Controls.Add(this.buttonViewCompiledCode);
			this.groupCompile.Enabled = false;
			this.groupCompile.Location = new System.Drawing.Point(8, 336);
			this.groupCompile.Name = "groupCompile";
			this.groupCompile.Size = new System.Drawing.Size(304, 80);
			this.groupCompile.TabIndex = 5;
			this.groupCompile.TabStop = false;
			// 
			// checkDisableOptimizer
			// 
			this.checkDisableOptimizer.AutoSize = true;
			this.checkDisableOptimizer.Location = new System.Drawing.Point(8, 56);
			this.checkDisableOptimizer.Name = "checkDisableOptimizer";
			this.checkDisableOptimizer.Size = new System.Drawing.Size(107, 17);
			this.checkDisableOptimizer.TabIndex = 9;
			this.checkDisableOptimizer.Text = "Disable Optimizer";
			this.checkDisableOptimizer.UseVisualStyleBackColor = true;
			this.checkDisableOptimizer.CheckedChanged += new System.EventHandler(this.checkDisableOptimizer_CheckedChanged);
			// 
			// FormSetupSimulation
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(319, 437);
			this.Controls.Add(this.listBoxes);
			this.Controls.Add(this.groupCompile);
			this.Controls.Add(this.labelName);
			this.Controls.Add(this.labelGates);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormSetupSimulation";
			this.Text = "Setup Simulation";
			this.Load += new System.EventHandler(this.FormSetupSimulation_Load);
			this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FormSetupSimulation_KeyPress);
			this.groupCompile.ResumeLayout(false);
			this.groupCompile.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxes;
		private System.Windows.Forms.Label labelGates;
		private System.Windows.Forms.Label labelName;
		private System.Windows.Forms.Button buttonViewCompiledCode;
		private System.Windows.Forms.Button buttonSimulate;
		private System.Windows.Forms.GroupBox groupCompile;
		private System.Windows.Forms.CheckBox checkDisableOptimizer;
	}
}