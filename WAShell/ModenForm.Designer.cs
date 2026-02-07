using System.Drawing;
using System.Windows.Forms;

namespace WAShell
{
	partial class ModernForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method by the code editor.
		/// </summary>
		private void InitializeComponent ()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModernForm));
			this.SuspendLayout();
			// 
			// ModernForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 600);
			this.DoubleBuffered = false;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ModernForm";
			this.Text = "Modern Custom Form";
			this.ResumeLayout(false);
			this.Padding = new Padding (0); // 边框交给系统管理
			this.BackColor = Color.White;
		}

		#endregion
	}
}