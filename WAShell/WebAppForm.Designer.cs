namespace WAShell
{
	partial class WebAppForm
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
			this.webui = new System.Windows.Forms.WebBrowser();
			this.SuspendLayout();
			// 
			// webui
			// 
			this.webui.Dock = System.Windows.Forms.DockStyle.Fill;
			this.webui.Location = new System.Drawing.Point(0, 0);
			this.webui.MinimumSize = new System.Drawing.Size(20, 20);
			this.webui.Name = "webui";
			this.webui.Size = new System.Drawing.Size(661, 416);
			this.webui.TabIndex = 0;
			this.webui.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webui_DocumentCompleted);
			this.webui.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.webui_PreviewKeyDown);
			// 
			// WebAppForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(661, 416);
			this.Controls.Add(this.webui);
			this.Name = "WebAppForm";
			this.Text = "Main Web App Form";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WebAppForm_FormClosing);
			this.Load += new System.EventHandler(this.WebAppForm_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.WebBrowser webui;
	}
}