namespace Gosub.Bit
{
	partial class FormSimulate
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
			this.components = new System.ComponentModel.Container();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.labelLocals = new System.Windows.Forms.Label();
			this.panelLocals = new System.Windows.Forms.Panel();
			this.panelParams = new System.Windows.Forms.Panel();
			this.comboSpeed = new System.Windows.Forms.ComboBox();
			this.labelTotalGen = new System.Windows.Forms.Label();
			this.labelPrevGen = new System.Windows.Forms.Label();
			this.labelThisGen = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.buttonPause = new System.Windows.Forms.Button();
			this.buttonClearCounts = new System.Windows.Forms.Button();
			this.buttonReset0 = new System.Windows.Forms.Button();
			this.buttonReset1 = new System.Windows.Forms.Button();
			this.labelRunning = new System.Windows.Forms.Label();
			this.labelStats = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 20;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// labelLocals
			// 
			this.labelLocals.AutoSize = true;
			this.labelLocals.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelLocals.Location = new System.Drawing.Point(8, 128);
			this.labelLocals.Name = "labelLocals";
			this.labelLocals.Size = new System.Drawing.Size(69, 24);
			this.labelLocals.TabIndex = 1;
			this.labelLocals.Text = "Locals:";
			// 
			// panelLocals
			// 
			this.panelLocals.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panelLocals.AutoScroll = true;
			this.panelLocals.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panelLocals.Location = new System.Drawing.Point(8, 152);
			this.panelLocals.Name = "panelLocals";
			this.panelLocals.Size = new System.Drawing.Size(836, 501);
			this.panelLocals.TabIndex = 2;
			// 
			// panelParams
			// 
			this.panelParams.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panelParams.AutoScroll = true;
			this.panelParams.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panelParams.Location = new System.Drawing.Point(8, 64);
			this.panelParams.Name = "panelParams";
			this.panelParams.Size = new System.Drawing.Size(836, 64);
			this.panelParams.TabIndex = 3;
			// 
			// comboSpeed
			// 
			this.comboSpeed.FormattingEnabled = true;
			this.comboSpeed.Location = new System.Drawing.Point(8, 32);
			this.comboSpeed.Name = "comboSpeed";
			this.comboSpeed.Size = new System.Drawing.Size(144, 21);
			this.comboSpeed.TabIndex = 5;
			this.comboSpeed.TabStop = false;
			this.comboSpeed.SelectedIndexChanged += new System.EventHandler(this.comboSpeed_SelectedIndexChanged);
			// 
			// labelTotalGen
			// 
			this.labelTotalGen.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelTotalGen.Location = new System.Drawing.Point(224, 40);
			this.labelTotalGen.Name = "labelTotalGen";
			this.labelTotalGen.Size = new System.Drawing.Size(48, 16);
			this.labelTotalGen.TabIndex = 7;
			this.labelTotalGen.Text = "0";
			this.labelTotalGen.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// labelPrevGen
			// 
			this.labelPrevGen.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelPrevGen.Location = new System.Drawing.Point(224, 24);
			this.labelPrevGen.Name = "labelPrevGen";
			this.labelPrevGen.Size = new System.Drawing.Size(48, 16);
			this.labelPrevGen.TabIndex = 8;
			this.labelPrevGen.Text = "0";
			this.labelPrevGen.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// labelThisGen
			// 
			this.labelThisGen.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelThisGen.Location = new System.Drawing.Point(224, 8);
			this.labelThisGen.Name = "labelThisGen";
			this.labelThisGen.Size = new System.Drawing.Size(48, 16);
			this.labelThisGen.TabIndex = 10;
			this.labelThisGen.Text = "0";
			this.labelThisGen.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label6
			// 
			this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label6.Location = new System.Drawing.Point(152, 40);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(72, 13);
			this.label6.TabIndex = 11;
			this.label6.Text = "Total Gen:";
			this.label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label7
			// 
			this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label7.Location = new System.Drawing.Point(152, 24);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(72, 14);
			this.label7.TabIndex = 12;
			this.label7.Text = "Prev. Gen:";
			this.label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label8
			// 
			this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label8.Location = new System.Drawing.Point(160, 8);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(64, 13);
			this.label8.TabIndex = 13;
			this.label8.Text = "This Gen:";
			this.label8.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// buttonPause
			// 
			this.buttonPause.Location = new System.Drawing.Point(88, 8);
			this.buttonPause.Name = "buttonPause";
			this.buttonPause.Size = new System.Drawing.Size(64, 23);
			this.buttonPause.TabIndex = 14;
			this.buttonPause.Text = "Stop (F5)";
			this.buttonPause.UseVisualStyleBackColor = true;
			this.buttonPause.Click += new System.EventHandler(this.buttonPause_Click);
			// 
			// buttonClearCounts
			// 
			this.buttonClearCounts.Location = new System.Drawing.Point(280, 8);
			this.buttonClearCounts.Name = "buttonClearCounts";
			this.buttonClearCounts.Size = new System.Drawing.Size(48, 48);
			this.buttonClearCounts.TabIndex = 16;
			this.buttonClearCounts.Text = "Clear\r\nTotals\r\n";
			this.buttonClearCounts.UseVisualStyleBackColor = true;
			this.buttonClearCounts.Click += new System.EventHandler(this.buttonClearCounts_Click);
			// 
			// buttonReset0
			// 
			this.buttonReset0.Location = new System.Drawing.Point(608, 8);
			this.buttonReset0.Name = "buttonReset0";
			this.buttonReset0.Size = new System.Drawing.Size(75, 23);
			this.buttonReset0.TabIndex = 17;
			this.buttonReset0.Text = "Reset 0";
			this.buttonReset0.UseVisualStyleBackColor = true;
			this.buttonReset0.Click += new System.EventHandler(this.buttonReset0_Click);
			// 
			// buttonReset1
			// 
			this.buttonReset1.Location = new System.Drawing.Point(608, 32);
			this.buttonReset1.Name = "buttonReset1";
			this.buttonReset1.Size = new System.Drawing.Size(75, 23);
			this.buttonReset1.TabIndex = 18;
			this.buttonReset1.Text = "Reset 1";
			this.buttonReset1.UseVisualStyleBackColor = true;
			this.buttonReset1.Click += new System.EventHandler(this.buttonReset1_Click);
			// 
			// labelRunning
			// 
			this.labelRunning.AutoSize = true;
			this.labelRunning.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelRunning.Location = new System.Drawing.Point(8, 8);
			this.labelRunning.Name = "labelRunning";
			this.labelRunning.Size = new System.Drawing.Size(69, 18);
			this.labelRunning.TabIndex = 19;
			this.labelRunning.Text = "Running";
			// 
			// labelStats
			// 
			this.labelStats.AutoSize = true;
			this.labelStats.Location = new System.Drawing.Point(344, 8);
			this.labelStats.Name = "labelStats";
			this.labelStats.Size = new System.Drawing.Size(35, 13);
			this.labelStats.TabIndex = 20;
			this.labelStats.Text = "label1";
			// 
			// FormSimulate
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(853, 661);
			this.Controls.Add(this.labelStats);
			this.Controls.Add(this.labelRunning);
			this.Controls.Add(this.buttonReset1);
			this.Controls.Add(this.buttonReset0);
			this.Controls.Add(this.buttonClearCounts);
			this.Controls.Add(this.buttonPause);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.labelTotalGen);
			this.Controls.Add(this.labelPrevGen);
			this.Controls.Add(this.labelThisGen);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.comboSpeed);
			this.Controls.Add(this.panelParams);
			this.Controls.Add(this.panelLocals);
			this.Controls.Add(this.labelLocals);
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.Name = "FormSimulate";
			this.Text = "Simulate";
			this.Load += new System.EventHandler(this.FormSimulate_Load);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormSimulate_FormClosing);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Label labelLocals;
		private System.Windows.Forms.Panel panelLocals;
		private System.Windows.Forms.Panel panelParams;
		private System.Windows.Forms.ComboBox comboSpeed;
		private System.Windows.Forms.Label labelTotalGen;
		private System.Windows.Forms.Label labelPrevGen;
		private System.Windows.Forms.Label labelThisGen;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Button buttonPause;
		private System.Windows.Forms.Button buttonClearCounts;
		private System.Windows.Forms.Button buttonReset0;
		private System.Windows.Forms.Button buttonReset1;
		private System.Windows.Forms.Label labelRunning;
		private System.Windows.Forms.Label labelStats;
	}
}