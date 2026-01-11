namespace WAShell
{
	partial class SplashForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose (bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose ();
			}
			base.Dispose (disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent ()
		{
			this.components = new System.ComponentModel.Container();
			this.picbox = new System.Windows.Forms.PictureBox();
			this.timer = new System.Windows.Forms.Timer(this.components);
			((System.ComponentModel.ISupportInitialize)(this.picbox)).BeginInit();
			this.SuspendLayout();
			// 
			// picbox
			// 
			this.picbox.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.picbox.BackColor = System.Drawing.Color.Transparent;
			this.picbox.Location = new System.Drawing.Point(6, 47);
			this.picbox.Name = "picbox";
			this.picbox.Size = new System.Drawing.Size(620, 300);
			this.picbox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.picbox.TabIndex = 0;
			this.picbox.TabStop = false;
			// 
			// timer
			// 
			this.timer.Tick += new System.EventHandler(this.timer_Tick);
			// 
			// SplashForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.ClientSize = new System.Drawing.Size(632, 425);
			this.Controls.Add(this.picbox);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SplashForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Splash Screen";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SplashForm_FormClosed);
			this.Load += new System.EventHandler(this.SplashForm_Load);
			this.Shown += new System.EventHandler(this.SplashForm_Shown);
			this.Resize += new System.EventHandler(this.SplashForm_Resize);
			((System.ComponentModel.ISupportInitialize)(this.picbox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox picbox;
		private System.Windows.Forms.Timer timer;
	}
}