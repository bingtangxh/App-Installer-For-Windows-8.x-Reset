namespace Manager
{
	partial class ManagerShell
	{
		/// <summary>
		/// 必需的设计器变量。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 清理所有正在使用的资源。
		/// </summary>
		/// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
		protected override void Dispose (bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose ();
			}
			base.Dispose (disposing);
		}

		#region Windows 窗体设计器生成的代码

		/// <summary>
		/// 设计器支持所需的方法 - 不要修改
		/// 使用代码编辑器修改此方法的内容。
		/// </summary>
		private void InitializeComponent ()
		{
			this.SuspendLayout();
			// 
			// ManagerShell
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(661, 416);
			this.Location = new System.Drawing.Point(0, 0);
			this.Name = "ManagerShell";
			this.PageScale = 125;
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.ManagerShell_Load);
			this.Resize += new System.EventHandler(this.ManagerShell_Resize);
			this.ResumeLayout(false);

		}

		#endregion
	}
}

